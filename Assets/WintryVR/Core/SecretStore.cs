using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WintryVR.Networking;

namespace WintryVR.Core
{
    /// <summary>
    /// Resolves API keys without ever embedding them in source control.
    /// Lookup order:
    ///   1. Environment variable (WINTRY_&lt;KEY&gt;) - editor/desktop/dev
    ///   2. Secure file in Application.persistentDataPath/wintry.secrets.json - pushed with adb, never in the APK
    ///   3. StreamingAssets/wintry.secrets.json (git-ignored) - local builds only
    ///   4. PlayerPrefs (entered from the settings UI)
    /// A backend proxy (WINTRY_PROXY_URL) is the recommended production setup: the headset never holds vendor keys.
    /// </summary>
    public static class SecretStore
    {
        private static Dictionary<string, string> _fileSecrets;
        private static bool _loaded;

        public static string Get(string key)
        {
            EnsureLoaded();
            string env = Environment.GetEnvironmentVariable("WINTRY_" + key.ToUpperInvariant());
            if (!string.IsNullOrEmpty(env)) return env;
            if (_fileSecrets != null && _fileSecrets.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)) return v;
            string pref = PlayerPrefs.GetString("wintry.secret." + key, "");
            return string.IsNullOrEmpty(pref) ? null : pref;
        }

        public static bool Has(string key) => !string.IsNullOrEmpty(Get(key));

        public static void SetFromUI(string key, string value)
        {
            PlayerPrefs.SetString("wintry.secret." + key, value ?? "");
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            foreach (var k in new[] { "AI_API_KEY", "STT_API_KEY", "TTS_API_KEY", "SEARCH_API_KEY", "ASSETGEN_API_KEY", "PROXY_TOKEN" })
                PlayerPrefs.DeleteKey("wintry.secret." + k);
            PlayerPrefs.Save();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _fileSecrets = new Dictionary<string, string>();
            TryLoadFile(Path.Combine(Application.persistentDataPath, "wintry.secrets.json"));
            TryLoadFile(Path.Combine(Application.streamingAssetsPath, "wintry.secrets.json"));
        }

        private static void TryLoadFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var obj = MiniJson.Deserialize(File.ReadAllText(path)) as Dictionary<string, object>;
                if (obj == null) return;
                foreach (var kv in obj)
                    if (kv.Value is string s && !_fileSecrets.ContainsKey(kv.Key)) _fileSecrets[kv.Key] = s;
                WintryLog.I("Secrets", "Loaded " + obj.Count + " secret(s) from " + Path.GetFileName(path));
            }
            catch (Exception ex) { WintryLog.W("Secrets", "Could not read " + path + ": " + ex.Message); }
        }
    }
}
