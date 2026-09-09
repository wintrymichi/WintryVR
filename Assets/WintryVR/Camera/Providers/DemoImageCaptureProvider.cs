using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Capture.Providers
{
    /// <summary>
    /// Demo Mode source: returns a procedurally drawn "desk" image (or an image from Resources/WintryDemo if
    /// present) so the whole vision pipeline can run without a headset or camera.
    /// </summary>
    public class DemoImageCaptureProvider : ICameraCaptureProvider
    {
        private readonly Func<Transform> _headProvider;
        private byte[] _jpeg;
        private int _w = 640, _h = 480;

        public DemoImageCaptureProvider(Func<Transform> headProvider) { _headProvider = headProvider; }
        public string Name => "DemoImage";
        public bool RequiresPermission => false;
        public bool IsAvailable => true;

        public Task<bool> InitializeAsync()
        {
            return MainThreadDispatcher.RunAsync(() =>
            {
                var res = Resources.Load<Texture2D>("WintryDemo/desk");
                Texture2D tex = res != null ? res : DrawSyntheticDesk(_w, _h);
                _w = tex.width; _h = tex.height;
                _jpeg = tex.EncodeToJPG(80);
                if (res == null) UnityEngine.Object.Destroy(tex);
                return true;
            });
        }

        public Task<CapturedFrame> CaptureAsync(CancellationToken ct)
        {
            var head = _headProvider != null ? _headProvider() : null;
            return Task.FromResult(new CapturedFrame
            {
                JpegBytes = _jpeg, Width = _w, Height = _h, CapturedAt = DateTime.UtcNow,
                CameraPose = head != null ? new Pose(head.position, head.rotation) : Pose.identity,
                Source = "demo"
            });
        }

        public void Shutdown() { }

        private static Texture2D DrawSyntheticDesk(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fy = (float)y / h;
                    Color c = fy < 0.45f ? Color.Lerp(new Color(0.55f, 0.4f, 0.28f), new Color(0.45f, 0.32f, 0.22f), fy / 0.45f) : new Color(0.85f, 0.86f, 0.88f);
                    px[y * w + x] = c;
                }
            FillRect(px, w, h, 0.42f, 0.35f, 0.22f, 0.28f, new Color(0.12f, 0.12f, 0.13f)); // camera body
            FillRect(px, w, h, 0.50f, 0.30f, 0.10f, 0.10f, new Color(0.05f, 0.05f, 0.06f)); // lens
            FillRect(px, w, h, 0.68f, 0.50f, 0.10f, 0.18f, new Color(0.2f, 0.2f, 0.25f));  // phone
            FillRect(px, w, h, 0.15f, 0.55f, 0.20f, 0.20f, new Color(0.9f, 0.85f, 0.6f));  // notebook
            tex.SetPixels32(px); tex.Apply(false);
            return tex;
        }

        private static void FillRect(Color32[] px, int w, int h, float nx, float ny, float nw, float nh, Color c)
        {
            int x0 = (int)(nx * w), y0 = (int)((1f - ny - nh) * h), x1 = (int)((nx + nw) * w), y1 = (int)((1f - ny) * h);
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++) px[y * w + x] = c;
        }
    }
}
