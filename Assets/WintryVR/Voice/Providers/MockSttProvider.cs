using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WintryVR.Core;

namespace WintryVR.Voice.Providers
{
    /// <summary>
    /// Demo transcription: cycles through a scripted conversation so the pipeline can be driven without a
    /// real recogniser. The demo controller can also inject explicit utterances.
    /// </summary>
    public class MockSttProvider : ISpeechToTextProvider
    {
        public string Name => "MockSTT";
        public bool RequiresNetwork => false;
        public bool IsConfigured => true;
        public string LanguageCode = "en";

        private readonly Queue<string> _scripted = new Queue<string>();
        private int _index;
        private static readonly string[] DefaultScript =
        {
            "Hey Wintry, what am I looking at?",
            "How much does it cost?",
            "Read this.",
            "Translate it into Italian.",
            "Where is my phone?",
            "How many chairs are there?",
            "What's on the table?",
            "Thanks Wintry, that's all."
        };

        public void Inject(string utterance) => _scripted.Enqueue(utterance);

        public async Task<SttResult> TranscribeAsync(float[] samples, int sampleRate, string languageHint, CancellationToken ct)
        {
            await Task.Delay(250, ct);
            string text = _scripted.Count > 0 ? _scripted.Dequeue() : DefaultScript[_index++ % DefaultScript.Length];
            return new SttResult { Success = true, Text = text, LanguageCode = LanguageCode, Confidence = 0.95f };
        }
    }
}
