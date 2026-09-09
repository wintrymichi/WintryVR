using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Capture.Providers
{
    /// <summary>
    /// Renders the main camera to a texture. On a headset with passthrough this only contains VIRTUAL content
    /// (the passthrough composition happens outside Unity), so it is a development/editor fallback and a way
    /// to analyse demo scenes. Documented limitation: it is not a substitute for the Passthrough Camera API.
    /// </summary>
    public class RenderCaptureProvider : ICameraCaptureProvider
    {
        private readonly Func<UnityEngine.Camera> _cameraProvider;
        private readonly int _maxWidth;
        private readonly int _quality;

        public RenderCaptureProvider(Func<UnityEngine.Camera> cameraProvider, int maxWidth, int quality)
        {
            _cameraProvider = cameraProvider; _maxWidth = maxWidth; _quality = quality;
        }

        public string Name => "RenderCapture";
        public bool RequiresPermission => false;
        public bool IsAvailable => true;
        public Task<bool> InitializeAsync() => Task.FromResult(_cameraProvider() != null);

        public Task<CapturedFrame> CaptureAsync(CancellationToken ct)
        {
            return MainThreadDispatcher.RunAsync(() =>
            {
                var cam = _cameraProvider();
                if (cam == null) return null;
                int w = Mathf.Min(_maxWidth, 1024);
                int h = Mathf.RoundToInt(w / Mathf.Max(0.5f, cam.aspect));
                var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
                var prevTarget = cam.targetTexture;
                var prevActive = RenderTexture.active;
                var prevClear = cam.clearFlags; var prevBg = cam.backgroundColor;
                cam.targetTexture = rt;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.2f, 0.22f, 1f);
                cam.Render();
                cam.targetTexture = prevTarget; cam.clearFlags = prevClear; cam.backgroundColor = prevBg;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply(false);
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
                byte[] jpeg = tex.EncodeToJPG(_quality);
                UnityEngine.Object.Destroy(tex);
                return new CapturedFrame
                {
                    JpegBytes = jpeg, Width = w, Height = h, CapturedAt = DateTime.UtcNow,
                    CameraPose = new Pose(cam.transform.position, cam.transform.rotation),
                    VerticalFovDegrees = cam.fieldOfView,
                    HorizontalFovDegrees = 2f * Mathf.Atan(Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect) * Mathf.Rad2Deg,
                    Source = "render"
                };
            });
        }

        public void Shutdown() { }
    }
}
