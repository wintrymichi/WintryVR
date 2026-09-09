using System.Threading;
using System.Threading.Tasks;

namespace WintryVR.Core
{
    public interface IOCRService
    {
        bool IsAvailable { get; }
        Task<OcrResult> RecognizeAsync(CapturedFrame frame, string languageHint, CancellationToken ct);
    }

    public interface ITranslationService
    {
        bool IsAvailable { get; }
        Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken ct);
        Task<string> DetectLanguageAsync(string text, CancellationToken ct);
    }
}
