using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;

namespace WintryVR.Capture
{
    /// <summary>
    /// Smart Camera Capture: frames are captured only when a trigger asks for one (voice command, gaze dwell,
    /// object selection, scene change, low-confidence retry). Nothing streams continuously to the cloud.
    /// Handles throttling, the camera privacy indicator and provider fallback.
    /// </summary>
    public class CameraCaptureService : ICameraCaptureService
    {
        private readonly List<ICameraCaptureProvider> _providers = new List<ICameraCaptureProvider>();
        private ICameraCaptureProvider _active;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private float _lastCaptureTime = -999f;
        private CapturedFrame _lastFrame;

        public float MinIntervalSeconds = 0.4f;      // hard throttle
        public float ReuseWindowSeconds = 1.5f;      // reuse the last frame for quick follow-ups
        public bool IsCapturing { get; private set; }
        public ICameraCaptureProvider ActiveProvider => _active;
        public CapturedFrame LastFrame => _lastFrame;
        public int TotalCaptures { get; private set; }

        public CameraCaptureService(IEnumerable<ICameraCaptureProvider> providersInPriorityOrder)
        {
            _providers.AddRange(providersInPriorityOrder);
        }

        public async Task<bool> InitializeAsync()
        {
            foreach (var p in _providers)
            {
                try
                {
                    if (!p.IsAvailable) { WintryLog.I("Camera", p.Name + " not available"); continue; }
                    if (p.RequiresPermission && !await PermissionManager.EnsureCameraAsync()) { WintryLog.W("Camera", p.Name + " needs camera permission (denied)"); continue; }
                    if (await p.InitializeAsync()) { _active = p; WintryLog.I("Camera", "Active capture provider: " + p.Name); return true; }
                }
                catch (Exception ex) { WintryLog.W("Camera", p.Name + " init failed: " + ex.Message); }
            }
            WintryLog.W("Camera", "No capture provider available");
            return false;
        }

        public async Task<CapturedFrame> CaptureAsync(CaptureTrigger trigger, CancellationToken ct)
        {
            if (!WintrySettings.Current.Privacy.CameraAllowed) return null;
            if (_active == null && !await InitializeAsync()) return null;

            await _gate.WaitAsync(ct);
            try
            {
                float now = Time.realtimeSinceStartup;
                bool quickFollowUp = trigger == CaptureTrigger.Gaze || trigger == CaptureTrigger.SceneChange;
                if (_lastFrame != null && _lastFrame.IsValid && now - _lastCaptureTime < (quickFollowUp ? ReuseWindowSeconds : MinIntervalSeconds))
                    return _lastFrame;

                IsCapturing = true;
                PublishIndicator(true);
                var frame = await _active.CaptureAsync(ct);
                if (frame != null && frame.IsValid)
                {
                    _lastFrame = frame; _lastCaptureTime = Time.realtimeSinceStartup; TotalCaptures++;
                    WintryLog.V("Camera", "Captured " + frame.Width + "x" + frame.Height + " (" + frame.JpegBytes.Length / 1024 + " KB) via " + _active.Name + " [" + trigger + "]");
                }
                else WintryLog.W("Camera", "Capture returned no frame");
                return frame;
            }
            finally
            {
                IsCapturing = false;
                PublishIndicator(false);
                _gate.Release();
            }
        }

        private static void PublishIndicator(bool camera)
        {
            WintryEvents.PublishOnMain(new PrivacyIndicatorEvent { CameraActive = camera, MicrophoneActive = PrivacyState.MicrophoneActive, CloudActive = PrivacyState.CloudActive });
            PrivacyState.CameraActive = camera;
        }

        public void Shutdown()
        {
            foreach (var p in _providers) { try { p.Shutdown(); } catch { } }
            _active = null;
        }
    }

    /// <summary>Shared flags for the privacy indicators.</summary>
    public static class PrivacyState
    {
        public static bool CameraActive;
        public static bool MicrophoneActive;
        public static bool CloudActive;

        public static void SetCloud(bool active)
        {
            CloudActive = active;
            WintryEvents.PublishOnMain(new PrivacyIndicatorEvent { CameraActive = CameraActive, MicrophoneActive = MicrophoneActive, CloudActive = active });
        }

        public static void SetMicrophone(bool active)
        {
            MicrophoneActive = active;
            WintryEvents.PublishOnMain(new PrivacyIndicatorEvent { CameraActive = CameraActive, MicrophoneActive = active, CloudActive = CloudActive });
        }
    }

    /// <summary>Downscale + JPEG encode helper shared by providers.</summary>
    public static class FrameEncoder
    {
        public static byte[] EncodeJpeg(Texture2D source, int maxWidth, int quality, out int outW, out int outH)
        {
            Texture2D tex = source;
            bool temp = false;
            if (source.width > maxWidth)
            {
                float scale = (float)maxWidth / source.width;
                outW = maxWidth; outH = Mathf.RoundToInt(source.height * scale);
                var rt = RenderTexture.GetTemporary(outW, outH, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex = new Texture2D(outW, outH, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                temp = true;
            }
            else { outW = source.width; outH = source.height; }
            byte[] jpeg = tex.EncodeToJPG(Mathf.Clamp(quality, 30, 95));
            if (temp) UnityEngine.Object.Destroy(tex);
            return jpeg;
        }
    }
}
