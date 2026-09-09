using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WintryVR.Core
{
    public class SttResult
    {
        public bool Success;
        public string Text = "";
        public string LanguageCode = "";
        public float Confidence = 1f;
        public string Error = "";
    }

    public interface ISpeechToTextProvider
    {
        string Name { get; }
        bool RequiresNetwork { get; }
        bool IsConfigured { get; }
        /// <summary>Transcribe 16-bit PCM mono audio.</summary>
        Task<SttResult> TranscribeAsync(float[] samples, int sampleRate, string languageHint, CancellationToken ct);
    }

    public class TtsResult
    {
        public bool Success;
        public AudioClip Clip;          // null when the provider speaks by itself (e.g. Android TTS)
        public bool SpokenByProvider;   // true when audio was already played by the provider
        public float DurationSeconds;
        public string Error = "";
    }

    public interface ITextToSpeechProvider
    {
        string Name { get; }
        bool RequiresNetwork { get; }
        bool IsConfigured { get; }
        Task<TtsResult> SynthesizeAsync(string text, string languageCode, string voice, float speed, CancellationToken ct);
        void Stop();
    }

    public interface IVoiceService
    {
        bool IsListening { get; }
        bool IsSpeaking { get; }
        bool MicrophoneAvailable { get; }
        bool WakeWordEnabled { get; set; }
        bool ContinuousConversation { get; set; }
        ISpeechToTextProvider SttProvider { get; }
        ITextToSpeechProvider TtsProvider { get; }
        float CurrentInputLevel { get; }

        /// <summary>Start a listening session (wake word already consumed or push-to-talk pressed).</summary>
        void StartListening();
        void StopListening();
        Task SpeakAsync(string text, string languageCode, CancellationToken ct);
        /// <summary>Interrupt current speech (barge-in).</summary>
        void Interrupt();
        event Action<string, string> OnUtterance; // text, language
    }
}
