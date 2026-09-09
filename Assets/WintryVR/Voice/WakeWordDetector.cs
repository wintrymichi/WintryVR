using System;
using System.Text.RegularExpressions;

namespace WintryVR.Voice
{
    /// <summary>
    /// Wake word ("Hey Wintry") detection on transcripts. Works with any STT backend and tolerates common
    /// mis-transcriptions (wintery, wintry, winter-y, vintri...). Strips the wake phrase and returns the remainder.
    /// </summary>
    public static class WakeWordDetector
    {
        private static readonly Regex Pattern = new Regex(@"^\s*(?:(?:hey|hi|ok|okay|ehi|ciao|hallo|salut|oye|hola)[\s,]+)?(?:wintry|wintery|winter[\s-]?y|vintri|vintry|wintri|wintrey|wintre)\b[\s,.!?]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool TryStrip(string transcript, out string remainder)
        {
            remainder = transcript ?? "";
            if (string.IsNullOrWhiteSpace(transcript)) return false;
            var m = Pattern.Match(transcript);
            if (!m.Success) return false;
            remainder = transcript.Substring(m.Length).Trim();
            return true;
        }

        public static bool Contains(string transcript)
        {
            return !string.IsNullOrEmpty(transcript) && Regex.IsMatch(transcript, @"\b(wintry|wintery|vintri|vintry|wintri)\b", RegexOptions.IgnoreCase);
        }
    }
}
