using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Capture;
using WintryVR.Core;
using WintryVR.Networking;
using WintryVR.Settings;

namespace WintryVR.Voice
{
    /// <summary>
    /// Voice Layer: microphone capture, VAD segmentation, wake word, continuous conversation, speech output
    /// through a spatial AudioSource, barge-in interruption and amplitude events for lip sync.
    ///
    /// Modes:
    ///   • WakeWord   – always listening locally; only utterances that start with "Hey Wintry" (or any
    ///                  utterance while a conversation window is open) are handed to the assistant.
    ///   • PushToTalk – the input layer calls StartListening()/StopListening().
    /// Audio never leaves the device unless an utterance is actually sent to the STT provider.
    /// </summary>
    public class VoiceService : MonoBehaviour, IVoiceService
    {
        public ISpeechToTextProvider SttProvider { get; private set; }
        public ITextToSpeechProvider TtsProvider { get; private set; }
        public AudioSource VoiceSource { get; private set; }

        public bool IsListening { get; private set; }
        public bool IsSpeaking { get; private set; }
        public bool MicrophoneAvailable { get; private set; }
        public bool WakeWordEnabled { get; set; } = true;
        public bool ContinuousConversation { get; set; } = true;
        public float CurrentInputLevel => _vad.CurrentLevel;
        public float ConversationWindowSeconds = 8f;
        public bool ConversationOpen => Time.time < _conversationOpenUntil;
        public string LastError { get; private set; } = "";
        /// <summary>When false (demo mode) the microphone only runs during push-to-talk.</summary>
        public bool AutoStartMicrophone = true;

        public event Action<string, string> OnUtterance;

        private readonly VoiceActivityDetector _vad = new VoiceActivityDetector();
        private AudioClip _micClip;
        private string _micDevice;
        private int _micRate = 16000;
        private int _lastReadPos;
        private readonly List<float> _utterance = new List<float>(16000 * 12);
        private bool _pushToTalkActive;
        private float _conversationOpenUntil;
        private CancellationTokenSource _speakCts;
        private bool _micRunning;
        private float _micRestartAt;
        private readonly float[] _amp = new float[256];
        private int _transcribing;

        private const int MicBufferSeconds = 10;

        public void Configure(ISpeechToTextProvider stt, ITextToSpeechProvider tts, AudioSource voiceSource)
        {
            SttProvider = stt; TtsProvider = tts; VoiceSource = voiceSource;
            var s = WintrySettings.Current;
            WakeWordEnabled = s.Voice.WakeWord; ContinuousConversation = s.Voice.ContinuousConversation;
            WintryEvents.Subscribe<SettingsChangedEvent>(_ => { WakeWordEnabled = WintrySettings.Current.Voice.WakeWord; ContinuousConversation = WintrySettings.Current.Voice.ContinuousConversation; });
            WintryLog.I("Voice", "STT=" + stt.Name + " TTS=" + tts.Name);
        }

        public async void StartMicrophone()
        {
            if (_micRunning) return;
            if (!WintrySettings.Current.Privacy.MicrophoneAllowed) { WintryLog.W("Voice", "Microphone disabled in privacy settings"); return; }
            if (!await PermissionManager.EnsureMicrophoneAsync()) { WintryLog.W("Voice", "Microphone permission denied"); MicrophoneAvailable = false; return; }
            if (Microphone.devices == null || Microphone.devices.Length == 0) { WintryLog.W("Voice", "No microphone device"); MicrophoneAvailable = false; return; }
            _micDevice = Microphone.devices[0];
            Microphone.GetDeviceCaps(_micDevice, out int minFreq, out int maxFreq);
            _micRate = (minFreq == 0 && maxFreq == 0) ? 16000 : Mathf.Clamp(16000, minFreq, maxFreq);
            _micClip = Microphone.Start(_micDevice, true, MicBufferSeconds, _micRate);
            _lastReadPos = 0; _micRunning = _micClip != null; MicrophoneAvailable = _micRunning;
            _vad.Reset();
            PrivacyState.SetMicrophone(_micRunning);
            WintryLog.I("Voice", _micRunning ? "Microphone started: " + _micDevice + " @" + _micRate : "Microphone failed to start");
        }

