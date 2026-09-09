using WintryVR.Core;
using WintryVR.Voice.Providers;

namespace WintryVR.Voice
{
    public static class VoiceProviderFactory
    {
        public static ISpeechToTextProvider CreateStt(WintryConfig cfg)
        {
            string choice = (cfg.SttProvider ?? "auto").ToLowerInvariant();
            string key = SecretStore.Get("STT_API_KEY") ?? SecretStore.Get("AI_API_KEY");
            string baseUrl = !string.IsNullOrEmpty(cfg.SttBaseUrl) ? cfg.SttBaseUrl : SecretStore.Get("PROXY_URL");
            if (cfg.DemoMode && choice == "auto") choice = "mock";
            if (choice == "auto")
            {
                var cloud = new CloudSttProvider(baseUrl, cfg.SttModel, key);
                if (cloud.IsConfigured) return cloud;
                var android = new AndroidSttProvider();
                if (android.IsConfigured) return android;
                return new MockSttProvider();
            }
            switch (choice)
            {
                case "cloud": return new CloudSttProvider(baseUrl, cfg.SttModel, key);
                case "android": return new AndroidSttProvider();
                default: return new MockSttProvider();
            }
        }

        public static ITextToSpeechProvider CreateTts(WintryConfig cfg)
        {
            string choice = (cfg.TtsProvider ?? "auto").ToLowerInvariant();
            string key = SecretStore.Get("TTS_API_KEY") ?? SecretStore.Get("AI_API_KEY");
            string baseUrl = !string.IsNullOrEmpty(cfg.TtsBaseUrl) ? cfg.TtsBaseUrl : SecretStore.Get("PROXY_URL");
            if (cfg.DemoMode && choice == "auto") choice = "mock";
            if (choice == "auto")
            {
                var cloud = new CloudTtsProvider(baseUrl, cfg.TtsModel, cfg.TtsVoice, key);
                if (cloud.IsConfigured) return cloud;
                var android = new AndroidTtsProvider();
                if (android.IsConfigured) return android;
                return new MockTtsProvider();
            }
            switch (choice)
            {
                case "cloud": return new CloudTtsProvider(baseUrl, cfg.TtsModel, cfg.TtsVoice, key);
                case "android": return new AndroidTtsProvider();
                default: return new MockTtsProvider();
            }
        }
    }
}
