using System;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Settings
{
    public enum ResponseLength { Short, Medium, Long }
    public enum AnchorBehavior { FollowUser, StayInPlace, ReturnHome }

    /// <summary>
    /// User settings for every section of the settings menu. Persisted as JSON in PlayerPrefs.
    /// Changing a value and calling Save() publishes SettingsChangedEvent for the section.
    /// </summary>
    [Serializable]
    public class WintrySettings
    {
        [Serializable]
        public class AISettings
        {
            public string Provider = "auto";
            public string Model = "";
            public ResponseLength ResponseLength = ResponseLength.Medium;
            [Range(0f, 1f)] public float Creativity = 0.3f;
            public bool ContextMemory = true;
            public bool CloudProcessing = true;
        }

        [Serializable]
        public class VoiceSettings
        {
            public string Voice = "default";
            public WintryLanguage Language = WintryLanguage.Auto;
            [Range(0.6f, 1.6f)] public float Speed = 1f;
            public bool WakeWord = true;
            public bool ContinuousConversation = true;
            [Range(0f, 1f)] public float Volume = 0.9f;
        }

        [Serializable]
        public class VisionSettings
        {
            public bool ObjectRecognition = true;
            public bool OCR = true;
            public bool SceneUnderstanding = true;
            public bool SmartHighlight = true;
            [Range(0.2f, 0.9f)] public float ConfidenceThreshold = 0.5f;
        }

        [Serializable]
        public class MRSettings
        {
            [Range(0.5f, 2.5f)] public float UIDistance = 1.1f;
            [Range(0.6f, 1.6f)] public float UISize = 1f;
            public AnchorBehavior AnchorBehavior = AnchorBehavior.FollowUser;
            [Range(-1f, 1f)] public float PassthroughBrightness = 0f;
            public bool ShowCore = true;
        }

        [Serializable]
        public class PrivacySettings
        {
            public bool CameraAllowed = true;
            public bool MicrophoneAllowed = true;
            public bool CloudProcessing = true;
            public bool KeepHistory = true;
            public bool LocationAllowed = false;
            public bool ShowIndicators = true;
        }

        [Serializable]
        public class AccessibilitySettings
        {
            public bool Subtitles = true;
            [Range(0.7f, 1.8f)] public float TextSize = 1f;
            public bool HighContrast = false;
            public bool ReducedMotion = false;
            [Range(0f, 1f)] public float VoiceVolume = 0.9f;
        }

        public AISettings AI = new AISettings();
        public VoiceSettings Voice = new VoiceSettings();
        public VisionSettings Vision = new VisionSettings();
        public MRSettings MR = new MRSettings();
        public PrivacySettings Privacy = new PrivacySettings();
        public AccessibilitySettings Accessibility = new AccessibilitySettings();
        public bool OnboardingCompleted = false;
        public WintryVariant CharacterVariant = WintryVariant.Default;

        private const string PrefKey = "wintry.settings.v1";
        private static WintrySettings _current;

        public static WintrySettings Current
        {
            get
            {
                if (_current == null) _current = Load();
                return _current;
            }
        }

        public static WintrySettings Load()
        {
            var s = new WintrySettings();
            try
            {
                string json = PlayerPrefs.GetString(PrefKey, "");
                if (!string.IsNullOrEmpty(json)) JsonUtility.FromJsonOverwrite(json, s);
            }
            catch (Exception ex) { WintryLog.W("Settings", "Failed to load settings: " + ex.Message); }
            return s;
        }

        public void Save(string section = "all")
        {
            try
            {
                PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(this));
                PlayerPrefs.Save();
            }
            catch (Exception ex) { WintryLog.W("Settings", "Failed to save settings: " + ex.Message); }
            WintryEvents.Publish(new SettingsChangedEvent { Section = section });
        }

        public void ResetToDefaults()
        {
            var fresh = new WintrySettings();
            AI = fresh.AI; Voice = fresh.Voice; Vision = fresh.Vision; MR = fresh.MR; Privacy = fresh.Privacy; Accessibility = fresh.Accessibility;
            Save();
        }

        /// <summary>Effective language code ("en", "it", ...), resolving Auto from the system language.</summary>
        public string EffectiveLanguage()
        {
            if (Voice.Language == WintryLanguage.Auto) return Localization.FromSystemLanguage(Application.systemLanguage);
            return LanguageCodes.ToCode(Voice.Language);
        }

        public bool CloudAllowed => AI.CloudProcessing && Privacy.CloudProcessing;
    }
}
