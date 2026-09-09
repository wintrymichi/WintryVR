using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Networking;
using WintryVR.Settings;

namespace WintryVR.AI
{
    /// <summary>
    /// Builds the multimodal prompt (spatial context + memory + frame), asks the provider for a structured
    /// JSON answer and converts it into a <see cref="StructuredAnswer"/>. Provider-agnostic.
    /// </summary>
    public class AssistantService : IAssistantService
    {
        private readonly IAIProvider _provider;
        public IAIProvider Provider => _provider;

        public AssistantService(IAIProvider provider) { _provider = provider; }

        public async Task<Intent> ClassifyIntentAsync(string text, string languageCode, CancellationToken ct)
        {
            var req = new AIRequest
            {
                SystemPrompt = "Classify the user's request for a mixed-reality assistant. Respond with JSON only: {\"intent\": ONE_OF[IDENTIFY, DESCRIBE, OCR, TRANSLATE, SEARCH, LOCATE, COMPARE, EXPLAIN, COUNT, NAVIGATE, GENERAL_CONVERSATION]}",
                MaxTokens = 64,
                Temperature = 0f,
                ExpectJson = true
            };
            req.Messages.Add(AIMessage.Text(AIRole.User, "[classify] " + text));
            var res = await _provider.CompleteAsync(req, ct);
            if (!res.Success) return Intent.GENERAL_CONVERSATION;
            var obj = MiniJson.ExtractObject(res.Text);
            string name = MiniJson.GetString(obj, "intent", "GENERAL_CONVERSATION");
            return Enum.TryParse(name, true, out Intent i) ? i : Intent.GENERAL_CONVERSATION;
        }

        public async Task<StructuredAnswer> AskAsync(AssistantContext ctx, CancellationToken ct)
        {
            var settings = WintrySettings.Current;
            var req = new AIRequest
            {
                SystemPrompt = BuildSystemPrompt(ctx, settings),
                MaxTokens = settings.AI.ResponseLength == ResponseLength.Short ? 400 : settings.AI.ResponseLength == ResponseLength.Long ? 1200 : 700,
                Temperature = Mathf.Clamp01(settings.AI.Creativity),
                ExpectJson = true,
                AllowThinking = ctx.Intent == Intent.COMPARE || ctx.Intent == Intent.EXPLAIN
            };

            var user = new AIMessage { Role = AIRole.User };
            var sb = new StringBuilder();
            if (ctx.Search != null && ctx.Search.Success) sb.Append("[search-summary] ");
            sb.Append("Intent: ").Append(ctx.Intent).Append('\n');
            if (!string.IsNullOrEmpty(ctx.MemoryContext)) sb.Append("Context memory:\n").Append(ctx.MemoryContext).Append('\n');
            if (!string.IsNullOrEmpty(ctx.SpatialContext)) sb.Append("Spatial context:\n").Append(ctx.SpatialContext).Append('\n');
            if (ctx.Vision != null && ctx.Vision.Success)
            {
                sb.Append("Pre-analysis of the current view: ").Append(ctx.Vision.SceneSummary).Append('\n');
                foreach (var d in ctx.Vision.Detections)
                    sb.Append("- ").Append(d.Identity ?? d.Label).Append(" (").Append(d.Label).Append(", ").Append(Mathf.RoundToInt(d.Confidence * 100)).Append("%)\n");
            }
            if (ctx.Ocr != null && ctx.Ocr.Success) sb.Append("Text read from the view (").Append(ctx.Ocr.LanguageCode).Append("):\n").Append(ctx.Ocr.FullText).Append('\n');
            if (ctx.Search != null && ctx.Search.Success)
            {
                sb.Append("Web search results for \"").Append(ctx.Search.Query).Append("\":\n");
                int n = 0;
                foreach (var it in ctx.Search.Items)
                {
                    sb.Append("- ").Append(it.Title).Append(" | ").Append(it.Snippet);
                    if (!string.IsNullOrEmpty(it.Price)) sb.Append(" | price: ").Append(it.Price);
                    sb.Append(" | ").Append(it.Source ?? it.Url).Append('\n');
                    if (++n >= 6) break;
                }
            }
            if (!string.IsNullOrEmpty(ctx.TargetLanguageForTranslation)) sb.Append("Translate into: ").Append(ctx.TargetLanguageForTranslation).Append('\n');
            sb.Append("User says: ").Append(ctx.UserText);
            user.Parts.Add(AIContentPart.FromText(sb.ToString()));
            if (ctx.Frame != null && ctx.Frame.IsValid && _provider.SupportsVision) user.Parts.Add(AIContentPart.FromJpeg(ctx.Frame.JpegBytes));
            req.Messages.Add(user);

            var res = await _provider.CompleteAsync(req, ct);
            var answer = new StructuredAnswer { LanguageCode = ctx.LanguageCode };
            if (!res.Success)
            {
                if (res.Refused) answer.Speech = Localization.Get("err.identify", ctx.LanguageCode);
                else answer.Speech = ctx.Online ? Localization.Get("err.generic", ctx.LanguageCode) : Localization.Get("err.offline", ctx.LanguageCode);
                WintryLog.W("Assistant", "Provider failed: " + res.Error);
                return answer;
            }
            Parse(res.Text, answer);
            if (string.IsNullOrEmpty(answer.LanguageCode) || answer.LanguageCode == "auto") answer.LanguageCode = ctx.LanguageCode;
            answer.Speech = ResponseSafety.ApplyConfidence(answer.Speech, answer.Confidence, answer.LanguageCode);
            answer.Speech = ResponseSafety.AppendDisclaimers(answer.Speech, ctx.UserText, answer.LanguageCode);
            return answer;
        }

