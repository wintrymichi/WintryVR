using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Networking;

namespace WintryVR.Voice.Providers
{
    /// <summary>
    /// Text-to-speech through an OpenAI-compatible <c>/audio/speech</c> endpoint returning WAV (or a proxy that
    /// does). WAV is decoded on device; MP3 would need UnityWebRequestMultimedia and is out of scope here.
    /// </summary>
    public class CloudTtsProvider : ITextToSpeechProvider
    {
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly string _apiKey;
        private readonly string _defaultVoice;

        public CloudTtsProvider(string baseUrl, string model, string defaultVoice, string apiKey)
        {
            _baseUrl = string.IsNullOrEmpty(baseUrl) ? "https://api.openai.com/v1" : baseUrl.TrimEnd('/');
            _model = string.IsNullOrEmpty(model) ? "tts-1" : model;
            _defaultVoice = string.IsNullOrEmpty(defaultVoice) ? "alloy" : defaultVoice;
            _apiKey = apiKey;
        }

        public string Name => "CloudTTS";
        public bool RequiresNetwork => true;
        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey) || CloudAIProviderUrlCheck.IsLocal(_baseUrl);

        public async Task<TtsResult> SynthesizeAsync(string text, string languageCode, string voice, float speed, CancellationToken ct)
        {
            var result = new TtsResult();
            var body = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["input"] = text,
                ["voice"] = string.IsNullOrEmpty(voice) || voice == "default" ? _defaultVoice : voice,
                ["response_format"] = "wav",
                ["speed"] = Mathf.Clamp(speed, 0.5f, 2f)
            };
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(_apiKey)) headers["Authorization"] = "Bearer " + _apiKey;
            var http = await HttpClientService.PostJsonAsync(_baseUrl + "/audio/speech", MiniJson.Serialize(body), headers, 30, ct);
            if (!http.Success) { result.Error = http.Error; return result; }
            var clip = await MainThreadDispatcher.RunAsync(() => WavUtility.ToAudioClip(http.Bytes, "tts"));
            if (clip == null) { result.Error = "Could not decode TTS audio (expected WAV)"; return result; }
            result.Clip = clip; result.DurationSeconds = clip.length; result.Success = true;
            return result;
        }

        public void Stop() { }
    }
}