        public void StopMicrophone()
        {
            if (!_micRunning) return;
            Microphone.End(_micDevice);
            _micRunning = false;
            PrivacyState.SetMicrophone(false);
        }

        public void StartListening()
        {
            _pushToTalkActive = true;
            IsListening = true;
            _utterance.Clear();
            _vad.Reset();
            if (IsSpeaking) Interrupt();
            if (!_micRunning) StartMicrophone();
        }

        public void StopListening()
        {
            if (!_pushToTalkActive) return;
            _pushToTalkActive = false;
            IsListening = false;
            if (_utterance.Count > _micRate / 4) _ = TranscribeAsync(_utterance.ToArray(), true);
            _utterance.Clear();
        }

        /// <summary>Opens the follow-up window after Wintry has spoken (continuous conversation).</summary>
        public void OpenConversationWindow(float seconds = -1f)
        {
            _conversationOpenUntil = Time.time + (seconds < 0 ? ConversationWindowSeconds : seconds);
        }

        public void CloseConversationWindow() { _conversationOpenUntil = 0f; }

        private void Update()
        {
            if (!_micRunning || _micClip == null)
            {
                if (AutoStartMicrophone && WintrySettings.Current.Privacy.MicrophoneAllowed && Time.time > _micRestartAt) { _micRestartAt = Time.time + 5f; StartMicrophone(); }
                return;
            }
            int pos = Microphone.GetPosition(_micDevice);
            if (pos < 0 || pos == _lastReadPos) return;
            int length = pos > _lastReadPos ? pos - _lastReadPos : (_micClip.samples - _lastReadPos) + pos;
            if (length <= 0) return;
            var chunk = new float[length];
            if (pos > _lastReadPos) _micClip.GetData(chunk, _lastReadPos);
            else
            {
                var a = new float[_micClip.samples - _lastReadPos]; _micClip.GetData(a, _lastReadPos);
                var b = new float[pos]; if (pos > 0) _micClip.GetData(b, 0);
                Array.Copy(a, 0, chunk, 0, a.Length); Array.Copy(b, 0, chunk, a.Length, b.Length);
            }
            _lastReadPos = pos;

            // Ignore our own voice while speaking unless barge-in is armed (loud speech over TTS)
            var evt = _vad.Process(chunk, length / (float)_micRate);
            bool armed = _pushToTalkActive || WakeWordEnabled || ConversationOpen;
            if (!armed) return;

            if (_pushToTalkActive)
            {
                _utterance.AddRange(chunk);
                return;
            }

            switch (evt)
            {
                case VoiceActivityDetector.Event.SpeechStart:
                    _utterance.Clear();
                    _utterance.AddRange(chunk);
                    if (IsSpeaking && ConversationOpen) Interrupt(); // barge-in
                    IsListening = true;
                    break;
                case VoiceActivityDetector.Event.None:
                    if (_vad.InSpeech) _utterance.AddRange(chunk);
                    break;
                case VoiceActivityDetector.Event.SpeechEnd:
                case VoiceActivityDetector.Event.Timeout:
                    _utterance.AddRange(chunk);
                    IsListening = false;
                    if (_utterance.Count > _micRate / 3) _ = TranscribeAsync(_utterance.ToArray(), ConversationOpen);
                    _utterance.Clear();
                    break;
            }
        }

        private async Task TranscribeAsync(float[] samples, bool conversationOpen)
        {
            if (Interlocked.CompareExchange(ref _transcribing, 1, 0) != 0) return; // one at a time
            try
            {
                if (SttProvider == null) return;
                if (SttProvider.RequiresNetwork && !ConnectivityMonitor.IsOnline) { LastError = "offline"; return; }
                if (SttProvider.RequiresNetwork && !WintrySettings.Current.CloudAllowed) { LastError = "cloud disabled"; return; }
                PrivacyState.SetCloud(SttProvider.RequiresNetwork);
                string hint = WintrySettings.Current.Voice.Language == WintryLanguage.Auto ? "auto" : WintrySettings.Current.EffectiveLanguage();
                var res = await SttProvider.TranscribeAsync(samples, _micRate, hint, CancellationToken.None);
                PrivacyState.SetCloud(false);
                if (!res.Success || string.IsNullOrWhiteSpace(res.Text)) { LastError = res.Error; return; }
                string text = res.Text.Trim();
                WintryLog.I("Voice", "Heard: \"" + text + "\" (" + res.LanguageCode + ")");

                bool wake = WakeWordDetector.TryStrip(text, out string remainder);
                if (wake) WintryEvents.PublishOnMain(new WakeWordDetectedEvent { Transcript = remainder });
                if (!wake && WakeWordEnabled && !conversationOpen && !_pushToTalkActive)
                {
                    WintryLog.V("Voice", "Ignored (no wake word, conversation closed)");
                    return;
                }
                string utterance = wake ? remainder : text;
                WintryEvents.PublishOnMain(new TranscriptEvent { Text = utterance, IsFinal = true, LanguageCode = res.LanguageCode });
                if (utterance.Length > 0)
                {
                    var handler = OnUtterance;
                    if (handler != null) MainThreadDispatcher.Enqueue(() => handler(utterance, res.LanguageCode));
                }
            }
            catch (Exception ex) { WintryLog.E("Voice", "Transcription failed", ex); }
            finally { Interlocked.Exchange(ref _transcribing, 0); }
        }

