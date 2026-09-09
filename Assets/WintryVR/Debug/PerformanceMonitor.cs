using UnityEngine;
using UnityEngine.Profiling;

namespace WintryVR.Diagnostics
{
    /// <summary>FPS / frame time / memory / battery sampling for the debug panel and adaptive quality.</summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public float Fps { get; private set; }
        public float FrameMs { get; private set; }
        public float CpuMs { get; private set; }     // main-thread time (approximation)
        public float GpuMs { get; private set; }     // from FrameTimingManager when available
        public long AllocatedMB => Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        public long ReservedMB => Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
        public float Battery => SystemInfo.batteryLevel;
        public BatteryStatus BatteryStatus => SystemInfo.batteryStatus;
        public int DroppedFramesLastSecond { get; private set; }

        private float _accum; private int _frames; private int _dropped;
        private readonly FrameTiming[] _timings = new FrameTiming[1];

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _accum += dt; _frames++;
            if (dt > 1f / 60f) _dropped++;
            if (_accum >= 1f)
            {
                Fps = _frames / _accum; FrameMs = 1000f * _accum / _frames; DroppedFramesLastSecond = _dropped;
                _accum = 0f; _frames = 0; _dropped = 0;
            }
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timings) > 0)
            {
                CpuMs = (float)_timings[0].cpuFrameTime;
                GpuMs = (float)_timings[0].gpuFrameTime;
            }
            else CpuMs = FrameMs;
        }
    }
}
