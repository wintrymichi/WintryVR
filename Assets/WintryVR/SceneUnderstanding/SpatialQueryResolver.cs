using System.Collections.Generic;
using UnityEngine;
using WintryVR.AI;
using WintryVR.Core;

namespace WintryVR.SceneUnderstanding
{
    /// <summary>
    /// Answers spatial questions locally from the scene graph and context memory, without the cloud:
    /// "Where is my phone?", "How many chairs are there?", "Which is the nearest door?", "What's on my left?",
    /// "What's on the table?", "Show me the window". Returns a target to point at when relevant.
    /// </summary>
    public class SpatialQueryResolver
    {
        public class Answer
        {
            public bool Handled;
            public string Speech = "";
            public bool HasTarget;
            public Vector3 Target;
            public string TargetLabel = "";
            public List<ObservedObject> Objects = new List<ObservedObject>();
            public bool NeedsVision;   // could not answer from memory: ask the vision pipeline
        }

        private readonly ISpatialService _spatial;
        private readonly ISceneUnderstandingService _scene;
        private readonly IMemoryService _memory;

        public SpatialQueryResolver(ISpatialService spatial, ISceneUnderstandingService scene, IMemoryService memory)
        {
            _spatial = spatial; _scene = scene; _memory = memory;
        }

        public Answer Resolve(Intent intent, string text, string lang)
        {
            var a = new Answer();
            string t = (text ?? "").ToLowerInvariant();
            switch (intent)
            {
                case Intent.LOCATE: Locate(t, lang, a); break;
                case Intent.COUNT: Count(t, lang, a); break;
                case Intent.NAVIGATE: Nearest(t, lang, a); break;
                case Intent.DESCRIBE: DescribeRegion(t, lang, a); break;
            }
            return a;
        }

        // ---------------------------------------------------------------- LOCATE
        private void Locate(string t, string lang, Answer a)
        {
            a.Handled = true;
            // 1. remembered object
            string canonical = ContextMemory.CanonicalLabel(t);
            ObservedObject obj = canonical != null ? _memory.FindByName(canonical) : null;
            if (obj == null)
            {
                var refs = _memory.ResolveReference(t, lang, _spatial.HeadPosition, _spatial.GazeDirection);
                if (refs.Count > 0 && (canonical == null || string.Equals(refs[0].Label, canonical, System.StringComparison.OrdinalIgnoreCase))) obj = refs[0];
            }
            if (obj != null && obj.HasPosition)
            {
                a.HasTarget = true; a.Target = obj.WorldPosition; a.TargetLabel = obj.DisplayName; a.Objects.Add(obj);
                _memory.SetFocus(obj);
                string surface = FindSurfaceName(obj.WorldPosition);
                a.Speech = Phrase(lang, "found", obj.DisplayName, _spatial.DescribeDirection(obj.WorldPosition, lang), surface);
                return;
            }
            // 2. room feature (door, window, table, tv ...)
            var kind = KindFromText(t);
            if (kind != SceneEntityKind.Other)
            {
                var nearest = NearestOfKind(kind);
                if (nearest != null)
                {
                    a.HasTarget = true; a.Target = nearest.Position; a.TargetLabel = nearest.Name;
                    a.Speech = Phrase(lang, "feature", nearest.Name, _spatial.DescribeDirection(nearest.Position, lang), null);
                    return;
                }
            }
            // 3. unknown: ask vision to look for it in the current view
            a.NeedsVision = true;
            a.TargetLabel = canonical ?? "";
            a.Speech = Localization.Get("locate.notSeen", lang);
        }

        // ---------------------------------------------------------------- COUNT
        private void Count(string t, string lang, Answer a)
        {
            var kind = KindFromText(t);
            string canonical = ContextMemory.CanonicalLabel(t);
            int count = 0; string what = "";
            Vector3 centroid = Vector3.zero;
            if (kind != SceneEntityKind.Other && kind != SceneEntityKind.Object)
            {
                foreach (var e in _scene.FindByKind(kind)) { count++; centroid += e.Position; what = e.Name; }
            }
            else if (canonical != null)
            {
                foreach (var o in _memory.Objects) if (string.Equals(o.Label, canonical, System.StringComparison.OrdinalIgnoreCase)) { count++; centroid += o.WorldPosition; what = o.Label; }
            }
            if (count == 0) { a.NeedsVision = true; a.Handled = true; a.TargetLabel = canonical ?? ""; a.Speech = ""; return; }
            a.Handled = true;
            centroid /= count;
            a.HasTarget = count > 0; a.Target = centroid; a.TargetLabel = what;
            a.Speech = CountPhrase(lang, count, canonical ?? what.ToLowerInvariant());
        }

