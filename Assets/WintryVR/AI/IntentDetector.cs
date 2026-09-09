using System;
using System.Collections.Generic;
using WintryVR.Core;

namespace WintryVR.AI
{
    /// <summary>
    /// Fast, offline, multilingual rule-based intent classifier (it/en/de/fr/es). Used first; the LLM is only
    /// consulted when the rules are unsure. Also detects the language of the utterance from keyword hits.
    /// </summary>
    public static class IntentDetector
    {
        private struct Rule { public Intent Intent; public string[] Keywords; public int Weight; }

        private static readonly Rule[] Rules =
        {
            new Rule { Intent = Intent.CLEAR_CONTEXT, Weight = 5, Keywords = new[] { "clear context", "forget that", "forget this", "forget it", "start over", "cancella il contesto", "dimentica", "ricomincia", "kontext löschen", "vergiss das", "oublie ça", "efface le contexte", "olvida eso", "borra el contexto" } },
            new Rule { Intent = Intent.DISMISS, Weight = 5, Keywords = new[] { "thanks wintry", "that's all", "goodbye", "bye wintry", "go away", "dismiss", "grazie wintry", "basta così", "ciao wintry", "vai pure", "danke wintry", "das war's", "tschüss", "merci wintry", "c'est tout", "au revoir", "gracias wintry", "eso es todo", "adiós" } },
            new Rule { Intent = Intent.CHANGE_LOOK, Weight = 5, Keywords = new[] { "new look", "change your look", "new outfit", "change your style", "nuovo look", "cambia look", "cambia aspetto", "neuen look", "ändere dein aussehen", "nouveau look", "change de style", "nuevo look", "cambia tu aspecto" } },
            new Rule { Intent = Intent.TRANSLATE, Weight = 4, Keywords = new[] { "translate", "traduci", "traduc", "übersetz", "traduis", "traduce", "in english", "in italiano", "auf deutsch", "en français", "en español" } },
            new Rule { Intent = Intent.OCR, Weight = 4, Keywords = new[] { "read this", "read that", "read it", "read the", "what does it say", "what does this say", "summarize this", "summarise", "leggi", "leggimi", "cosa c'è scritto", "riassumi", "lies das", "lies mir", "was steht", "fass das zusammen", "lis ça", "lis-moi", "qu'est-ce qui est écrit", "résume", "lee esto", "léeme", "qué dice", "resume esto" } },
            new Rule { Intent = Intent.SEARCH, Weight = 4, Keywords = new[] { "how much", "price", "cost", "buy", "cheapest", "reviews", "latest", "quanto costa", "prezzo", "quanto viene", "dove lo compro", "recensioni", "wie viel kostet", "was kostet", "preis", "kaufen", "combien coûte", "prix", "acheter", "cuánto cuesta", "precio", "comprar" } },
            new Rule { Intent = Intent.LOCATE, Weight = 4, Keywords = new[] { "where is", "where are", "where's", "find my", "show me the", "dov'è", "dove sono", "dove è", "trova", "mostrami", "wo ist", "wo sind", "zeig mir", "finde", "où est", "où sont", "montre-moi", "trouve", "dónde está", "dónde están", "muéstrame", "encuentra" } },
            new Rule { Intent = Intent.COUNT, Weight = 4, Keywords = new[] { "how many", "count the", "quante", "quanti", "conta", "wie viele", "zähl", "combien de", "compte", "cuántas", "cuántos", "cuenta" } },
            new Rule { Intent = Intent.COMPARE, Weight = 4, Keywords = new[] { "which one", "which of", "compare", "difference between", "better", "more expensive", "quale dei", "quale è", "confronta", "differenza", "migliore", "più costoso", "welches", "vergleich", "unterschied", "besser", "teurer", "lequel", "compare", "différence", "meilleur", "cuál de", "compara", "diferencia", "mejor", "más caro" } },
            new Rule { Intent = Intent.NAVIGATE, Weight = 3, Keywords = new[] { "take me to", "guide me", "how do i get to", "nearest", "closest", "portami", "guidami", "come arrivo", "più vicin", "bring mich", "führ mich", "nächste", "emmène-moi", "guide-moi", "le plus proche", "llévame", "guíame", "más cercan" } },
            new Rule { Intent = Intent.EXPLAIN, Weight = 3, Keywords = new[] { "explain", "how does", "how do", "why", "what is it for", "tell me more", "spiega", "spiegamelo", "come funziona", "perché", "a cosa serve", "dimmi di più", "erklär", "wie funktioniert", "warum", "wofür", "erzähl mir mehr", "explique", "comment ça marche", "pourquoi", "à quoi ça sert", "dis-m'en plus", "explica", "cómo funciona", "por qué", "para qué sirve", "cuéntame más" } },
            new Rule { Intent = Intent.DESCRIBE, Weight = 3, Keywords = new[] { "describe", "what do you see", "what's around", "what is on", "what's on", "what is in", "descrivi", "cosa vedi", "cosa c'è", "beschreib", "was siehst du", "was ist auf", "décris", "que vois-tu", "qu'y a-t-il", "describe", "qué ves", "qué hay" } },
            new Rule { Intent = Intent.IDENTIFY, Weight = 2, Keywords = new[] { "what is this", "what is that", "what's this", "what's that", "what am i looking at", "identify", "which model", "what brand", "cos'è questo", "cos'è quello", "cosa sto guardando", "che cos'è", "cosa è", "identifica", "che modello", "che marca", "was ist das", "was sehe ich", "was ist es", "welches modell", "qu'est-ce que c'est", "qu'est-ce que je regarde", "quel modèle", "qué es esto", "qué es eso", "qué estoy mirando", "qué modelo" } },
        };

