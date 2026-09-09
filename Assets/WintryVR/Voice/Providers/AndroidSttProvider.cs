using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Voice.Providers
{
    /// <summary>
    /// On-device recognition through Android's SpeechRecognizer. Meta Quest ships without Google speech
    /// services, so this reports itself as unavailable there and is used only when the platform actually
    /// offers a recogniser (checked at runtime with SpeechRecognizer.isRecognitionAvailable).
    /// It is push-to-talk style: it records via the OS, not from our sample buffer, so the samples are ignored.
    /// </summary>
    public class AndroidSttProvider : ISpeechToTextProvider
    {
        public string Name => "AndroidSTT";
        public bool RequiresNetwork => false;
        private bool? _available;

        public bool IsConfigured
        {
            get
            {
                if (_available.HasValue) return _available.Value;
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var recognizer = new AndroidJavaClass("android.speech.SpeechRecognizer"))
                        _available = recognizer.CallStatic<bool>("isRecognitionAvailable", activity);
                }
                catch { _available = false; }
#else
                _available = false;
#endif
                WintryLog.I("Voice", "Android SpeechRecognizer available: " + _available.Value);
                return _available.Value;
            }
        }

        public Task<SttResult> TranscribeAsync(float[] samples, int sampleRate, string languageHint, CancellationToken ct)
        {
            // Driving the Android intent-based recogniser requires an Activity result callback; without a
            // platform plugin we cannot receive it, so we honestly report failure and let the service fall back.
            return Task.FromResult(new SttResult { Success = false, Error = "Android SpeechRecognizer needs the WintryVR Android plugin (not bundled)" });
        }
    }
}
