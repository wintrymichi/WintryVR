using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WintryVR.Core;
using WintryVR.Networking;

namespace WintryVR.AI.Providers
{
    /// <summary>
    /// Deterministic offline provider used by Demo Mode and when nothing is configured. It understands the
    /// structured-answer contract used by <see cref="WintryVR.AI.AssistantService"/> so the full pipeline
    /// (intent → vision → answer → card → speech) can be exercised without any network.
    /// </summary>
    public class MockAIProvider : IAIProvider
    {
        public string Name => "Mock";
        public string Model => "mock-1";
        public bool SupportsVision => true;
        public bool RequiresNetwork => false;
        public bool IsConfigured => true;
        public int SimulatedLatencyMs = 350;

        public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken ct)
        {
            await Task.Delay(SimulatedLatencyMs, ct);
            string user = "";
            bool hasImage = false;
            foreach (var m in request.Messages)
            {
                if (m.Role == AIRole.User) user = m.PlainText;
                foreach (var p in m.Parts) if (p.IsImage) hasImage = true;
            }
            string lower = user.ToLowerInvariant();
            string lang = DetectLang(lower, request.SystemPrompt);

            var answer = new Dictionary<string, object> { ["language"] = lang };

            if (lower.Contains("\"intent\"") || lower.Contains("classify"))
            {
                answer["intent"] = "GENERAL_CONVERSATION";
            }
            else if (lower.Contains("[vision]"))
            {
                answer["scene"] = "A desk with a camera, a phone and a notebook.";
                answer["objects"] = new List<object>
                {
                    Obj("camera", "Canon EOS R6 Mark II", "electronics", 0.93, 0.42, 0.35, 0.22, 0.28),
                    Obj("phone", "Smartphone", "electronics", 0.81, 0.68, 0.5, 0.1, 0.18),
                    Obj("notebook", "Notebook", "stationery", 0.77, 0.15, 0.55, 0.2, 0.2)
                };
            }
            else if (lower.Contains("[ocr]"))
            {
                answer["text"] = "Opening hours\nMon-Fri 9:00 - 18:00\nSat 9:00 - 13:00";
                answer["source_language"] = "en";
                answer["blocks"] = new List<object> { new Dictionary<string, object> { ["text"] = "Opening hours", ["x"] = 0.3, ["y"] = 0.3, ["w"] = 0.4, ["h"] = 0.08 } };
            }
            else if (lower.Contains("[translate]"))
            {
                answer["translated"] = Translate(user, lang);
                answer["source_language"] = "en";
            }
            else if (lower.Contains("[search-summary]"))
            {
                answer["speech"] = Speak(lang, "search");
                answer["card_title"] = "Canon EOS R6 Mark II";
                answer["card_subtitle"] = "Mirrorless camera";
                answer["card_lines"] = new List<object> { "≈ 2.499 € (body)", "Available online", "Source: demo store" };
            }
            else if (lower.Contains("quanto costa") || lower.Contains("how much") || lower.Contains("price") || lower.Contains("prezzo") || lower.Contains("kostet") || lower.Contains("combien") || lower.Contains("cuánto"))
            {
                answer["speech"] = Speak(lang, "needsSearch");
                answer["needs_search"] = true;
                answer["search_query"] = "Canon EOS R6 Mark II price";
            }
            else if (hasImage || lower.Contains("cosa sto guardando") || lower.Contains("what am i looking") || lower.Contains("what is this") || lower.Contains("cos'è"))
            {
                answer["speech"] = Speak(lang, "identify");
                answer["object_name"] = "Canon EOS R6 Mark II";
                answer["object_category"] = "camera";
                answer["confidence"] = 0.94;
                answer["card_title"] = "Canon EOS R6 Mark II";
                answer["card_subtitle"] = "Mirrorless Camera";
                answer["card_lines"] = new List<object> { "24.2 MP", "Full Frame", "4K 60p video" };
                answer["focus_x"] = 0.53; answer["focus_y"] = 0.49;
            }
            else
            {
                answer["speech"] = Speak(lang, "general");
            }

            return new AIResponse { Success = true, Text = MiniJson.Serialize(answer), ProviderName = Name, Model = Model, LatencyMs = SimulatedLatencyMs };
        }

