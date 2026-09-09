using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WintryVR.Core;
using WintryVR.Networking;

namespace WintryVR.AI.Providers
{
    /// <summary>
    /// Anthropic Messages API provider (raw HTTP, since the official C# SDK is not consumable inside Unity).
    /// Images are sent as base64 image blocks. Adaptive thinking is the model default; depth is steered
    /// with output_config.effort so short spatial answers stay fast. Requests that come back with
    /// stop_reason "refusal" are surfaced as Refused so the orchestrator can answer gracefully.
    /// The base URL can point at a backend proxy that injects the key server-side (recommended for shipping).
    /// </summary>
    public class AnthropicAIProvider : AIProviderBase
    {
        public const string DefaultModel = "claude-opus-5";
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly string _apiKey;

        public AnthropicAIProvider(string baseUrl, string model, string apiKey)
        {
            _baseUrl = string.IsNullOrEmpty(baseUrl) ? "https://api.anthropic.com" : baseUrl.TrimEnd('/');
            _model = string.IsNullOrEmpty(model) ? DefaultModel : model;
            _apiKey = apiKey;
        }

        public override string Name => "Anthropic";
        public override string Model => _model;
        public override bool IsConfigured => !string.IsNullOrEmpty(_apiKey) || _baseUrl != "https://api.anthropic.com";

        protected override async Task CompleteInternalAsync(AIRequest request, AIResponse response, CancellationToken ct)
        {
            var messages = new List<object>();
            foreach (var m in request.Messages)
            {
                if (m.Role == AIRole.System) continue; // system goes in the top-level field
                var blocks = new List<object>();
                foreach (var p in m.Parts)
                {
                    if (p.IsImage)
                        blocks.Add(new Dictionary<string, object>
                        {
                            ["type"] = "image",
                            ["source"] = new Dictionary<string, object> { ["type"] = "base64", ["media_type"] = "image/jpeg", ["data"] = Convert.ToBase64String(p.ImageJpeg) }
                        });
                    else if (!string.IsNullOrEmpty(p.Text))
                        blocks.Add(new Dictionary<string, object> { ["type"] = "text", ["text"] = p.Text });
                }
                if (blocks.Count == 0) continue;
                messages.Add(new Dictionary<string, object> { ["role"] = RoleName(m.Role), ["content"] = blocks });
            }

            string system = request.SystemPrompt ?? "";
            foreach (var m in request.Messages) if (m.Role == AIRole.System) system += "\n" + m.PlainText;

            var body = new Dictionary<string, object>
            {
                ["model"] = response.Model,
                ["max_tokens"] = Math.Max(256, request.MaxTokens),
                ["messages"] = messages,
                ["output_config"] = new Dictionary<string, object> { ["effort"] = request.AllowThinking ? "medium" : "low" }
            };
            if (!string.IsNullOrEmpty(system)) body["system"] = system;

            var headers = new Dictionary<string, string> { ["anthropic-version"] = "2023-06-01" };
            if (!string.IsNullOrEmpty(_apiKey)) headers["x-api-key"] = _apiKey;

            var http = await HttpClientService.PostJsonAsync(_baseUrl + "/v1/messages", MiniJson.Serialize(body), headers, TimeoutSeconds, ct);
            if (!http.Success) { response.Error = http.Error; return; }

            var root = MiniJson.Deserialize(http.Body);
            string stop = MiniJson.GetString(root, "stop_reason", "");
            if (stop == "refusal") { response.Refused = true; response.Error = "refusal"; return; }

            var sb = new System.Text.StringBuilder();
            var content = MiniJson.GetArray(root, "content");
            if (content != null)
                foreach (var block in content)
                    if (MiniJson.GetString(block, "type") == "text") sb.Append(MiniJson.GetString(block, "text", ""));
            response.Text = sb.ToString();
            response.Success = response.Text.Length > 0;
            if (!response.Success) response.Error = "Empty response";
            var usage = MiniJson.GetObject(root, "usage");
            response.InputTokens = (int)MiniJson.GetNumber(usage, "input_tokens");
            response.OutputTokens = (int)MiniJson.GetNumber(usage, "output_tokens");
        }
    }
}