        public async Task SpeakAsync(string text, string languageCode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text) || TtsProvider == null) return;
            Interrupt();
            var myCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _speakCts = myCts;
            var token = myCts.Token;
            IsSpeaking = true;
            try
            {
                var s = WintrySettings.Current;
                if (TtsProvider.RequiresNetwork && (!ConnectivityMonitor.IsOnline || !s.CloudAllowed))
                {
                    await PlayFallbackAsync(text, languageCode, token);
                    return;
                }
                PrivacyState.SetCloud(TtsProvider.RequiresNetwork);
                var res = await TtsProvider.SynthesizeAsync(text, languageCode, s.Voice.Voice, s.Voice.Speed, token);
                PrivacyState.SetCloud(false);
                if (!res.Success)
                {
                    WintryLog.W("Voice", "TTS failed: " + res.Error + " – using fallback voice");
                    await PlayFallbackAsync(text, languageCode, token);
                    return;
                }
                if (res.SpokenByProvider) { await Task.Delay((int)(res.DurationSeconds * 1000), token); return; }
                await PlayClipAsync(res.Clip, token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_speakCts == myCts) // a newer SpeakAsync may already be running
                {
                    IsSpeaking = false;
                    _speakCts = null;
                    WintryEvents.PublishOnMain(new SpeechAmplitudeEvent { Amplitude = 0f });
                    if (ContinuousConversation) OpenConversationWindow();
                }
                myCts.Dispose();
            }
        }

        private async Task PlayFallbackAsync(string text, string lang, CancellationToken token)
        {
            var mock = new Providers.MockTtsProvider();
            var res = await mock.SynthesizeAsync(text, lang, "", WintrySettings.Current.Voice.Speed, token);
            if (res.Success) await PlayClipAsync(res.Clip, token);
        }

        private async Task PlayClipAsync(AudioClip clip, CancellationToken token)
        {
            if (clip == null || VoiceSource == null) return;
            VoiceSource.clip = clip;
            VoiceSource.volume = WintrySettings.Current.Accessibility.VoiceVolume * WintrySettings.Current.Voice.Volume;
            VoiceSource.Play();
            float end = Time.time + clip.length + 0.05f;
            while (Time.time < end && VoiceSource.isPlaying)
            {
                token.ThrowIfCancellationRequested();
                VoiceSource.GetOutputData(_amp, 0);
                float sum = 0; for (int i = 0; i < _amp.Length; i++) sum += _amp[i] * _amp[i];
                WintryEvents.Publish(new SpeechAmplitudeEvent { Amplitude = Mathf.Clamp01(Mathf.Sqrt(sum / _amp.Length) * 6f) });
                await Task.Yield();
            }
            if (clip.name == "tts" || clip.name == "mock-tts") Destroy(clip);
        }

        public void Interrupt()
        {
            if (_speakCts != null) { _speakCts.Cancel(); _speakCts = null; }
            if (VoiceSource != null && VoiceSource.isPlaying) VoiceSource.Stop();
            TtsProvider?.Stop();
            IsSpeaking = false;
            WintryEvents.Publish(new SpeechAmplitudeEvent { Amplitude = 0f });
        }

        private void OnDestroy() { StopMicrophone(); Interrupt(); }
    }
}
