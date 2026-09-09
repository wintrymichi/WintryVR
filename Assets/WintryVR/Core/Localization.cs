using System.Collections.Generic;

namespace WintryVR.Core
{
    /// <summary>
    /// User-facing system phrases in the five supported languages. AI answers are generated in the
    /// user's language by the model; these strings cover onboarding, errors and status messages that
    /// must work offline.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _table = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["app.tagline"] = "Your world, understood.",
                ["onboard.look"] = "Look around.",
                ["onboard.hi"] = "Hi. I'm Wintry.",
                ["onboard.ask"] = "Ask me anything about what you see.",
                ["onboard.wake"] = "Say \"Hey Wintry\" or pinch to talk.",
                ["status.offline"] = "Offline Mode",
                ["status.listening"] = "Listening…",
                ["status.thinking"] = "Thinking…",
                ["status.looking"] = "Looking…",
                ["status.searching"] = "Searching…",
                ["err.identify"] = "Sorry, I couldn't identify that.",
                ["err.identify.hint1"] = "Try getting a little closer.",
                ["err.identify.hint2"] = "Ask me to describe it instead.",
                ["err.offline"] = "I can't access live information right now.",
                ["err.noText"] = "I couldn't find any readable text there.",
                ["err.noCamera"] = "I can't see right now. Camera access is off.",
                ["err.noMic"] = "I can't hear you. Microphone access is off.",
                ["err.generic"] = "Something went wrong on my side. Let's try again.",
                ["err.unsure"] = "I'm not completely sure, but",
                ["ctx.cleared"] = "Okay, I've cleared what we were talking about.",
                ["ctx.forgot"] = "Forgotten.",
                ["locate.notSeen"] = "I haven't seen that yet. Look around and I'll keep an eye out.",
                ["locate.found"] = "It's over there.",
                ["card.more"] = "Tell me more",
                ["you"] = "YOU",
                ["wintry"] = "WINTRY",
                ["look.confirm"] = "Here's a new look. Should I apply it?",
                ["look.applied"] = "Done. How do I look?",
                ["safety.medical"] = "This is general information, not medical advice.",
                ["safety.legal"] = "This is general information, not legal advice.",
                ["safety.safety"] = "Please be careful and follow official safety guidance."
            },
            ["it"] = new Dictionary<string, string>
            {
                ["app.tagline"] = "Il tuo mondo, compreso.",
                ["onboard.look"] = "Guardati intorno.",
                ["onboard.hi"] = "Ciao. Sono Wintry.",
                ["onboard.ask"] = "Chiedimi qualsiasi cosa su ciò che vedi.",
                ["onboard.wake"] = "Dì \"Hey Wintry\" o fai un pinch per parlare.",
                ["status.offline"] = "Modalità Offline",
                ["status.listening"] = "Ti ascolto…",
                ["status.thinking"] = "Ci penso…",
                ["status.looking"] = "Guardo…",
                ["status.searching"] = "Cerco…",
                ["err.identify"] = "Mi dispiace, non sono riuscito a identificarlo.",
                ["err.identify.hint1"] = "Prova ad avvicinarti un po'.",
                ["err.identify.hint2"] = "Chiedimi di descriverlo, invece.",
                ["err.offline"] = "In questo momento non posso accedere a informazioni aggiornate.",
                ["err.noText"] = "Non ho trovato testo leggibile lì.",
                ["err.noCamera"] = "Adesso non riesco a vedere. L'accesso alla camera è disattivato.",
                ["err.noMic"] = "Non riesco a sentirti. L'accesso al microfono è disattivato.",
                ["err.generic"] = "Qualcosa è andato storto da parte mia. Riproviamo.",
                ["err.unsure"] = "Non ne sono del tutto sicuro, ma",
                ["ctx.cleared"] = "Ok, ho dimenticato di cosa stavamo parlando.",
                ["ctx.forgot"] = "Dimenticato.",
                ["locate.notSeen"] = "Non l'ho ancora visto. Guardati intorno e ci farò attenzione.",
                ["locate.found"] = "È lì.",
                ["card.more"] = "Dimmi di più",
                ["you"] = "TU",
                ["wintry"] = "WINTRY",
                ["look.confirm"] = "Ecco un nuovo look. Lo applico?",
                ["look.applied"] = "Fatto. Come sto?",
                ["safety.medical"] = "Queste sono informazioni generali, non un consiglio medico.",
                ["safety.legal"] = "Queste sono informazioni generali, non un consiglio legale.",
                ["safety.safety"] = "Fai attenzione e segui le indicazioni ufficiali di sicurezza."
            },
            ["de"] = new Dictionary<string, string>
            {
                ["app.tagline"] = "Deine Welt, verstanden.",
                ["onboard.look"] = "Schau dich um.",
                ["onboard.hi"] = "Hallo. Ich bin Wintry.",
                ["onboard.ask"] = "Frag mich alles über das, was du siehst.",
                ["onboard.wake"] = "Sag \"Hey Wintry\" oder pinche zum Sprechen.",
                ["status.offline"] = "Offline-Modus",
                ["status.listening"] = "Ich höre zu…",
                ["status.thinking"] = "Ich denke nach…",
                ["status.looking"] = "Ich schaue…",
                ["status.searching"] = "Ich suche…",
                ["err.identify"] = "Entschuldigung, das konnte ich nicht erkennen.",
                ["err.identify.hint1"] = "Geh etwas näher heran.",
                ["err.identify.hint2"] = "Bitte mich stattdessen, es zu beschreiben.",
                ["err.offline"] = "Ich habe gerade keinen Zugriff auf aktuelle Informationen.",
                ["err.noText"] = "Dort konnte ich keinen lesbaren Text finden.",
                ["err.noCamera"] = "Ich kann gerade nichts sehen. Der Kamerazugriff ist aus.",
                ["err.noMic"] = "Ich kann dich nicht hören. Der Mikrofonzugriff ist aus.",
                ["err.generic"] = "Bei mir ist etwas schiefgelaufen. Versuchen wir es noch einmal.",
                ["err.unsure"] = "Ich bin mir nicht ganz sicher, aber",
                ["ctx.cleared"] = "Okay, ich habe unser Thema vergessen.",
                ["ctx.forgot"] = "Vergessen.",
                ["locate.notSeen"] = "Das habe ich noch nicht gesehen. Schau dich um, ich achte darauf.",
                ["locate.found"] = "Es ist dort drüben.",
                ["card.more"] = "Mehr erzählen",
                ["you"] = "DU",
                ["wintry"] = "WINTRY",
                ["look.confirm"] = "Hier ist ein neuer Look. Soll ich ihn anwenden?",
                ["look.applied"] = "Fertig. Wie sehe ich aus?",
                ["safety.medical"] = "Das sind allgemeine Informationen, keine medizinische Beratung.",
                ["safety.legal"] = "Das sind allgemeine Informationen, keine Rechtsberatung.",
                ["safety.safety"] = "Bitte sei vorsichtig und befolge offizielle Sicherheitshinweise."
            },
            ["fr"] = new Dictionary<string, string>
            {
                ["app.tagline"] = "Votre monde, compris.",
                ["onboard.look"] = "Regardez autour de vous.",
                ["onboard.hi"] = "Salut. Je suis Wintry.",
                ["onboard.ask"] = "Demandez-moi tout ce que vous voulez sur ce que vous voyez.",
                ["onboard.wake"] = "Dites « Hey Wintry » ou pincez pour parler.",
                ["status.offline"] = "Mode hors ligne",
                ["status.listening"] = "J'écoute…",
                ["status.thinking"] = "Je réfléchis…",
                ["status.looking"] = "Je regarde…",
                ["status.searching"] = "Je cherche…",
                ["err.identify"] = "Désolé, je n'ai pas pu l'identifier.",
                ["err.identify.hint1"] = "Essayez de vous rapprocher un peu.",
                ["err.identify.hint2"] = "Demandez-moi plutôt de le décrire.",
                ["err.offline"] = "Je ne peux pas accéder aux informations en direct pour le moment.",
                ["err.noText"] = "Je n'ai trouvé aucun texte lisible ici.",
                ["err.noCamera"] = "Je ne peux pas voir pour le moment. L'accès à la caméra est désactivé.",
                ["err.noMic"] = "Je ne vous entends pas. L'accès au micro est désactivé.",
                ["err.generic"] = "Quelque chose s'est mal passé de mon côté. Réessayons.",
                ["err.unsure"] = "Je ne suis pas tout à fait sûr, mais",
                ["ctx.cleared"] = "D'accord, j'ai oublié notre sujet.",
                ["ctx.forgot"] = "Oublié.",
                ["locate.notSeen"] = "Je ne l'ai pas encore vu. Regardez autour de vous, je resterai attentif.",
                ["locate.found"] = "C'est là-bas.",
                ["card.more"] = "En savoir plus",
                ["you"] = "VOUS",
                ["wintry"] = "WINTRY",
                ["look.confirm"] = "Voici un nouveau look. Je l'applique ?",
                ["look.applied"] = "C'est fait. Comment je suis ?",
                ["safety.medical"] = "Ce sont des informations générales, pas un avis médical.",
                ["safety.legal"] = "Ce sont des informations générales, pas un avis juridique.",
                ["safety.safety"] = "Soyez prudent et suivez les consignes de sécurité officielles."
            },
            ["es"] = new Dictionary<string, string>
            {
                ["app.tagline"] = "Tu mundo, comprendido.",
                ["onboard.look"] = "Mira a tu alrededor.",
                ["onboard.hi"] = "Hola. Soy Wintry.",
                ["onboard.ask"] = "Pregúntame lo que quieras sobre lo que ves.",
                ["onboard.wake"] = "Di \"Hey Wintry\" o haz un pinch para hablar.",
                ["status.offline"] = "Modo sin conexión",
                ["status.listening"] = "Escuchando…",
                ["status.thinking"] = "Pensando…",
                ["status.looking"] = "Mirando…",
                ["status.searching"] = "Buscando…",
                ["err.identify"] = "Lo siento, no pude identificarlo.",
                ["err.identify.hint1"] = "Intenta acercarte un poco.",
                ["err.identify.hint2"] = "Pídeme que lo describa.",
                ["err.offline"] = "Ahora mismo no puedo acceder a información actualizada.",
                ["err.noText"] = "No encontré texto legible ahí.",
                ["err.noCamera"] = "Ahora no puedo ver. El acceso a la cámara está desactivado.",
                ["err.noMic"] = "No puedo oírte. El acceso al micrófono está desactivado.",
                ["err.generic"] = "Algo salió mal por mi parte. Intentémoslo de nuevo.",
                ["err.unsure"] = "No estoy del todo seguro, pero",
                ["ctx.cleared"] = "Vale, he olvidado de qué hablábamos.",
                ["ctx.forgot"] = "Olvidado.",
                ["locate.notSeen"] = "Aún no lo he visto. Mira a tu alrededor y estaré atento.",
                ["locate.found"] = "Está allí.",
                ["card.more"] = "Cuéntame más",
                ["you"] = "TÚ",
                ["wintry"] = "WINTRY",
                ["look.confirm"] = "Aquí tienes un nuevo look. ¿Lo aplico?",
                ["look.applied"] = "Hecho. ¿Qué tal me veo?",
                ["safety.medical"] = "Esta es información general, no un consejo médico.",
                ["safety.legal"] = "Esta es información general, no un consejo legal.",
                ["safety.safety"] = "Ten cuidado y sigue las indicaciones oficiales de seguridad."
            }
        };

        public static string CurrentLanguage = "en";

        public static string Get(string key) => Get(key, CurrentLanguage);

        public static string Get(string key, string languageCode)
        {
            string lang = Normalize(languageCode);
            if (_table.TryGetValue(lang, out var t) && t.TryGetValue(key, out var s)) return s;
            if (_table["en"].TryGetValue(key, out var en)) return en;
            return key;
        }

        public static string Normalize(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCode == "auto") return CurrentLanguage ?? "en";
            string l = languageCode.ToLowerInvariant();
            if (l.Length > 2) l = l.Substring(0, 2);
            return _table.ContainsKey(l) ? l : "en";
        }

        public static string FromSystemLanguage(UnityEngine.SystemLanguage sys)
        {
            switch (sys)
            {
                case UnityEngine.SystemLanguage.Italian: return "it";
                case UnityEngine.SystemLanguage.German: return "de";
                case UnityEngine.SystemLanguage.French: return "fr";
                case UnityEngine.SystemLanguage.Spanish: return "es";
                default: return "en";
            }
        }
    }
}
