using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.AI
{
    /// <summary>
    /// Temporary contextual memory. Remembers observed objects (with positions), conversation turns,
    /// search results and which object is "in focus", and resolves references such as "it", "that one",
    /// "the one next to it", "the second one", "the three of them".
    /// </summary>
    public class ContextMemory : IMemoryService
    {
        private readonly List<ObservedObject> _objects = new List<ObservedObject>();
        private readonly List<AssistantTurn> _turns = new List<AssistantTurn>();
        private readonly List<ObservedObject> _recentlyMentioned = new List<ObservedObject>(); // ordered, most recent last
        private ObservedObject _focus;
        private int _counter;

        public int MaxObjects = 40;
        public int MaxTurns = 30;
        public float MergeDistanceMeters = 0.35f;

        public IReadOnlyList<ObservedObject> Objects => _objects;
        public IReadOnlyList<AssistantTurn> Turns => _turns;
        public ObservedObject Focus => _focus;

        public ObservedObject Remember(Detection d, Vector3 worldPosition, bool hasPosition)
        {
            // Merge with an existing object of the same label close by (re-observation)
            if (hasPosition)
            {
                foreach (var o in _objects)
                {
                    if (o.HasPosition && SameLabel(o, d) && Vector3.Distance(o.WorldPosition, worldPosition) < MergeDistanceMeters)
                    {
                        if (d.Confidence >= o.Confidence)
                        {
                            o.Confidence = d.Confidence;
                            if (!string.IsNullOrEmpty(d.Identity)) o.Identity = d.Identity;
                            if (!string.IsNullOrEmpty(d.Description)) o.Description = d.Description;
                        }
                        o.WorldPosition = Vector3.Lerp(o.WorldPosition, worldPosition, 0.5f);
                        o.ObservedAt = DateTime.UtcNow;
                        return o;
                    }
                }
            }
            var obj = new ObservedObject
            {
                Id = "obj_" + (++_counter),
                Label = d.Label,
                Identity = d.Identity,
                Category = d.Category,
                Confidence = d.Confidence,
                Description = d.Description,
                WorldPosition = worldPosition,
                HasPosition = hasPosition,
                ObservedAt = DateTime.UtcNow,
                Attributes = new List<string>(d.Attributes)
            };
            _objects.Add(obj);
            while (_objects.Count > MaxObjects) _objects.RemoveAt(0);
            return obj;
        }

        private static bool SameLabel(ObservedObject o, Detection d)
        {
            if (!string.IsNullOrEmpty(o.Identity) && !string.IsNullOrEmpty(d.Identity)) return string.Equals(o.Identity, d.Identity, StringComparison.OrdinalIgnoreCase);
            return string.Equals(o.Label, d.Label, StringComparison.OrdinalIgnoreCase);
        }

        public void RememberTurn(AssistantTurn turn)
        {
            turn.Time = DateTime.UtcNow;
            _turns.Add(turn);
            while (_turns.Count > MaxTurns) _turns.RemoveAt(0);
            foreach (var id in turn.ReferencedObjectIds)
            {
                var o = FindById(id);
                if (o != null) { _recentlyMentioned.Remove(o); _recentlyMentioned.Add(o); }
            }
            while (_recentlyMentioned.Count > 6) _recentlyMentioned.RemoveAt(0);
        }

        public void RememberSearch(string objectId, SearchResult result)
        {
            var o = FindById(objectId);
            if (o == null || result == null || !result.Success) return;
            if (result.Items.Count > 0)
            {
                var top = result.Items[0];
                if (!string.IsNullOrEmpty(top.Price)) o.Facts["price"] = top.Price;
                if (!string.IsNullOrEmpty(top.Availability)) o.Facts["availability"] = top.Availability;
                if (!string.IsNullOrEmpty(top.Url)) o.Facts["source"] = top.Url;
            }
            if (!string.IsNullOrEmpty(result.Summary)) o.Facts["summary"] = result.Summary;
        }

        public void SetFocus(ObservedObject obj)
        {
            _focus = obj;
            if (obj != null) { _recentlyMentioned.Remove(obj); _recentlyMentioned.Add(obj); }
        }

        public ObservedObject FindById(string id)
        {
            foreach (var o in _objects) if (o.Id == id) return o;
            return null;
        }

        public ObservedObject FindByName(string nameFragment)
        {
            if (string.IsNullOrEmpty(nameFragment)) return null;
            string f = nameFragment.ToLowerInvariant();
            ObservedObject best = null; float bestScore = 0;
            foreach (var o in _objects)
            {
                float s = 0;
                if (!string.IsNullOrEmpty(o.Identity) && o.Identity.ToLowerInvariant().Contains(f)) s = 3;
                else if (!string.IsNullOrEmpty(o.Label) && (o.Label.ToLowerInvariant().Contains(f) || f.Contains(o.Label.ToLowerInvariant()))) s = 2;
                else if (!string.IsNullOrEmpty(o.Category) && f.Contains(o.Category.ToLowerInvariant())) s = 1;
                if (s > 0) s += (float)(1.0 / (1.0 + (DateTime.UtcNow - o.ObservedAt).TotalMinutes));
                if (s > bestScore) { bestScore = s; best = o; }
            }
            return best;
        }

        public List<ObservedObject> ResolveReference(string userText, string languageCode, Vector3 userPosition, Vector3 gazeDirection)
        {
            var result = new List<ObservedObject>();
            string t = (userText ?? "").ToLowerInvariant();

            // 1. explicit names ("the camera", "il telefono")
            foreach (var o in _objects)
            {
                if (!string.IsNullOrEmpty(o.Label) && ContainsWord(t, o.Label.ToLowerInvariant())) result.Add(o);
                else if (!string.IsNullOrEmpty(o.Identity) && t.Contains(o.Identity.ToLowerInvariant())) result.Add(o);
                else if (!string.IsNullOrEmpty(o.Label) && ContainsWord(t, LocalizedLabel(o.Label, languageCode))) result.Add(o);
            }
            if (result.Count > 0) return result;

            // 2. "the second one", "the three", "all of them", "quale dei tre"
            int ordinal = ParseOrdinal(t);
            if (ordinal > 0 && ordinal <= _recentlyMentioned.Count)
            {
                var list = LastObservedGroup();
                if (ordinal <= list.Count) { result.Add(list[ordinal - 1]); return result; }
            }
            int cardinal = ParseCardinal(t);
            if (cardinal > 1)
            {
                var group = LastObservedGroup();
                if (group.Count >= cardinal) { result.AddRange(group.GetRange(group.Count - cardinal, cardinal)); return result; }
            }
            if (t.Contains("all of them") || t.Contains("tutti") || t.Contains("tutte") || t.Contains("alle") || t.Contains("tous") || t.Contains("todos") || t.Contains("todas") || t.Contains("these") || t.Contains("questi") || t.Contains("queste"))
            {
                result.AddRange(LastObservedGroup()); return result;
            }

            // 3. "the one next to it / beside it" -> nearest other object to the focus
            if (_focus != null && (t.Contains("next to") || t.Contains("beside") || t.Contains("accanto") || t.Contains("vicino") || t.Contains("daneben") || t.Contains("à côté") || t.Contains("al lado")))
            {
                ObservedObject nearest = null; float best = float.MaxValue;
                foreach (var o in _objects)
                {
                    if (o == _focus || !o.HasPosition || !_focus.HasPosition) continue;
                    float dist = Vector3.Distance(o.WorldPosition, _focus.WorldPosition);
                    if (dist < best) { best = dist; nearest = o; }
                }
                if (nearest != null) { result.Add(nearest); return result; }
            }

            // 4. "that one over there" -> object closest to gaze
            if (t.Contains("that") || t.Contains("quello") || t.Contains("quella") || t.Contains("das da") || t.Contains("celui-là") || t.Contains("celle-là") || t.Contains("aquel") || t.Contains("ese") || t.Contains("over there") || t.Contains("laggiù"))
            {
                var g = ClosestToGaze(userPosition, gazeDirection, 25f);
                if (g != null) { result.Add(g); return result; }
            }

            // 5. pronouns / implicit -> focus
            if (_focus != null) result.Add(_focus);
            return result;
        }

        public ObservedObject ClosestToGaze(Vector3 userPosition, Vector3 gazeDirection, float maxAngle)
        {
            ObservedObject best = null; float bestAngle = maxAngle;
            foreach (var o in _objects)
            {
                if (!o.HasPosition) continue;
                float a = Vector3.Angle(gazeDirection, o.WorldPosition - userPosition);
                if (a < bestAngle) { bestAngle = a; best = o; }
            }
            return best;
        }

        private List<ObservedObject> LastObservedGroup()
        {
            // objects observed within 20s of the most recent observation
            var group = new List<ObservedObject>();
            if (_objects.Count == 0) return group;
            DateTime latest = DateTime.MinValue;
            foreach (var o in _objects) if (o.ObservedAt > latest) latest = o.ObservedAt;
            foreach (var o in _objects) if ((latest - o.ObservedAt).TotalSeconds < 20) group.Add(o);
            return group;
        }

        public string BuildContext(int maxObjects, int maxTurns)
        {
            var sb = new StringBuilder();
            if (_objects.Count > 0)
            {
                sb.AppendLine("Objects observed recently (id | name | category | confidence | facts):");
                int start = Math.Max(0, _objects.Count - maxObjects);
                for (int i = start; i < _objects.Count; i++)
                {
                    var o = _objects[i];
                    sb.Append("- ").Append(o.Id).Append(" | ").Append(o.DisplayName).Append(" | ").Append(o.Category ?? "").Append(" | ").Append(Mathf.RoundToInt(o.Confidence * 100)).Append('%');
                    if (o == _focus) sb.Append(" | IN FOCUS");
                    foreach (var f in o.Facts) sb.Append(" | ").Append(f.Key).Append('=').Append(f.Value);
                    sb.AppendLine();
                }
            }
            if (_turns.Count > 0)
            {
                sb.AppendLine("Recent conversation:");
                int start = Math.Max(0, _turns.Count - maxTurns);
                for (int i = start; i < _turns.Count; i++)
                {
                    var t = _turns[i];
                    sb.Append("User: ").AppendLine(t.UserText);
                    sb.Append("Wintry: ").AppendLine(t.AssistantText);
                }
            }
            return sb.ToString();
        }

        public void ClearContext()
        {
            _objects.Clear(); _turns.Clear(); _recentlyMentioned.Clear(); _focus = null;
            WintryLog.I("Memory", "Context cleared");
        }

        public void ForgetCurrentContext()
        {
            if (_focus != null) { _objects.Remove(_focus); _recentlyMentioned.Remove(_focus); }
            _focus = null;
            if (_turns.Count > 0) _turns.RemoveAt(_turns.Count - 1);
        }

        public void ClearHistory() { _turns.Clear(); }

        // ------------------------------------------------------------------ parsing helpers
        private static bool ContainsWord(string text, string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            int idx = text.IndexOf(word, StringComparison.Ordinal);
            while (idx >= 0)
            {
                bool startOk = idx == 0 || !char.IsLetter(text[idx - 1]);
                bool endOk = idx + word.Length >= text.Length || !char.IsLetter(text[idx + word.Length]);
                if (startOk && endOk) return true;
                idx = text.IndexOf(word, idx + 1, StringComparison.Ordinal);
            }
            return false;
        }

        private static readonly Dictionary<string, string[]> LabelSynonyms = new Dictionary<string, string[]>
        {
            ["phone"] = new[] { "telefono", "cellulare", "handy", "téléphone", "teléfono", "móvil", "smartphone" },
            ["camera"] = new[] { "macchina fotografica", "fotocamera", "kamera", "appareil photo", "cámara" },
            ["keys"] = new[] { "chiavi", "schlüssel", "clés", "llaves" },
            ["book"] = new[] { "libro", "buch", "livre" },
            ["chair"] = new[] { "sedia", "stuhl", "chaise", "silla" },
            ["table"] = new[] { "tavolo", "tisch", "table", "mesa" },
            ["door"] = new[] { "porta", "tür", "porte", "puerta" },
            ["window"] = new[] { "finestra", "fenster", "fenêtre", "ventana" },
            ["laptop"] = new[] { "portatile", "computer", "ordinateur", "portátil" },
            ["cup"] = new[] { "tazza", "tasse", "taza" },
            ["bottle"] = new[] { "bottiglia", "flasche", "bouteille", "botella" },
            ["notebook"] = new[] { "quaderno", "notizbuch", "cahier", "cuaderno" }
        };

        public static string LocalizedLabel(string label, string languageCode)
        {
            if (label == null) return "";
            if (LabelSynonyms.TryGetValue(label.ToLowerInvariant(), out var syn))
            {
                int idx = languageCode == "it" ? 0 : languageCode == "de" ? 1 : languageCode == "fr" ? 2 : languageCode == "es" ? 3 : -1;
                if (idx >= 0 && idx < syn.Length) return syn[idx];
            }
            return label.ToLowerInvariant();
        }

        /// <summary>Returns the English label for a localized word if we know it (used by LOCATE).</summary>
        public static string CanonicalLabel(string text)
        {
            string t = text.ToLowerInvariant();
            foreach (var kv in LabelSynonyms)
            {
                if (ContainsWord(t, kv.Key)) return kv.Key;
                foreach (var s in kv.Value) if (ContainsWord(t, s)) return kv.Key;
            }
            return null;
        }

        private static int ParseOrdinal(string t)
        {
            string[][] ords =
            {
                new[] { "first", "primo", "prima", "erste", "premier", "première", "primero", "primera" },
                new[] { "second", "secondo", "seconda", "zweite", "deuxième", "segundo", "segunda" },
                new[] { "third", "terzo", "terza", "dritte", "troisième", "tercero", "tercera" },
                new[] { "fourth", "quarto", "quarta", "vierte", "quatrième", "cuarto", "cuarta" }
            };
            for (int i = 0; i < ords.Length; i++) foreach (var w in ords[i]) if (ContainsWord(t, w)) return i + 1;
            return 0;
        }

        private static int ParseCardinal(string t)
        {
            string[][] cards =
            {
                new[] { "two", "due", "zwei", "deux", "dos", "both", "entrambi", "beide", "les deux", "ambos" },
                new[] { "three", "tre", "drei", "trois", "tres" },
                new[] { "four", "quattro", "vier", "quatre", "cuatro" },
                new[] { "five", "cinque", "fünf", "cinq", "cinco" }
            };
            for (int i = 0; i < cards.Length; i++) foreach (var w in cards[i]) if (ContainsWord(t, w)) return i + 2;
            return 0;
        }
    }
}
