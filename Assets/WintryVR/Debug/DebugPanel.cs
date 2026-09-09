using System.Text;
using UnityEngine;
using WintryVR.AI.Providers;
using WintryVR.Core;
using WintryVR.Networking;
using WintryVR.UI;

namespace WintryVR.Diagnostics
{
    /// <summary>
    /// Hidden debug panel (F1 in the editor, both primary buttons on controllers): FPS, CPU, GPU, memory,
    /// battery, tracking, passthrough, microphone, camera, AI/network latency, scene objects, anchors, model,
    /// provider. Includes log export.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        public PerformanceMonitor Perf;
        public ISpatialService Spatial;
        public System.Func<string> ExtraInfo;   // supplied by the bootstrap: state, providers, capabilities
        private GlassPanel _panel;
        private float _nextRefresh;
        private string _lastExport = "";

        public bool IsOpen => _panel != null;

        public void Toggle()
        {
            if (_panel != null) { _panel.Close(); _panel = null; return; }
            _panel = GlassPanel.Create("DebugPanel", 0.4f, 0.34f, Spatial?.Head, new Color(0.04f, 0.05f, 0.07f));
            _panel.transform.SetParent(transform, false);
            _panel.SetTitle("WintryVR Debug");
            _panel.SetBodyColor(new Color(0.75f, 0.95f, 0.8f));
            _panel.AddButton("Export log", () => { _lastExport = WintryLog.Export(); }, 0.11f);
            _panel.AddButton("Verbose", () => { WintryLog.VerboseEnabled = !WintryLog.VerboseEnabled; }, 0.09f);
            _panel.OnClosed = () => _panel = null;
            _panel.transform.position = Spatial != null ? Spatial.ComfortablePosition(1.0f, 28f, 4f) : Vector3.forward;
        }

        private void Update()
        {
            if (_panel == null || Time.time < _nextRefresh) return;
            _nextRefresh = Time.time + 0.5f;
            _panel.SetBody(Build());
        }

        public string Build()
        {
            var sb = new StringBuilder();
            if (Perf != null)
            {
                sb.Append("FPS ").Append(Perf.Fps.ToString("0")).Append("  frame ").Append(Perf.FrameMs.ToString("0.0")).Append(" ms  cpu ").Append(Perf.CpuMs.ToString("0.0")).Append("  gpu ").Append(Perf.GpuMs.ToString("0.0")).AppendLine();
                sb.Append("RAM ").Append(Perf.AllocatedMB).Append("/").Append(Perf.ReservedMB).Append(" MB  battery ").Append(Perf.Battery < 0 ? "n/a" : Mathf.RoundToInt(Perf.Battery * 100) + "%").Append("  dropped ").Append(Perf.DroppedFramesLastSecond).AppendLine();
            }
            sb.Append("Net ").Append(ConnectivityMonitor.IsOnline ? "online" : "OFFLINE").Append("  http ").Append(HttpClientService.LastLatencyMs.ToString("0")).Append(" ms  inflight ").Append(HttpClientService.InFlight).AppendLine();
            sb.Append("AI ").Append(AIProviderStats.LastProvider).Append('/').Append(AIProviderStats.LastModel).Append("  last ").Append(AIProviderStats.LastLatencyMs.ToString("0")).Append(" ms  avg ").Append(AIProviderStats.AverageLatencyMs.ToString("0")).Append("  req ").Append(AIProviderStats.Requests).Append(" fail ").Append(AIProviderStats.Failures).AppendLine();
            if (ExtraInfo != null) sb.Append(ExtraInfo());
            if (!string.IsNullOrEmpty(_lastExport)) sb.AppendLine("Exported: " + System.IO.Path.GetFileName(_lastExport));
            return sb.ToString();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (_panel == null) return;
            GUI.Label(new Rect(10, 10, 600, 400), Build());
        }
#endif
    }
}
