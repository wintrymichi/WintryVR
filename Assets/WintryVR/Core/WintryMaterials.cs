using UnityEngine;

namespace WintryVR.Core
{
    /// <summary>
    /// Creates mobile-friendly materials for the Core, character, highlights and glass UI. Uses the WintryVR
    /// shaders when present and falls back to URP Unlit/Lit or built-in shaders so nothing renders magenta.
    /// Materials are cached by key to keep draw calls batched.
    /// </summary>
    public static class WintryMaterials
    {
        private static readonly System.Collections.Generic.Dictionary<string, Material> _cache = new System.Collections.Generic.Dictionary<string, Material>();

        public static Shader FindShader(params string[] names)
        {
            foreach (var n in names) { var s = Shader.Find(n); if (s != null) return s; }
            return null;
        }

        public static Material Glow(Color color, Color emission, float strength = 1.5f, float alpha = 1f, float fresnel = 2f)
        {
            var shader = FindShader("WintryVR/Glow", "Universal Render Pipeline/Unlit", "Unlit/Color");
            var m = new Material(shader) { name = "WintryGlow" };
            Set(m, "_Color", color); Set(m, "_BaseColor", color);
            Set(m, "_EmissionColor", emission * strength);
            SetF(m, "_EmissionStrength", strength); SetF(m, "_Fresnel", fresnel); SetF(m, "_Alpha", alpha);
            if (alpha < 0.999f) MakeTransparent(m);
            return m;
        }

        public static Material Body(Color color, float metallic, float smoothness, Color emission, float emissionStrength)
        {
            var shader = FindShader("Universal Render Pipeline/Lit", "Standard");
            var m = new Material(shader) { name = "WintryBody" };
            Set(m, "_BaseColor", color); Set(m, "_Color", color);
            SetF(m, "_Metallic", metallic); SetF(m, "_Smoothness", smoothness); SetF(m, "_Glossiness", smoothness);
            m.EnableKeyword("_EMISSION");
            Set(m, "_EmissionColor", emission * emissionStrength);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        public static Material Glass(Color tint, float alpha = 0.35f, float fresnel = 1.5f)
        {
            var shader = FindShader("WintryVR/Glass", "Universal Render Pipeline/Unlit", "Unlit/Transparent");
            var m = new Material(shader) { name = "WintryGlass" };
            var c = tint; c.a = alpha;
            Set(m, "_Color", c); Set(m, "_BaseColor", c);
            SetF(m, "_Fresnel", fresnel); Set(m, "_EdgeColor", new Color(1f, 1f, 1f, 0.35f));
            MakeTransparent(m);
            m.renderQueue = 3000;
            return m;
        }

        public static Material Unlit(Color color, bool transparent = false)
        {
            string key = "unlit_" + ColorUtility.ToHtmlStringRGBA(color) + (transparent ? "_t" : "");
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var shader = FindShader("Universal Render Pipeline/Unlit", transparent ? "Unlit/Transparent" : "Unlit/Color");
            var m = new Material(shader) { name = "WintryUnlit" };
            Set(m, "_BaseColor", color); Set(m, "_Color", color);
            if (transparent) MakeTransparent(m);
            _cache[key] = m;
            return m;
        }

        public static void MakeTransparent(Material m)
        {
            // URP Unlit/Lit surface type switches
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        public static void SetColor(Material m, Color c) { Set(m, "_BaseColor", c); Set(m, "_Color", c); }
        public static void SetEmission(Material m, Color c) { Set(m, "_EmissionColor", c); }
        public static void SetAlpha(Material m, float a)
        {
            foreach (var p in new[] { "_BaseColor", "_Color" })
                if (m.HasProperty(p)) { var c = m.GetColor(p); c.a = a; m.SetColor(p, c); }
            SetF(m, "_Alpha", a);
        }

        private static void Set(Material m, string prop, Color c) { if (m.HasProperty(prop)) m.SetColor(prop, c); }
        private static void SetF(Material m, string prop, float f) { if (m.HasProperty(prop)) m.SetFloat(prop, f); }
    }
}