        private static readonly Dictionary<string, string[]> LanguageHints = new Dictionary<string, string[]>
        {
            ["it"] = new[] { "cosa", "cos'", "quanto", "dove", "dov'", "leggi", "traduci", "questo", "quello", "sto", "quale", "quanti", "quante", "perché", "grazie", "spiega", "descrivi", "mostrami", "sedie", "tavolo", "telefono" },
            ["de"] = new[] { "was", "wie", "wo", "ist", "das", "lies", "übersetz", "kostet", "zeig", "welches", "warum", "danke", "erklär", "beschreib", "stühle", "tisch", "telefon" },
            ["fr"] = new[] { "qu'est", "combien", "où", "lis", "traduis", "ceci", "cela", "quel", "pourquoi", "merci", "explique", "décris", "montre", "chaises", "table", "téléphone" },
            ["es"] = new[] { "qué", "cuánto", "dónde", "lee", "traduce", "esto", "eso", "cuál", "por qué", "gracias", "explica", "describe", "muéstrame", "sillas", "mesa", "teléfono" },
            ["en"] = new[] { "what", "how", "where", "read", "translate", "this", "that", "which", "why", "thanks", "explain", "describe", "show", "chairs", "table", "phone", "looking" }
        };

        public struct Result
        {
            public Intent Intent;
            public float Confidence;
            public string LanguageCode;
        }

        public static Result Detect(string text, string languageHint = null)
        {
            var r = new Result { Intent = Intent.GENERAL_CONVERSATION, Confidence = 0.35f, LanguageCode = string.IsNullOrEmpty(languageHint) || languageHint == "auto" ? DetectLanguage(text) : languageHint };
            if (string.IsNullOrWhiteSpace(text)) return r;
            string lower = " " + Normalize(text) + " ";

            int bestScore = 0; Intent best = Intent.GENERAL_CONVERSATION; int hits = 0;
            foreach (var rule in Rules)
            {
                foreach (var kw in rule.Keywords)
                {
                    if (lower.Contains(kw))
                    {
                        int score = rule.Weight * 10 + kw.Length;
                        if (score > bestScore) { bestScore = score; best = rule.Intent; }
                        hits++;
                        break;
                    }
                }
            }

            // "quanto costa" implies search but the target still needs identification; the orchestrator handles the chain.
            if (bestScore > 0)
            {
                r.Intent = best;
                r.Confidence = Math.Min(0.95f, 0.55f + bestScore / 100f);
            }
            else if (lower.Trim().EndsWith("?") || lower.Contains(" this ") || lower.Contains(" that ") || lower.Contains(" questo ") || lower.Contains(" quello "))
            {
                r.Intent = Intent.IDENTIFY; r.Confidence = 0.45f;
            }
            return r;
        }

        public static string DetectLanguage(string text)
        {
            if (string.IsNullOrEmpty(text)) return "en";
            string lower = " " + Normalize(text) + " ";
            string best = "en"; int bestHits = 0;
            foreach (var kv in LanguageHints)
            {
                int h = 0;
                foreach (var w in kv.Value) if (lower.Contains(" " + w) ) h++;
                if (h > bestHits) { bestHits = h; best = kv.Key; }
            }
            return best;
        }

        /// <summary>True when the request needs a camera frame to be answered.</summary>
        public static bool NeedsVision(Intent intent, string text)
        {
            switch (intent)
            {
                case Intent.IDENTIFY:
                case Intent.DESCRIBE:
                case Intent.OCR:
                case Intent.TRANSLATE:
                case Intent.COUNT:
                    return true;
                case Intent.COMPARE:
                case Intent.EXPLAIN:
                case Intent.SEARCH:
                    // needs vision only if it refers to something not yet in memory; orchestrator decides with memory
                    return false;
                default:
                    return false;
            }
        }

        public static bool NeedsSearch(Intent intent) => intent == Intent.SEARCH;

        private static string Normalize(string s)
        {
            s = s.ToLowerInvariant().Trim();
            s = s.Replace("’", "'").Replace("‘", "'");
            return s;
        }
    }
}
