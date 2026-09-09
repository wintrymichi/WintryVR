using WintryVR.Core;

namespace WintryVR.AI.Providers
{
    /// <summary>Builds the configured provider; never hardcodes a vendor.</summary>
    public static class AIProviderFactory
    {
        public static IAIProvider Create(WintryConfig cfg)
        {
            string choice = (cfg.AiProvider ?? "auto").ToLowerInvariant();
            string key = SecretStore.Get("AI_API_KEY");
            string proxy = SecretStore.Get("PROXY_URL"); // optional backend proxy that holds the real keys
            string baseUrl = !string.IsNullOrEmpty(cfg.AiBaseUrl) ? cfg.AiBaseUrl : proxy;

            if (choice == "auto")
            {
                if (!string.IsNullOrEmpty(key) || !string.IsNullOrEmpty(baseUrl))
                    choice = LooksLikeAnthropic(key, baseUrl) ? "anthropic" : "openai-compatible";
                else choice = "mock";
            }

            switch (choice)
            {
                case "anthropic": return new AnthropicAIProvider(baseUrl, cfg.AiModel, key);
                case "openai-compatible":
                case "openai":
                case "cloud": return new CloudAIProvider("Cloud", baseUrl, cfg.AiModel, key);
                case "local": return new LocalAIProvider(cfg.LocalAiBaseUrl, cfg.LocalAiModel);
                default: return new MockAIProvider();
            }
        }

        private static bool LooksLikeAnthropic(string key, string baseUrl)
        {
            if (!string.IsNullOrEmpty(baseUrl) && baseUrl.Contains("anthropic")) return true;
            return !string.IsNullOrEmpty(key) && key.StartsWith("sk-ant-");
        }
    }
}
