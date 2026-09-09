using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;
using WintryVR.Core;
using WintryVR.Networking;

namespace WintryVR.Voice.Providers
{
    /// <summary>
    /// Speech-to-text through an OpenAI-compatible <c>/audio/transcriptions</c> endpoint (OpenAI Whisper,
    /// Groq, local whisper servers, or a backend proxy). Audio is uploaded as 16 kHz mono WAV.
    /// </summary>
    public class CloudSttProvider : ISpeechToTextProvider
    {
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly string _apiKey;

        public CloudSttProvider(string baseUrl, string model, string apiKey)
        {
            _baseUrl = string.IsNullOrEmpty(baseUrl) ? "https://api.openai.com/v1" : baseUrl.TrimEnd('/');
            _model = string.IsNullOrEmpty(model) ? "whisper-1" : model;
            _apiKey = apiKey;
        }

        public string Name => "CloudSTT";
        public bool RequiresNetwork => true;
        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey) || CloudAIProviderUrlCheck.IsLocal(_baseUrl);

        public async Task<SttResult> TranscribeAsync(float[] samples, int sampleRate, string languageHint, CancellationToken ct)
        {
            var result = new SttResult();
            var pcm = WavUtility.Resample(samples, sampleRate, 16000);
            byte[] wav = WavUtility.FromSamples(pcm, 16000);
            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", wav, "audio.wav", "audio/wav"),
                new MultipartFormDataSection("model", _model),
                new MultipartFormDataSection("response_format", "verbose_json")
            };
            if (!string.IsNullOrEmpty(languageHint) && languageHint != "auto") form.Add(new MultipartFormDataSection("language", languageHint));
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(_apiKey)) headers["Authorization"] = "Bearer " + _apiKey;

            var http = await HttpClientService.PostMultipartAsync(_baseUrl + "/audio/transcriptions", form, headers, 30, ct);
            if (!http.Success) { result.Error = http.Error; return result; }
            var root = MiniJson.Deserialize(http.Body);
            result.Text = (MiniJson.GetString(root, "text", "") ?? "").Trim();
            result.LanguageCode = MiniJson.GetString(root, "language", "") ?? "";
            result.Success = result.Text.Length > 0;
            return result;
        }
    }

    internal static class CloudAIProviderUrlCheck
    {
        public static bool IsLocal(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            url = url.ToLowerInvariant();
            return url.Contains("localhost") || url.Contains("127.0.0.1") || url.Contains("192.168.") || url.Contains("10.0.") || url.Contains(".local");
        }
    }
}
