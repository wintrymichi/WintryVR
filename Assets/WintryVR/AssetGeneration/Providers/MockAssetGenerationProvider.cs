using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.AssetGeneration.Providers
{
    /// <summary>
    /// Offline concept generator: derives look parameters from the prompt's words (colours, moods, materials)
    /// and renders a small procedural "concept card" texture so the user can preview and confirm. Does not
    /// pretend to generate 3D geometry — the character mesh stays procedural and identity-locked.
    /// </summary>
    public class MockAssetGenerationProvider : IAssetGenerationProvider
    {
        public string Name => "MockAssetGen";
        public bool IsConfigured => true;
        public bool RequiresNetwork => false;
        public bool SupportsConceptImages => true;
        public bool Supports3DModels => false;

        public async Task<ConceptResult> GenerateConceptAsync(string prompt, WintryLookDefinition coreIdentity, CancellationToken ct)
        {
            await Task.Delay(300, ct);
            var look = LookFromPrompt(prompt, coreIdentity);
            var tex = await MainThreadDispatcher.RunAsync(() => RenderConceptCard(look, 256));
            return new ConceptResult { Success = true, Prompt = prompt, Look = look, ConceptImage = tex, Description = "Concept '" + look.Name + "': " + Describe(look) };
        }

        public Task<Mesh> GenerateMeshAsync(string prompt, CancellationToken ct) => Task.FromResult<Mesh>(null);

        public static WintryLookDefinition LookFromPrompt(string prompt, WintryLookDefinition baseLook)
        {
            var l = new WintryLookDefinition
            {
                Name = "Custom " + System.DateTime.UtcNow.ToString("HHmm"),
                PrimaryColor = baseLook.PrimaryColor, SecondaryColor = baseLook.SecondaryColor, EmissionColor = baseLook.EmissionColor, EyeColor = baseLook.EyeColor,
                EmissionStrength = baseLook.EmissionStrength, Metallic = baseLook.Metallic, Smoothness = baseLook.Smoothness, Softness = baseLook.Softness, TexturePreset = baseLook.TexturePreset,
                PatternScale = baseLook.PatternScale, PatternStrength = baseLook.PatternStrength, ShowRing = baseLook.ShowRing, ShowParticles = baseLook.ShowParticles
            };
            string p = (prompt ?? "").ToLowerInvariant();
            void C(string[] words, Color primary, Color emission) { foreach (var w in words) if (p.Contains(w)) { l.PrimaryColor = primary; l.EmissionColor = emission; l.EyeColor = Color.Lerp(emission, Color.white, 0.4f); return; } }
            C(new[] { "red", "rosso", "rot", "rouge", "rojo" }, new Color(0.55f, 0.12f, 0.15f), new Color(1f, 0.35f, 0.3f));
            C(new[] { "green", "verde", "grün", "vert" }, new Color(0.12f, 0.4f, 0.28f), new Color(0.3f, 1f, 0.6f));
            C(new[] { "gold", "oro", "golden", "dorato", "doré", "dorado" }, new Color(0.85f, 0.65f, 0.25f), new Color(1f, 0.85f, 0.4f));
            C(new[] { "purple", "viola", "violet", "lila", "morado" }, new Color(0.3f, 0.15f, 0.45f), new Color(0.8f, 0.45f, 1f));
            C(new[] { "white", "bianco", "weiß", "blanc", "blanco" }, new Color(0.95f, 0.96f, 0.98f), new Color(0.75f, 0.9f, 1f));
            C(new[] { "black", "nero", "schwarz", "noir", "negro" }, new Color(0.06f, 0.06f, 0.08f), new Color(0.5f, 0.75f, 1f));
            C(new[] { "orange", "arancio", "naranja" }, new Color(0.7f, 0.35f, 0.1f), new Color(1f, 0.6f, 0.2f));
            C(new[] { "pink", "rosa", "rose" }, new Color(0.8f, 0.45f, 0.6f), new Color(1f, 0.6f, 0.85f));
            if (p.Contains("metal") || p.Contains("chrome") || p.Contains("steel") || p.Contains("acciaio") || p.Contains("metall")) { l.Metallic = 0.85f; l.Smoothness = 0.92f; }
            if (p.Contains("matte") || p.Contains("opac") || p.Contains("matt")) { l.Metallic = 0f; l.Smoothness = 0.45f; }
            if (p.Contains("neon") || p.Contains("glow") || p.Contains("luminos") || p.Contains("leucht")) l.EmissionStrength = 2.4f;
            if (p.Contains("soft") || p.Contains("friendly") || p.Contains("morbid") || p.Contains("amichev") || p.Contains("doux") || p.Contains("suave")) l.Softness = 0.9f;
            if (p.Contains("sharp") || p.Contains("angular") || p.Contains("tech") || p.Contains("spigol")) l.Softness = 0.15f;
            if (p.Contains("frost") || p.Contains("ice") || p.Contains("ghiacc") || p.Contains("eis") || p.Contains("glace") || p.Contains("hielo") || p.Contains("winter") || p.Contains("invern")) { l.TexturePreset = "frost"; l.PatternStrength = 0.4f; }
            else if (p.Contains("circuit") || p.Contains("cyber") || p.Contains("digital")) { l.TexturePreset = "circuit"; l.PatternStrength = 0.5f; }
            else if (p.Contains("marble") || p.Contains("stone") || p.Contains("marmo") || p.Contains("cloud") || p.Contains("nuvol")) { l.TexturePreset = "noise"; l.PatternStrength = 0.35f; }
            if (p.Contains("no ring") || p.Contains("senza anello") || p.Contains("ohne ring")) l.ShowRing = false;
            if (p.Contains("minimal")) { l.PatternStrength = 0f; l.ShowParticles = false; }
            l.SecondaryColor = Color.Lerp(l.PrimaryColor, Color.black, 0.55f);
            return l;
        }

        public static Texture2D RenderConceptCard(WintryLookDefinition look, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "ConceptCard" };
            var px = new Color32[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.42f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fy = y / (float)size;
                    Color bg = Color.Lerp(new Color(0.05f, 0.07f, 0.12f), new Color(0.1f, 0.13f, 0.2f), fy);
                    Vector2 p = new Vector2(x, y);
                    float dHead = Vector2.Distance(p, c + new Vector2(0, size * 0.22f)) / (size * 0.11f);
                    float dBody = Vector2.Distance(new Vector2(p.x, p.y * 1.4f), new Vector2(c.x, c.y * 1.4f)) / (size * 0.16f);
                    float dRing = Mathf.Abs(Vector2.Distance(p, c + new Vector2(0, size * 0.36f)) - size * 0.1f) / (size * 0.012f);
                    float dEyeL = Vector2.Distance(p, c + new Vector2(-size * 0.035f, size * 0.22f)) / (size * 0.02f);
                    float dEyeR = Vector2.Distance(p, c + new Vector2(size * 0.035f, size * 0.22f)) / (size * 0.02f);
                    Color col = bg;
                    if (dBody < 1f) col = Color.Lerp(look.PrimaryColor, look.SecondaryColor, dBody * dBody * 0.8f);
                    if (dHead < 1f) col = Color.Lerp(look.PrimaryColor, look.SecondaryColor, dHead * 0.5f);
                    if (dRing < 1f) col = look.EmissionColor * look.EmissionStrength;
                    if (dEyeL < 1f || dEyeR < 1f) col = look.EyeColor * 1.4f;
                    px[y * size + x] = col;
                }
            tex.SetPixels32(px); tex.Apply(false);
            return tex;
        }

        private static string Describe(WintryLookDefinition l)
        {
            return (l.Metallic > 0.5f ? "metallic " : "") + (l.Softness > 0.6f ? "soft " : l.Softness < 0.3f ? "angular " : "") + l.TexturePreset + " finish, emission " + l.EmissionStrength.ToString("0.0") + (l.ShowRing ? ", halo" : ", no halo");
        }
    }
}
