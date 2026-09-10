using UnityEngine;
using UnityEngine.Rendering;

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

        /// <summary>
        /// True when a scriptable render pipeline (URP here) is actually driving rendering.
        /// </summary>
        public static bool UniversalPipelineActive =>
            GraphicsSettings.currentRenderPipeline != null || GraphicsSettings.defaultRenderPipeline != null;

        /// <summary>
        /// Picks the first shader in <paramref name="names"/> that exists <em>and</em> suits the active pipeline.
        /// </summary>
        /// <remarks>
        /// The URP package being installed is not the same as URP being switched on. With the package present
        /// but no pipeline asset assigned in Project Settings → Graphics, Unity renders through the built-in
        /// pipeline while <c>Shader.Find</c> still happily returns URP-only shaders — so the intended fallback
        /// never fired and the UI drew URP glass under a pipeline that cannot feed it. Skipping the
        /// pipeline-specific names when URP is off makes the fallback real, and the app looks plainer instead
        /// of wrong on a project that has not had a URP asset assigned yet.
        /// </remarks>
        public static Shader FindShader(params string[] names)
        {
            bool urp = UniversalPipelineActive;
            foreach (var n in names)
            {
                if (!urp && IsUniversalOnly(n)) continue;
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            // never hand back null: a null shader is the magenta this whole method exists to avoid
            return Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        }

        private static bool IsUniversalOnly(string shaderName)
        {
            return shaderName != null
                && (shaderName.StartsWith("WintryVR/") || shaderName.StartsWith("Universal Render Pipeline/"));
        }

        public static Material Glow(Color color, Color emission, float strength = 1.5f, float alpha = 1f, float fresnel = 2f, float coreGlow = 0.35f)
        {
            var shader = FindShader("WintryVR/Glow", "Universal Render Pipeline/Unlit", "Unlit/Color");
            var m = new Material(shader) { name = "WintryGlow" };
            Set(m, "_Color", color); Set(m, "_BaseColor", color);
            // intensity is folded into the colour, which is the convention every SetEmission caller follows;
            // WintryVR/Glow therefore uses _EmissionColor as-is rather than scaling it again
            Set(m, "_EmissionColor", emission * strength);
            SetF(m, "_Fresnel", fresnel); SetF(m, "_Alpha", alpha); SetF(m, "_CoreGlow", coreGlow);
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

        /// <summary>
        /// Tells a glass material the real dimensions of the plate it is drawn on, so its edge band can be
        /// measured in metres and keep one thickness on both axes and around the corners. Harmless when the
        /// shader has fallen back to a plain unlit one, which simply has no such properties.
        /// </summary>
        public static void SetPanelShape(Material m, float width, float height, float radius, float edgeWidth = 0.006f)
        {
            if (m == null) return;
            if (m.HasProperty("_Size")) m.SetVector("_Size", new Vector4(Mathf.Max(1e-4f, width), Mathf.Max(1e-4f, height), 0f, 0f));
            SetF(m, "_Radius", radius);
            SetF(m, "_EdgeWidth", edgeWidth);
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
