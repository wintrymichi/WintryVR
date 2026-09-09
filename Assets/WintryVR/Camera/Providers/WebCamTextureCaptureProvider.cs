using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Capture.Providers
{
    /// <summary>
    /// Captures frames from the headset's forward passthrough cameras through Unity's WebCamTexture.
    /// On Meta Quest 3/3S this is the Passthrough Camera API (Horizon OS v74+, needs the
    /// horizonos.permission.HEADSET_CAMERA permission and android.permission.CAMERA). The camera pose used
    /// for image→world projection is approximated from the head pose at capture time, which is what the
    /// spatial localiser needs; exact per-camera intrinsics can be layered on top by a platform probe.
    /// On desktop it falls back to any webcam, which is useful for development.
    /// </summary>
    public class WebCamTextureCaptureProvider : ICameraCaptureProvider
    {
        private WebCamTexture _webcam;
        private Texture2D _readback;
        private readonly Func<Transform> _headProvider;
        private readonly int _maxWidth;
        private readonly int _quality;

        public WebCamTextureCaptureProvider(Func<Transform> headProvider, int maxWidth, int quality)
        {
            _headProvider = headProvider; _maxWidth = maxWidth; _quality = quality;
        }

        public string Name => "PassthroughCamera(WebCamTexture)";
        public bool RequiresPermission => true;
        public bool IsAvailable => WebCamTexture.devices != null && WebCamTexture.devices.Length > 0;
        public static string DeviceNameHint = "";  // set by platform probe if it knows the left/right camera names

        public async Task<bool> InitializeAsync()
        {
            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0) return false;
            string chosen = devices[0].name;
            foreach (var d in devices)
            {
                string n = d.name.ToLowerInvariant();
                if (!string.IsNullOrEmpty(DeviceNameHint) && n.Contains(DeviceNameHint.ToLowerInvariant())) { chosen = d.name; break; }
                if (!d.isFrontFacing) chosen = d.name; // forward-facing headset cameras report as back cameras
            }
            _webcam = new WebCamTexture(chosen, 1280, 960, 15);
            _webcam.Play();
            // Wait for the first real frame (a few frames on device)
            float deadline = Time.realtimeSinceStartup + 4f;
            while (_webcam.width <= 16 && Time.realtimeSinceStartup < deadline) await Task.Yield();
            bool ok = _webcam.isPlaying && _webcam.width > 16;
            WintryLog.I("Camera", ok ? "WebCamTexture started: " + chosen + " " + _webcam.width + "x" + _webcam.height : "WebCamTexture failed to start: " + chosen);
            if (!ok) { _webcam.Stop(); _webcam = null; }
            else { _webcam.Pause(); } // keep the device warm but do not stream continuously
            return ok;
        }

        public async Task<CapturedFrame> CaptureAsync(CancellationToken ct)
        {
            if (_webcam == null) return null;
            return await MainThreadDispatcher.RunAsync(async () =>
            {
                _webcam.Play();
                // let a fresh frame arrive
                int frames = 0;
                while (!_webcam.didUpdateThisFrame && frames++ < 30) await Task.Yield();
                if (_readback == null || _readback.width != _webcam.width || _readback.height != _webcam.height)
                    _readback = new Texture2D(_webcam.width, _webcam.height, TextureFormat.RGB24, false);
                _readback.SetPixels32(_webcam.GetPixels32());
                _readback.Apply(false);
                _webcam.Pause();

                var head = _headProvider != null ? _headProvider() : null;
                var frame = new CapturedFrame
                {
                    JpegBytes = FrameEncoder.EncodeJpeg(_readback, _maxWidth, _quality, out int w, out int h),
                    Width = w, Height = h,
                    CapturedAt = DateTime.UtcNow,
                    CameraPose = head != null ? new Pose(head.position, head.rotation) : Pose.identity,
                    Source = "passthrough-camera",
                    HorizontalFovDegrees = 80f, VerticalFovDegrees = 62f  // approximate Quest 3 passthrough camera FOV
                };
                return frame;
            }).Unwrap();
        }

        public void Shutdown()
        {
            if (_webcam != null) { _webcam.Stop(); _webcam = null; }
            if (_readback != null) { UnityEngine.Object.Destroy(_readback); _readback = null; }
        }
    }
}
