using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WintryVR.Core
{
    public enum AIRole { System, User, Assistant }

    /// <summary>A content part of a multimodal message: text or a JPEG image.</summary>
    public class AIContentPart
    {
        public string Text;
        public byte[] ImageJpeg;
        public bool IsImage => ImageJpeg != null && ImageJpeg.Length > 0;
        public static AIContentPart FromText(string t) => new AIContentPart { Text = t };
        public static AIContentPart FromJpeg(byte[] jpeg) => new AIContentPart { ImageJpeg = jpeg };
    }

    public class AIMessage
    {
        public AIRole Role;
        public List<AIContentPart> Parts = new List<AIContentPart>();
        public static AIMessage Text(AIRole role, string text)
        {
            var m = new AIMessage { Role = role };
            m.Parts.Add(AIContentPart.FromText(text));
            return m;
        }
        public string PlainText
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                foreach (var p in Parts) if (!p.IsImage && p.Text != null) sb.Append(p.Text);
                return sb.ToString();
            }
        }
    }

    public class AIRequest
    {
        public string SystemPrompt = "";
        public List<AIMessage> Messages = new List<AIMessage>();
        public int MaxTokens = 1024;
        public float Temperature = 0.4f;
        public bool ExpectJson;          // provider may enable JSON mode when supported
        public string ModelOverride;     // optional per-request model
        public bool AllowThinking;       // slower, deeper answers (used for COMPARE/EXPLAIN)
    }

    public class AIResponse
    {
        public bool Success;
        public string Text = "";
        public string Error = "";         // technical, for logs only
        public string ProviderName = "";
        public string Model = "";
        public int InputTokens;
        public int OutputTokens;
        public float LatencyMs;
        public bool Refused;              // model/provider declined (safety)
    }

    /// <summary>Abstraction over any multimodal LLM backend.</summary>
    public interface IAIProvider
    {
        string Name { get; }
        string Model { get; }
        bool SupportsVision { get; }
        bool RequiresNetwork { get; }
        bool IsConfigured { get; }
        Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken ct);
    }
}
