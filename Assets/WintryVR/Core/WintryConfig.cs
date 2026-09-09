using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WintryVR.Networking;

namespace WintryVR.Core
{
    /// <summary>
    /// Non-secret runtime configuration: endpoints, model names, feature flags.
    /// Loaded from StreamingAssets/wintry.config.json (see wintry.config.example.json) with
    /// environment-variable overrides (WINTRY_CFG_&lt;KEY&gt;). Secrets live in <see cref="SecretStore"/>.
    /// </summary>
    [Serializable]
    public class WintryConfig
    {
        // ---- AI ----
        public string AiProvider = "auto";            // auto | anthropic | openai-compatible | local | mock
        public string AiBaseUrl = "";                 // provider base URL or backend proxy URL
        public string AiModel = "";                   // empty => provider default
        public string LocalAiBaseUrl = "http://192.168.1.10:11434/v1"; // LAN model server (OpenAI-compatible)
        public string LocalAiModel = "llava";
        public int AiTimeoutSeconds = 45;

        // ---- Voice ----
        public string SttProvider = "auto";           // auto | cloud | android | mock
        public string SttBaseUrl = "";                // OpenAI-compatible /audio/transcriptions or proxy
        public string SttModel = "whisper-1";
        public string TtsProvider = "auto";           // auto | cloud | android | mock
        public string TtsBaseUrl = "";                // OpenAI-compatible /audio/speech or proxy
        public string TtsModel = "tts-1";
        public string TtsVoice = "alloy";

        // ---- Search ----
        public string SearchProvider = "auto";        // auto | tavily | generic | mock
        public string SearchBaseUrl = "";

        // ---- Asset generation ----
        public string AssetGenProvider = "auto";      // auto | openai-images | generic | mock
        public string AssetGenBaseUrl = "";

        // ---- Camera ----
        public string CameraProvider = "auto";        // auto | passthrough | render | demo
        public int CaptureMaxWidth = 1024;
        public int CaptureJpegQuality = 80;

        // ---- Flags ----
        public bool DemoMode = false;
        public bool DebugPanel = false;
        public bool VerboseLogging = false;
        public bool ForceOffline = false;

        private static WintryConfig _instance;
        public static WintryConfig Instance
        {
            get { if (_instance == null) _instance = Load(); return _instance; }
        }

        public static WintryConfig Load()
        {
            var cfg = new WintryConfig();
            string path = Path.Combine(Application.streamingAssetsPath, "wintry.config.json");
            try
            {
                if (File.Exists(path))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(path), cfg);
                    WintryLog.I("Config", "Loaded wintry.config.json");
                }
                else WintryLog.I("Config", "No wintry.config.json found, using defaults (mock/demo providers).");
            }
            catch (Exception ex) { WintryLog.W("Config", "Config parse failed: " + ex.Message); }

            // persistentDataPath override (adb push without rebuilding)
            string overridePath = Path.Combine(Application.persistentDataPath, "wintry.config.json");
            try
            {
                if (File.Exists(overridePath))
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(overridePath), cfg);
                    WintryLog.I("Config", "Applied persistentDataPath config override");
                }
            }
            catch (Exception ex) { WintryLog.W("Config", "Override parse failed: " + ex.Message); }

            ApplyEnvOverrides(cfg);
            WintryLog.VerboseEnabled = cfg.VerboseLogging;
            return cfg;
        }

        private static void ApplyEnvOverrides(WintryConfig cfg)
        {
            foreach (var f in typeof(WintryConfig).GetFields())
            {
                if (f.IsStatic) continue;
                string env = Environment.GetEnvironmentVariable("WINTRY_CFG_" + f.Name.ToUpperInvariant());
                if (string.IsNullOrEmpty(env)) continue;
                try
                {
                    if (f.FieldType == typeof(string)) f.SetValue(cfg, env);
                    else if (f.FieldType == typeof(bool)) f.SetValue(cfg, env == "1" || env.Equals("true", StringComparison.OrdinalIgnoreCase));
                    else if (f.FieldType == typeof(int)) f.SetValue(cfg, int.Parse(env));
                    else if (f.FieldType == typeof(float)) f.SetValue(cfg, float.Parse(env, System.Globalization.CultureInfo.InvariantCulture));
                }
                catch { WintryLog.W("Config", "Bad env override for " + f.Name); }
            }
        }

        public string ToJson() => JsonUtility.ToJson(this, true);
    }
}
