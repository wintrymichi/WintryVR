using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WintryVR.Core;
using WintryVR.Networking;

namespace WintryVR.AI.Providers
{
    /// <summary>
    /// Generic "OpenAI-compatible chat completions" provider. Works with OpenAI, Azure OpenAI (with the
    /// right base URL), OpenRouter, Groq, Mistral, vLLM, LM Studio, Ollama's /v1 endpoint and most
    /// backend proxies. Images are sent as base64 data URLs.
    /// </summary>
    public class CloudAIProvider : AIProviderBase
    {
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly string _apiKey;
        private readonly string _name;

        public CloudAIProvider(string name, string baseUrl, string model, string apiKey)
        {
            _name = name;
            _baseUrl = string.IsNullOrEmpty(baseUrl) ? "https://api.openai.com/v1" : baseUrl.TrimEnd('/');
            _model = string.IsNullOrEmpty(model) ? "gpt-4o-mini" : model;
            _apiKey = apiKey;
        }

        public override string Name => _name;
        public override string Model => _model;
        public override bool IsConfigured => !string.IsNullOrEmpty(_baseUrl) && (!string.IsNullOrEmpty(_apiKey) || IsLocalUrl(_baseUrl));

        internal static bool IsLocalUrl(string url)
        {
            url = url.ToLowerInvariant();
            return url.Contains("localhost") || url.Contains("127.0.0.1") || url.Contains("192.168.") || url.Contains("10.0.") || url.Contains(".local");
        }

        protected override async Task CompleteInternalAsync(AIRequest request, AIResponse response, CancellationToken ct)
        {
            var messages = new List<object>();
            if (!string.IsNullOrEmpty(request.SystemPrompt))
                messages.Add(new Dictionary<string, object> { ["role"] = "system", ["content"] = request.SystemPrompt });

            foreach (var m in request.Messages)
            {
                bool hasImage = false;
                foreach (var p in m.Parts) if (p.IsImage) { hasImage = true; break; }
                if (!hasImage)
                {
                    messages.Add(new Dictionary<string, object> { ["role"] = RoleName(m.Role), ["content"] = m.PlainText });
                    continue;
                }
                var parts = new List<object>();
                foreach (var p in m.Parts)
                {
                    if (p.IsImage)
                        parts.Add(new Dictionary<string, object>
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new Dictionary<string, object> { ["url"] = "data:image/jpeg;base64," + Convert.ToBase64String(p.ImageJpeg), ["detail"] = "auto" }
                        });
                    else if (!string.IsNullOrEmpty(p.Text))
                        parts.Add(new Dictionary<string, object> { ["type"] = "text", ["text"] = p.Text });
                }
                messages.Add(new Dictionary<string, object> { ["role"] = RoleName(m.Role), ["content"] = parts });
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = response.Model,
                ["messages"] = messages,
                ["max_tokens"] = request.MaxTokens,
                ["temperature"] = request.Temperature
            };
            if (request.ExpectJson) body["response_format"] = new Dictionary<string, object> { ["type"] = "json_object" };

            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(_apiKey)) headers["Authorization"] = "Bearer " + _apiKey;

            var http = await HttpClientService.PostJsonAsync(_baseUrl + "/chat/completions", MiniJson.Serialize(body), headers, TimeoutSeconds, ct);
            if (!http.Success) { response.Error = http.Error; return; }

            var root = MiniJson.Deserialize(http.Body);
            var content = MiniJson.Path(root, "choices.0.message.content");
            if (content is string s) { response.Text = s; response.Success = true; }
            else if (content is List<object> list)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var part in list) { var t = MiniJson.GetString(part, "text"); if (t != null) sb.Append(t); }
                response.Text = sb.ToString(); response.Success = response.Text.Length > 0;
            }
            else response.Error = "No content in response: " + (http.Body.Length > 300 ? http.Body.Substring(0, 300) : http.Body);

            string finish = MiniJson.Path(root, "choices.0.finish_reason") as string;
            if (finish == "content_filter") response.Refused = true;
            response.InputTokens = (int)MiniJson.GetNumber(MiniJson.GetObject(root, "usage"), "prompt_tokens");
            response.OutputTokens = (int)MiniJson.GetNumber(MiniJson.GetObject(root, "usage"), "completion_tokens");
        }
    }
}