        private static string BuildSystemPrompt(AssistantContext ctx, WintrySettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are Wintry, a spatial AI assistant living in the user's mixed-reality headset. You see what the user sees through the attached camera frame and you know where things are in the room.");
            sb.AppendLine("Be concise, natural and warm; you are speaking aloud. No markdown, no lists in speech. Prefer one to three short sentences unless the user asks for detail.");
            sb.AppendLine("Never invent facts. If you are unsure, say so and give your confidence. Do not present uncertain identifications as certain.");
            sb.AppendLine("For medical, legal or safety questions give general information only and avoid diagnoses or dangerous instructions.");
            sb.AppendLine("Resolve references like 'it', 'that one', 'the one next to it' using the context memory (object IN FOCUS is the default referent).");
            sb.AppendLine("Reply in the user's language. Reply language: " + (ctx.LanguageCode == "auto" || string.IsNullOrEmpty(ctx.LanguageCode) ? "same as the user's message" : ctx.LanguageCode));
            sb.AppendLine("Respond with a single JSON object and nothing else:");
            sb.AppendLine("{");
            sb.AppendLine("  \"speech\": string (what you say aloud),");
            sb.AppendLine("  \"language\": ISO code of the speech,");
            sb.AppendLine("  \"object_name\": string (specific identity if you identified an object, else empty),");
            sb.AppendLine("  \"object_category\": string,");
            sb.AppendLine("  \"confidence\": number 0..1 for the identification (omit if not applicable),");
            sb.AppendLine("  \"focus_x\": number 0..1, \"focus_y\": number 0..1 (image position of the main object, origin top-left; omit if none),");
            sb.AppendLine("  \"card_title\": string, \"card_subtitle\": string, \"card_lines\": [up to 4 short facts] (only when a floating info card helps),");
            sb.AppendLine("  \"needs_search\": boolean (true when the answer needs live web data such as prices or availability that you do not have), \"search_query\": string,");
            sb.AppendLine("  \"detected_text\": string, \"translated\": string, \"source_language\": string (for reading/translation requests)");
            sb.AppendLine("}");
            if (!ctx.Online) sb.AppendLine("The device is OFFLINE: do not request search; say you cannot access live information if needed.");
            if (settings.AI.ResponseLength == ResponseLength.Short) sb.AppendLine("Keep speech under 25 words.");
            return sb.ToString();
        }

        public static void Parse(string text, StructuredAnswer a)
        {
            var obj = MiniJson.ExtractObject(text);
            if (obj == null)
            {
                a.Speech = (text ?? "").Trim().Trim('`');
                return;
            }
            a.Speech = MiniJson.GetString(obj, "speech", "") ?? "";
            a.LanguageCode = MiniJson.GetString(obj, "language", a.LanguageCode);
            a.ObjectName = MiniJson.GetString(obj, "object_name", "") ?? "";
            a.ObjectCategory = MiniJson.GetString(obj, "object_category", "") ?? "";
            a.Confidence = (float)MiniJson.GetNumber(obj, "confidence", -1);
            if (obj.ContainsKey("focus_x") && obj.ContainsKey("focus_y"))
            {
                a.FocusPoint = new Vector2((float)MiniJson.GetNumber(obj, "focus_x", 0.5), (float)MiniJson.GetNumber(obj, "focus_y", 0.5));
                a.HasFocusPoint = true;
            }
            a.CardTitle = MiniJson.GetString(obj, "card_title", "") ?? "";
            a.CardSubtitle = MiniJson.GetString(obj, "card_subtitle", "") ?? "";
            var lines = MiniJson.GetArray(obj, "card_lines");
            if (lines != null) foreach (var l in lines) if (l != null) a.CardLines.Add(l.ToString());
            a.NeedsSearch = MiniJson.GetBool(obj, "needs_search");
            a.SearchQuery = MiniJson.GetString(obj, "search_query", "") ?? "";
            a.DetectedText = MiniJson.GetString(obj, "detected_text", "") ?? "";
            a.TranslatedText = MiniJson.GetString(obj, "translated", "") ?? "";
            a.SourceLanguage = MiniJson.GetString(obj, "source_language", "") ?? "";
            if (string.IsNullOrEmpty(a.Speech) && !string.IsNullOrEmpty(a.TranslatedText)) a.Speech = a.TranslatedText;
        }
    }
}
