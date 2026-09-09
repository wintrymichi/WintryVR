using System;
using WintryVR.Core;

namespace WintryVR.AI
{
    /// <summary>
    /// Confidence phrasing and sensitive-topic disclaimers. Wintry never presents uncertain information as
    /// certain, and never gives medical/legal/safety-critical instructions as professional advice.
    /// </summary>
    public static class ResponseSafety
    {
        public const float LowConfidence = 0.6f;
        public const float VeryLowConfidence = 0.35f;

        public static string ApplyConfidence(string speech, float confidence, string languageCode)
        {
            if (confidence < 0 || confidence >= LowConfidence || string.IsNullOrEmpty(speech)) return speech;
            string prefix = Localization.Get("err.unsure", languageCode);
            // Avoid double hedging if the model already hedged
            string lower = speech.ToLowerInvariant();
            if (lower.Contains("not sure") || lower.Contains("non sono sicur") || lower.Contains("nicht sicher") || lower.Contains("pas sûr") || lower.Contains("no estoy segur") || lower.StartsWith("it looks like") || lower.StartsWith("sembra") || lower.StartsWith("es sieht") || lower.StartsWith("on dirait") || lower.StartsWith("parece"))
                return speech;
            return prefix + " " + char.ToLowerInvariant(speech[0]) + speech.Substring(1);
        }

        public static string AppendDisclaimers(string speech, string userText, string languageCode)
        {
            string t = ((userText ?? "") + " " + (speech ?? "")).ToLowerInvariant();
            string add = null;
            if (Has(t, "dose", "dosage", "symptom", "diagnos", "medicin", "farmac", "sintom", "malatt", "krankheit", "medikament", "maladie", "médicament", "enfermedad", "medicamento", "pill", "pillol"))
                add = Localization.Get("safety.medical", languageCode);
            else if (Has(t, "lawsuit", "contract", "legal", "illegal", "contratto", "legale", "avvocato", "vertrag", "rechtlich", "anwalt", "contrat", "juridique", "avocat", "contrato", "abogado"))
                add = Localization.Get("safety.legal", languageCode);
            else if (Has(t, "electric", "wiring", "gas leak", "chemical", "elettric", "fuga di gas", "chimic", "strom", "gasleck", "chemikal", "électri", "fuite de gaz", "chimi", "eléctric", "fuga de gas", "químic"))
                add = Localization.Get("safety.safety", languageCode);
            if (add == null || string.IsNullOrEmpty(speech) || speech.Contains(add)) return speech;
            return speech.TrimEnd() + " " + add;
        }

        private static bool Has(string text, params string[] words)
        {
            foreach (var w in words) if (text.Contains(w)) return true;
            return false;
        }
    }
}
