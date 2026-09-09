using System.Threading;
using System.Threading.Tasks;

namespace WintryVR.Core
{
    public interface IVisionService
    {
        bool IsAvailable { get; }
        /// <summary>Analyse a frame. Optional hint focuses the analysis ("what is at the centre", "list every object").</summary>
        Task<VisionResult> AnalyzeAsync(CapturedFrame frame, string hint, string languageCode, CancellationToken ct);
    }

    public interface ICameraCaptureProvider
    {
        string Name { get; }
        bool IsAvailable { get; }
        bool RequiresPermission { get; }
        Task<bool> InitializeAsync();
        Task<CapturedFrame> CaptureAsync(CancellationToken ct);
        void Shutdown();
    }

    public enum CaptureTrigger { VoiceCommand, Gaze, ObjectSelection, SceneChange, LowConfidenceRetry, Manual }

    public interface ICameraCaptureService
    {
        bool IsCapturing { get; }
        ICameraCaptureProvider ActiveProvider { get; }
        Task<CapturedFrame> CaptureAsync(CaptureTrigger trigger, CancellationToken ct);
        CapturedFrame LastFrame { get; }
    }
}
