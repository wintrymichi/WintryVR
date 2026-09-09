namespace WintryVR.Core
{
    /// <summary>The visible/behavioural state of the assistant. Drives Core, character, audio and UI.</summary>
    public enum AssistantState
    {
        Idle,
        Listening,
        Thinking,
        Vision,
        Searching,
        Speaking,
        Error,
        Offline
    }

    /// <summary>Which visual form Wintry currently has.</summary>
    public enum PresenceForm
    {
        Hidden,
        Core,
        Transforming,
        Character
    }

    public enum Intent
    {
        IDENTIFY,
        DESCRIBE,
        OCR,
        TRANSLATE,
        SEARCH,
        LOCATE,
        COMPARE,
        EXPLAIN,
        COUNT,
        NAVIGATE,
        GENERAL_CONVERSATION,
        // Control intents (not part of the product spec list, but needed to run the assistant by voice)
        CLEAR_CONTEXT,
        DISMISS,
        CHANGE_LOOK
    }

    public enum WintryLanguage
    {
        Auto,
        Italian,
        English,
        German,
        French,
        Spanish
    }

    public static class LanguageCodes
    {
        public static string ToCode(WintryLanguage l)
        {
            switch (l)
            {
                case WintryLanguage.Italian: return "it";
                case WintryLanguage.German: return "de";
                case WintryLanguage.French: return "fr";
                case WintryLanguage.Spanish: return "es";
                case WintryLanguage.English: return "en";
                default: return "auto";
            }
        }

        public static WintryLanguage FromCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return WintryLanguage.Auto;
            code = code.ToLowerInvariant();
            if (code.StartsWith("it")) return WintryLanguage.Italian;
            if (code.StartsWith("de")) return WintryLanguage.German;
            if (code.StartsWith("fr")) return WintryLanguage.French;
            if (code.StartsWith("es")) return WintryLanguage.Spanish;
            if (code.StartsWith("en")) return WintryLanguage.English;
            return WintryLanguage.Auto;
        }

        public static string DisplayName(string code)
        {
            switch (code)
            {
                case "it": return "Italiano";
                case "de": return "Deutsch";
                case "fr": return "Français";
                case "es": return "Español";
                case "en": return "English";
                default: return code;
            }
        }
    }
}
