using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WintryVR.Networking
{
    /// <summary>
    /// Dependency-free JSON reader/writer. Objects map to Dictionary&lt;string,object&gt;, arrays to List&lt;object&gt;,
    /// numbers to double (or long when integral), plus string/bool/null. Used for every provider payload so
    /// that no vendor schema leaks into the rest of the app.
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            var p = new Parser(json);
            return p.ParseValue();
        }

        public static string Serialize(object obj)
        {
            var sb = new StringBuilder();
            WriteValue(sb, obj);
            return sb.ToString();
        }

        // ------------------------------------------------------------------ helpers
        public static Dictionary<string, object> AsObject(object o) => o as Dictionary<string, object>;
        public static List<object> AsArray(object o) => o as List<object>;

        public static string GetString(object obj, string key, string fallback = null)
        {
            var d = AsObject(obj);
            if (d != null && d.TryGetValue(key, out var v) && v != null) return v is string s ? s : Convert.ToString(v, CultureInfo.InvariantCulture);
            return fallback;
        }

        public static double GetNumber(object obj, string key, double fallback = 0)
        {
            var d = AsObject(obj);
            if (d != null && d.TryGetValue(key, out var v) && v != null)
            {
                if (v is double dd) return dd;
                if (v is long l) return l;
                if (v is int i) return i;
                if (v is float f) return f;
                if (v is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            }
            return fallback;
        }

        public static bool GetBool(object obj, string key, bool fallback = false)
        {
            var d = AsObject(obj);
            if (d != null && d.TryGetValue(key, out var v) && v is bool b) return b;
            return fallback;
        }

        public static Dictionary<string, object> GetObject(object obj, string key)
        {
            var d = AsObject(obj);
            return d != null && d.TryGetValue(key, out var v) ? v as Dictionary<string, object> : null;
        }

        public static List<object> GetArray(object obj, string key)
        {
            var d = AsObject(obj);
            return d != null && d.TryGetValue(key, out var v) ? v as List<object> : null;
        }

        /// <summary>Walks a dotted path like "choices.0.message.content".</summary>
        public static object Path(object root, string path)
        {
            object cur = root;
            foreach (var seg in path.Split('.'))
            {
                if (cur == null) return null;
                if (cur is Dictionary<string, object> d) { if (!d.TryGetValue(seg, out cur)) return null; }
                else if (cur is List<object> l) { if (!int.TryParse(seg, out int idx) || idx < 0 || idx >= l.Count) return null; cur = l[idx]; }
                else return null;
            }
            return cur;
        }

        /// <summary>Extracts the first top-level JSON object embedded in free text (models sometimes wrap JSON in prose or fences).</summary>
        public static Dictionary<string, object> ExtractObject(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int start = text.IndexOf('{');
            while (start >= 0)
            {
                int depth = 0; bool inStr = false; bool esc = false;
                for (int i = start; i < text.Length; i++)
                {
                    char c = text[i];
                    if (inStr) { if (esc) esc = false; else if (c == '\\') esc = true; else if (c == '"') inStr = false; continue; }
                    if (c == '"') inStr = true;
                    else if (c == '{') depth++;
                    else if (c == '}') { depth--; if (depth == 0) { try { return Deserialize(text.Substring(start, i - start + 1)) as Dictionary<string, object>; } catch { break; } } }
                }
                start = text.IndexOf('{', start + 1);
            }
            return null;
        }

        // ------------------------------------------------------------------ writer
        private static void WriteValue(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case IDictionary dict:
                    {
                        sb.Append('{'); bool first = true;
                        foreach (DictionaryEntry e in dict)
                        {
                            if (!first) sb.Append(','); first = false;
                            WriteString(sb, e.Key.ToString()); sb.Append(':'); WriteValue(sb, e.Value);
                        }
                        sb.Append('}'); break;
                    }
                case IEnumerable list:
                    {
                        sb.Append('['); bool first = true;
                        foreach (var e in list) { if (!first) sb.Append(','); first = false; WriteValue(sb, e); }
                        sb.Append(']'); break;
                    }
                default: WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture)); break;
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ------------------------------------------------------------------ parser
        private sealed class Parser
        {
            private readonly string _s; private int _i;
            public Parser(string s) { _s = s; _i = 0; }

            private void SkipWs() { while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++; }
            private char Peek() { SkipWs(); return _i < _s.Length ? _s[_i] : '\0'; }

            public object ParseValue()
            {
                char c = Peek();
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    case '\0': throw new FormatException("Unexpected end of JSON");
                    default: return ParseNumber();
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw new FormatException("Invalid JSON literal at " + _i);
                _i += word.Length;
            }

            private Dictionary<string, object> ParseObject()
            {
                var d = new Dictionary<string, object>();
                _i++; // {
                if (Peek() == '}') { _i++; return d; }
                while (true)
                {
                    if (Peek() != '"') throw new FormatException("Expected string key at " + _i);
                    string key = ParseString();
                    if (Peek() != ':') throw new FormatException("Expected ':' at " + _i);
                    _i++;
                    d[key] = ParseValue();
                    char c = Peek();
                    if (c == ',') { _i++; continue; }
                    if (c == '}') { _i++; return d; }
                    throw new FormatException("Expected ',' or '}' at " + _i);
                }
            }

            private List<object> ParseArray()
            {
                var l = new List<object>();
                _i++; // [
                if (Peek() == ']') { _i++; return l; }
                while (true)
                {
                    l.Add(ParseValue());
                    char c = Peek();
                    if (c == ',') { _i++; continue; }
                    if (c == ']') { _i++; return l; }
                    throw new FormatException("Expected ',' or ']' at " + _i);
                }
            }

            private string ParseString()
            {
                var sb = new StringBuilder();
                _i++; // opening quote
                while (_i < _s.Length)
                {
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (_i >= _s.Length) break;
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw new FormatException("Bad unicode escape");
                            sb.Append((char)Convert.ToInt32(_s.Substring(_i, 4), 16)); _i += 4; break;
                        default: sb.Append(e); break;
                    }
                }
                throw new FormatException("Unterminated string");
            }

            private object ParseNumber()
            {
                SkipWs();
                int start = _i;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (char.IsDigit(c) || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') _i++; else break;
                }
                string tok = _s.Substring(start, _i - start);
                if (tok.Length == 0) throw new FormatException("Invalid JSON token at " + start);
                if (tok.IndexOfAny(new[] { '.', 'e', 'E' }) < 0 && long.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
                if (double.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                throw new FormatException("Invalid number '" + tok + "'");
            }
        }
    }
}
