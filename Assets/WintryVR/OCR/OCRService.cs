using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Networking;
using WintryVR.Settings;

namespace WintryVR.OCR
{
    /// <summary>
    /// OCR through the multimodal provider (signs, books, menus, documents, packaging, labels, screens).
    /// Returns the full text, its language and positioned blocks so translations can be shown next to the text.
    /// </summary>
    public class OCRService : IOCRService
    {
        private readonly IAIProvider _provider;
        public OCRService(IAIProvider provider) { _provider = provider; }
        public bool IsAvailable => _provider != null && _provider.IsConfigured && _provider.SupportsVision && WintrySettings.Current.Vision.OCR;

        public async Task<OcrResult> RecognizeAsync(CapturedFrame frame, string languageHint, CancellationToken ct)
        {
            var result = new OcrResult();
            if (frame == null || !frame.IsValid) { result.UserMessage = Localization.Get("err.noCamera", languageHint); return result; }
            if (!IsAvailable) { result.UserMessage = Localization.Get("err.generic", languageHint); return result; }

            var req = new AIRequest
            {
                SystemPrompt = "You are an OCR engine. Transcribe all legible text in the image exactly, preserving line breaks. Return JSON only: " +
                               "{\"text\": full text, \"source_language\": ISO code, \"blocks\": [{\"text\": line or paragraph, \"x\":0..1,\"y\":0..1,\"w\":0..1,\"h\":0..1}]}. " +
                               "If there is no readable text return {\"text\": \"\"}.",
                MaxTokens = 900,
                Temperature = 0f,
                ExpectJson = true
            };
            var msg = new AIMessage { Role = AIRole.User };
            msg.Parts.Add(AIContentPart.FromText("[ocr] Read the text the user is looking at (centre of the image has priority)."));
            msg.Parts.Add(AIContentPart.FromJpeg(frame.JpegBytes));
            req.Messages.Add(msg);

            var res = await _provider.CompleteAsync(req, ct);
            if (!res.Success) { result.UserMessage = Localization.Get("err.generic", languageHint); return result; }
            var obj = MiniJson.ExtractObject(res.Text);
            result.FullText = (MiniJson.GetString(obj, "text", "") ?? "").Trim();
            result.LanguageCode = MiniJson.GetString(obj, "source_language", "") ?? "";
            var blocks = MiniJson.GetArray(obj, "blocks");
            if (blocks != null)
                foreach (var b in blocks)
                {
                    var tb = new TextBlock { Text = MiniJson.GetString(b, "text", ""), LanguageCode = result.LanguageCode, Confidence = 0.9f };
                    float x = (float)MiniJson.GetNumber(b, "x", -1), y = (float)MiniJson.GetNumber(b, "y", -1);
                    float w = (float)MiniJson.GetNumber(b, "w", 0), h = (float)MiniJson.GetNumber(b, "h", 0);
                    if (x >= 0 && y >= 0) tb.NormalizedBounds = new Rect(x, y, w, h);
                    result.Blocks.Add(tb);
                }
            result.Success = result.FullText.Length > 0;
            if (!result.Success) result.UserMessage = Localization.Get("err.noText", languageHint);
            return result;
        }
    }

    /// <summary>Text translation and language detection through the AI provider.</summary>
    public class TranslationService : ITranslationService
    {
        private readonly IAIProvider _provider;
        public TranslationService(IAIProvider provider) { _provider = provider; }
        public bool IsAvailable => _provider != null && _provider.IsConfigured;

        public async Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken ct)
        {
            var result = new TranslationResult { Original = text, SourceLanguage = sourceLanguage, TargetLanguage = targetLanguage };
            if (string.IsNullOrWhiteSpace(text)) { result.UserMessage = Localization.Get("err.noText", targetLanguage); return result; }
            var req = new AIRequest
            {
                SystemPrompt = "You are a professional translator. Translate the user's text into " + LanguageCodes.DisplayName(targetLanguage) + " (" + targetLanguage + "). Keep formatting and line breaks. Return JSON only: {\"translated\": string, \"source_language\": ISO code}",
                MaxTokens = 800,
                Temperature = 0f,
                ExpectJson = true
            };
            req.Messages.Add(AIMessage.Text(AIRole.User, "[translate] " + text));
            var res = await _provider.CompleteAsync(req, ct);
            if (!res.Success) { result.UserMessage = Localization.Get("err.generic", targetLanguage); return result; }
            var obj = MiniJson.ExtractObject(res.Text);
            result.Translated = MiniJson.GetString(obj, "translated", res.Text) ?? "";
            string src = MiniJson.GetString(obj, "source_language", "");
            if (!string.IsNullOrEmpty(src)) result.SourceLanguage = src;
            result.Success = !string.IsNullOrEmpty(result.Translated);
            return result;
        }

        public async Task<string> DetectLanguageAsync(string text, CancellationToken ct)
        {
            var req = new AIRequest { SystemPrompt = "Detect the language of the text. Return JSON only: {\"language\": ISO 639-1 code}", MaxTokens = 20, Temperature = 0f, ExpectJson = true };
            req.Messages.Add(AIMessage.Text(AIRole.User, text));
            var res = await _provider.CompleteAsync(req, ct);
            if (!res.Success) return "";
            return MiniJson.GetString(MiniJson.ExtractObject(res.Text), "language", "") ?? "";
        }
    }
}