        // ---------------------------------------------------------------- NAVIGATE / nearest
        private void Nearest(string t, string lang, Answer a)
        {
            var kind = KindFromText(t);
            if (kind == SceneEntityKind.Other) return;
            var nearest = NearestOfKind(kind);
            if (nearest == null) return;
            a.Handled = true; a.HasTarget = true; a.Target = nearest.Position; a.TargetLabel = nearest.Name;
            a.Speech = Phrase(lang, "nearest", nearest.Name, _spatial.DescribeDirection(nearest.Position, lang), null);
        }

        // ---------------------------------------------------------------- DESCRIBE a region ("on the table", "on my left")
        private void DescribeRegion(string t, string lang, Answer a)
        {
            RelativeDirection? dir = null;
            if (t.Contains("left") || t.Contains("sinistra") || t.Contains("links") || t.Contains("gauche") || t.Contains("izquierda")) dir = RelativeDirection.Left;
            else if (t.Contains("right") || t.Contains("destra") || t.Contains("rechts") || t.Contains("droite") || t.Contains("derecha")) dir = RelativeDirection.Right;
            else if (t.Contains("behind") || t.Contains("dietro") || t.Contains("hinter") || t.Contains("derrière") || t.Contains("detrás")) dir = RelativeDirection.Back;

            var kind = KindFromText(t);
            var names = new List<string>();
            if (kind != SceneEntityKind.Other && kind != SceneEntityKind.Object)
            {
                // "what's on the table"
                var surface = NearestOfKind(kind);
                if (surface == null) return;
                foreach (var c in surface.Children) names.Add(c.Name);
                a.Handled = true;
                a.HasTarget = true; a.Target = surface.Position; a.TargetLabel = surface.Name;
                if (names.Count == 0) { a.NeedsVision = true; a.Speech = ""; return; }
                a.Speech = ListPhrase(lang, surface.Name, names);
                return;
            }
            if (dir.HasValue)
            {
                foreach (var e in _scene.Entities)
                {
                    if (e.Kind == SceneEntityKind.Room || e.Kind == SceneEntityKind.Floor || e.Kind == SceneEntityKind.Ceiling || e.Kind == SceneEntityKind.Wall) continue;
                    var d = _spatial.DirectionTo(e.Position);
                    bool match = dir == RelativeDirection.Left ? (d == RelativeDirection.Left || d == RelativeDirection.FrontLeft || d == RelativeDirection.BackLeft)
                               : dir == RelativeDirection.Right ? (d == RelativeDirection.Right || d == RelativeDirection.FrontRight || d == RelativeDirection.BackRight)
                               : (d == RelativeDirection.Back || d == RelativeDirection.BackLeft || d == RelativeDirection.BackRight);
                    if (match && _spatial.DistanceTo(e.Position) < 4f) names.Add(e.Name);
                }
                a.Handled = true;
                if (names.Count == 0) { a.NeedsVision = true; a.Speech = ""; return; }
                a.Speech = ListPhrase(lang, null, names, dir.Value);
            }
        }

        // ---------------------------------------------------------------- helpers
        private SceneEntity NearestOfKind(SceneEntityKind kind)
        {
            SceneEntity best = null; float bestDist = float.MaxValue;
            foreach (var e in _scene.FindByKind(kind))
            {
                float d = _spatial.DistanceTo(e.Position);
                if (d < bestDist) { bestDist = d; best = e; }
            }
            return best;
        }

        private string FindSurfaceName(Vector3 pos)
        {
            foreach (var e in _scene.Entities)
            {
                if (!e.IsSurface || e.Kind == SceneEntityKind.Floor) continue;
                var b = e.WorldBounds; b.Expand(new Vector3(0.1f, 0.6f, 0.1f));
                if (b.Contains(pos)) return e.Name;
            }
            return null;
        }

        public static SceneEntityKind KindFromText(string t)
        {
            if (Has(t, "door", "porta", "tür", "porte", "puerta")) return SceneEntityKind.Door;
            if (Has(t, "window", "finestra", "fenster", "fenêtre", "ventana")) return SceneEntityKind.Window;
            if (Has(t, "table", "desk", "tavolo", "scrivania", "tisch", "mesa", "bureau")) return SceneEntityKind.Table;
            if (Has(t, "chair", "sedia", "sedie", "stuhl", "stühle", "chaise", "silla")) return SceneEntityKind.Chair;
            if (Has(t, "couch", "sofa", "divano", "canapé", "sofá")) return SceneEntityKind.Couch;
            if (Has(t, "bed", "letto", "bett", "lit", "cama")) return SceneEntityKind.Bed;
            if (Has(t, "tv", "television", "screen", "televisore", "schermo", "fernseher", "bildschirm", "télé", "écran", "pantalla")) return SceneEntityKind.Screen;
            if (Has(t, "lamp", "lampada", "lampe", "lámpara")) return SceneEntityKind.Lamp;
            if (Has(t, "plant", "pianta", "pflanze", "plante", "planta")) return SceneEntityKind.Plant;
            if (Has(t, "shelf", "cabinet", "wardrobe", "scaffale", "armadio", "regal", "schrank", "étagère", "armoire", "estante", "armario")) return SceneEntityKind.Storage;
            if (Has(t, "floor", "pavimento", "boden", "sol", "suelo")) return SceneEntityKind.Floor;
            if (Has(t, "wall", "muro", "parete", "wand", "mur", "pared")) return SceneEntityKind.Wall;
            return SceneEntityKind.Other;
        }