        private static Dictionary<string, object> Obj(string label, string identity, string cat, double conf, double x, double y, double w, double h)
            => new Dictionary<string, object> { ["label"] = label, ["identity"] = identity, ["category"] = cat, ["confidence"] = conf, ["x"] = x, ["y"] = y, ["w"] = w, ["h"] = h };

        private static string DetectLang(string lower, string system)
        {
            if (lower.Contains("cosa") || lower.Contains("quanto") || lower.Contains("dov'è") || lower.Contains("leggi")) return "it";
            if (lower.Contains("was ist") || lower.Contains("wo ist") || lower.Contains("kostet")) return "de";
            if (lower.Contains("qu'est") || lower.Contains("combien") || lower.Contains("où est")) return "fr";
            if (lower.Contains("qué es") || lower.Contains("cuánto") || lower.Contains("dónde")) return "es";
            if (system != null && system.Contains("Reply language: it")) return "it";
            return "en";
        }

        private static string Speak(string lang, string kind)
        {
            switch (kind)
            {
                case "identify":
                    switch (lang)
                    {
                        case "it": return "Sembra una Canon EOS R6 Mark II, una mirrorless full frame.";
                        case "de": return "Das sieht aus wie eine Canon EOS R6 Mark II, eine spiegellose Vollformatkamera.";
                        case "fr": return "On dirait un Canon EOS R6 Mark II, un hybride plein format.";
                        case "es": return "Parece una Canon EOS R6 Mark II, una cámara sin espejo de formato completo.";
                        default: return "It looks like a Canon EOS R6 Mark II, a full-frame mirrorless camera.";
                    }
                case "needsSearch":
                    switch (lang)
                    {
                        case "it": return "Controllo il prezzo attuale.";
                        case "de": return "Ich prüfe den aktuellen Preis.";
                        case "fr": return "Je vérifie le prix actuel.";
                        case "es": return "Compruebo el precio actual.";
                        default: return "Let me check the current price.";
                    }
                case "search":
                    switch (lang)
                    {
                        case "it": return "Oggi il solo corpo costa circa duemilacinquecento euro ed è disponibile online.";
                        case "de": return "Das Gehäuse kostet heute etwa zweitausendfünfhundert Euro und ist online verfügbar.";
                        case "fr": return "Aujourd'hui le boîtier coûte environ deux mille cinq cents euros et il est disponible en ligne.";
                        case "es": return "Hoy el cuerpo cuesta unos dos mil quinientos euros y está disponible en línea.";
                        default: return "Today the body costs around twenty-five hundred euros and it's available online.";
                    }
                default:
                    switch (lang)
                    {
                        case "it": return "Certo. Dimmi cosa vuoi sapere su ciò che vedi.";
                        case "de": return "Gern. Sag mir, was du über das wissen willst, was du siehst.";
                        case "fr": return "Bien sûr. Dis-moi ce que tu veux savoir sur ce que tu vois.";
                        case "es": return "Claro. Dime qué quieres saber sobre lo que ves.";
                        default: return "Sure. Tell me what you'd like to know about what you see.";
                    }
            }
        }

        private static string Translate(string user, string lang)
        {
            switch (lang)
            {
                case "it": return "Orari di apertura\nLun-Ven 9:00 - 18:00\nSab 9:00 - 13:00";
                case "de": return "Öffnungszeiten\nMo-Fr 9:00 - 18:00\nSa 9:00 - 13:00";
                case "fr": return "Horaires d'ouverture\nLun-Ven 9:00 - 18:00\nSam 9:00 - 13:00";
                case "es": return "Horario de apertura\nLun-Vie 9:00 - 18:00\nSáb 9:00 - 13:00";
                default: return "Opening hours\nMon-Fri 9:00 - 18:00\nSat 9:00 - 13:00";
            }
        }
    }
}
