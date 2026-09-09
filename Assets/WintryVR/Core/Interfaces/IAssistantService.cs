using System.Threading;
using System.Threading.Tasks;

namespace WintryVR.Core
{
    /// <summary>Context handed to the assistant for one turn.</summary>
    public class AssistantContext
    {
        public string UserText;
        public Intent Intent;
        public string LanguageCode = "auto";
        public CapturedFrame Frame;                // optional
        public VisionResult Vision;                // optional (pre-analysis)
        public OcrResult Ocr;                      // optional
        public SearchResult Search;                // optional
        public string SpatialContext = "";         // textual scene graph + user pose
        public string MemoryContext = "";          // recent objects & conversation
        public bool Online = true;
        public string TargetLanguageForTranslation = "";
    }

    /// <summary>The "brain": turns a request + context into a spoken answer and optional structured data.</summary>
    public interface IAssistantService
    {
        IAIProvider Provider { get; }
        Task<StructuredAnswer> AskAsync(AssistantContext context, CancellationToken ct);
        Task<Intent> ClassifyIntentAsync(string text, string languageCode, CancellationToken ct);
    }
}