        private static bool Has(string t, params string[] words)
        {
            foreach (var w in words)
            {
                int i = t.IndexOf(w, System.StringComparison.Ordinal);
                while (i >= 0)
                {
                    bool s = i == 0 || !char.IsLetter(t[i - 1]);
                    bool e = i + w.Length >= t.Length || !char.IsLetter(t[i + w.Length]);
                    if (s && e) return true;
                    i = t.IndexOf(w, i + 1, System.StringComparison.Ordinal);
                }
            }
            return false;
        }

        private static string Phrase(string lang, string kind, string name, string where, string surface)
        {
            string l = Localization.Normalize(lang);
            string onSurface = "";
            if (!string.IsNullOrEmpty(surface))
                onSurface = l == "it" ? ", su " + surface.ToLowerInvariant() : l == "de" ? ", auf " + surface : l == "fr" ? ", sur " + surface.ToLowerInvariant() : l == "es" ? ", en " + surface.ToLowerInvariant() : ", on the " + surface.ToLowerInvariant();
            switch (kind)
            {
                case "found":
                    return l == "it" ? name + " è " + where + onSurface + "." : l == "de" ? name + " ist " + where + onSurface + "." : l == "fr" ? name + " est " + where + onSurface + "." : l == "es" ? name + " está " + where + onSurface + "." : "Your " + name.ToLowerInvariant() + " is " + where + onSurface + ".";
                case "nearest":
                    return l == "it" ? "La più vicina è " + where + ": " + name + "." : l == "de" ? "Am nächsten ist " + name + ", " + where + "." : l == "fr" ? "Le plus proche est " + where + " : " + name + "." : l == "es" ? "La más cercana está " + where + ": " + name + "." : "The nearest one is " + where + ": " + name + ".";
                default:
                    return l == "it" ? name + " è " + where + "." : l == "de" ? name + " ist " + where + "." : l == "fr" ? name + " est " + where + "." : l == "es" ? name + " está " + where + "." : "The " + name.ToLowerInvariant() + " is " + where + ".";
            }
        }

        private static string CountPhrase(string lang, int n, string what)
        {
            string l = Localization.Normalize(lang);
            string w = ContextMemory.LocalizedLabel(what, l);
            switch (l)
            {
                case "it": return n == 1 ? "Ne vedo una: " + w + "." : "Ne conto " + n + " (" + w + ")." ;
                case "de": return n == 1 ? "Ich sehe eins: " + w + "." : "Ich zähle " + n + " (" + w + ").";
                case "fr": return n == 1 ? "J'en vois un : " + w + "." : "J'en compte " + n + " (" + w + ").";
                case "es": return n == 1 ? "Veo uno: " + w + "." : "Cuento " + n + " (" + w + ").";
                default: return n == 1 ? "I can see one " + w + "." : "I count " + n + " " + w + (w.EndsWith("s") ? "" : "s") + ".";
            }
        }

        private static string ListPhrase(string lang, string surface, List<string> names, RelativeDirection dir = RelativeDirection.Front)
        {
            string l = Localization.Normalize(lang);
            string joined = string.Join(", ", names);
            if (surface != null)
                return l == "it" ? "Su " + surface.ToLowerInvariant() + " vedo: " + joined + "." : l == "de" ? "Auf dem " + surface + " sehe ich: " + joined + "." : l == "fr" ? "Sur " + surface.ToLowerInvariant() + " je vois : " + joined + "." : l == "es" ? "En " + surface.ToLowerInvariant() + " veo: " + joined + "." : "On the " + surface.ToLowerInvariant() + " I can see: " + joined + ".";
            string side = WintryVR.Spatial.SpatialService.DirWord(l, dir);
            return l == "it" ? "" + side.Substring(0, 1).ToUpperInvariant() + side.Substring(1) + " ci sono: " + joined + "." : l == "de" ? side.Substring(0, 1).ToUpperInvariant() + side.Substring(1) + " sind: " + joined + "." : l == "fr" ? side.Substring(0, 1).ToUpperInvariant() + side.Substring(1) + " il y a : " + joined + "." : l == "es" ? side.Substring(0, 1).ToUpperInvariant() + side.Substring(1) + " hay: " + joined + "." : side.Substring(0, 1).ToUpperInvariant() + side.Substring(1) + " there are: " + joined + ".";
        }
    }
}
