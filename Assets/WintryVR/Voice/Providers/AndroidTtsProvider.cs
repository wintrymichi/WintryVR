using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Voice.Providers
{
    /// <summary>
    /// android.speech.tts.TextToSpeech via JNI. The voice is played by the OS (not through our spatial
    /// AudioSource), so it is a non-spatial fallback used only when no cloud TTS is configured and the
    /// device actually has a TTS engine (Quest headsets generally do not ship one).
    /// </summary>
    public class AndroidTtsProvider : ITextToSpeechProvider
    {
        public string Name => "AndroidTTS";
        public bool RequiresNetwork => false;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _tts;
        private bool _initTried;
        private bool _ready;

        public bool IsConfigured
        {
            get
            {
                if (!_initTried)
                {
                    _initTried = true;
                    try
                    {
                        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        {
                            _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, null);
                            // getEngines() is non-empty only when an engine exists
                            using (var engines = _tts.Call<AndroidJavaObject>("getEngines"))
                                _ready = engines != null && engines.Call<int>("size") > 0;
                        }
                    }
                    catch (System.Exception ex) { WintryLog.W("Voice", "Android TTS unavailable: " + ex.Message); _ready = false; }
                    WintryLog.I("Voice", "Android TTS engine available: " + _ready);
                }
                return _ready;
            }
        }

        public async Task<TtsResult> SynthesizeAsync(string text, string languageCode, string voice, float speed, CancellationToken ct)
        {
            if (!IsConfigured) return new TtsResult { Error = "no engine" };
            try
            {
                using (var locale = new AndroidJavaObject("java.util.Locale", string.IsNullOrEmpty(languageCode) || languageCode == "auto" ? "en" : languageCode))
                    _tts.Call<int>("setLanguage", locale);
                _tts.Call<int>("setSpeechRate", speed);
                _tts.Call<int>("speak", text, 0, null, "wintry");
                float est = Mathf.Max(0.8f, MockTtsProvider.CountSyllables(text) * 0.17f / Mathf.Max(0.5f, speed));
                await Task.Delay((int)(est * 1000), ct);
                return new TtsResult { Success = true, SpokenByProvider = true, DurationSeconds = est };
            }
            catch (System.Exception ex) { return new TtsResult { Error = ex.Message }; }
        }

        public void Stop() { try { _tts?.Call<int>("stop"); } catch { } }
#else
        public bool IsConfigured => false;
        public Task<TtsResult> SynthesizeAsync(string text, string languageCode, string voice, float speed, CancellationToken ct)
            => Task.FromResult(new TtsResult { Error = "Android only" });
        public void Stop() { }
#endif
    }
}
