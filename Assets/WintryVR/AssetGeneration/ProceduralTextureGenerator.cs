using System;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.AssetGeneration
{
    /// <summary>
    /// Procedural Texture System: generates a full PBR set (Base Color, Normal, Roughness, Metallic, Emission,
    /// Detail) from a look definition and a preset (smooth | frost | circuit | noise). Everything is parametric,
    /// deterministic (seeded by the look name) and cheap enough to run on device at 256–512 px.
    /// </summary>
    public static class ProceduralTextureGenerator
    {
        public static GeneratedTextureSet Generate(WintryLookDefinition look, int resolution)
        {
            resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(resolution), 64, 1024);
            int seed = (look.Name ?? "Default").GetHashCode();
            var height = new float[resolution * resolution];
            var pattern = new float[resolution * resolution];
            var emissionMask = new float[resolution * resolution];
            FillFields(look, resolution, seed, height, pattern, emissionMask);

            var set = new GeneratedTextureSet { Resolution = resolution };
            set.BaseColor = BuildBaseColor(look, resolution, pattern, emissionMask);
            set.Normal = BuildNormal(resolution, height, 1.5f + look.PatternStrength * 3f);
            set.Roughness = BuildGray(resolution, i => Mathf.Clamp01(1f - look.Smoothness + pattern[i] * 0.15f * look.PatternStrength), "Roughness");
            set.Metallic = BuildMetallicGloss(look, resolution, pattern);
            set.Emission = BuildEmission(look, resolution, emissionMask);
            set.Detail = BuildGray(resolution, i => 0.5f + (pattern[i] - 0.5f) * 0.5f, "Detail");
            return set;
        }

        // ------------------------------------------------------------------ field synthesis
        private static void FillFields(WintryLookDefinition look, int n, int seed, float[] height, float[] pattern, float[] emission)
        {
            float scale = look.PatternScale;
            float ox = (seed & 0xFFFF) * 0.001f, oy = ((seed >> 8) & 0xFFFF) * 0.001f;
            string preset = (look.TexturePreset ?? "smooth").ToLowerInvariant();
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    int i = y * n + x;
                    float h, p, e;
                    switch (preset)
                    {
                        case "frost":
                            {
                                // crystalline branching: layered ridged noise
                                float r1 = Ridged(u * scale + ox, v * scale + oy);
                                float r2 = Ridged(u * scale * 2.3f + ox * 2f, v * scale * 2.3f + oy * 2f);
                                p = Mathf.Clamp01(r1 * 0.7f + r2 * 0.3f);
                                h = p;
                                e = Mathf.Clamp01((p - 0.7f) / 0.3f);
                                break;
                            }
                        case "circuit":
                            {
                                // grid of traces with pads: quantised noise lines
                                float gx = Mathf.Abs(Mathf.Repeat(u * scale * 2f, 1f) - 0.5f);
                                float gy = Mathf.Abs(Mathf.Repeat(v * scale * 2f, 1f) - 0.5f);
                                float cell = Mathf.PerlinNoise(Mathf.Floor(u * scale * 2f) * 0.37f + ox, Mathf.Floor(v * scale * 2f) * 0.37f + oy);
                                bool horizontal = cell > 0.5f;
                                float line = horizontal ? gy : gx;
                                float trace = 1f - Mathf.Clamp01((line - 0.04f) / 0.03f);
                                float pad = 1f - Mathf.Clamp01((Mathf.Sqrt(gx * gx + gy * gy) - 0.08f) / 0.03f);
                                p = Mathf.Clamp01(Mathf.Max(trace * (cell > 0.25f ? 1f : 0f), pad * (cell > 0.6f ? 1f : 0f)));
                                h = p * 0.6f;
                                e = p;
                                break;
                            }
                        case "noise":
                            {
                                float f = Fbm(u * scale + ox, v * scale + oy, 4);
                                p = f; h = f;
                                e = Mathf.Clamp01((f - 0.62f) / 0.25f);
                                break;
                            }
                        default: // smooth: very soft large-scale variation with a faint seam line
                            {
                                float f = Fbm(u * scale * 0.5f + ox, v * scale * 0.5f + oy, 3);
                                float seam = 1f - Mathf.Clamp01(Mathf.Abs(v - 0.5f) / 0.006f);
                                p = f * 0.5f + 0.25f; h = f * 0.3f + seam * 0.4f;
                                e = seam;
                                break;
                            }
                    }
                    height[i] = h; pattern[i] = p; emission[i] = e;
                }
            }
        }

        private static float Fbm(float x, float y, int octaves)
        {
            float sum = 0, amp = 0.5f, freq = 1f, norm = 0;
            for (int o = 0; o < octaves; o++) { sum += Mathf.PerlinNoise(x * freq, y * freq) * amp; norm += amp; amp *= 0.5f; freq *= 2.1f; }
            return sum / norm;
        }

        private static float Ridged(float x, float y)
        {
            float n = Mathf.PerlinNoise(x, y);
            return 1f - Mathf.Abs(n * 2f - 1f);
        }

        // ------------------------------------------------------------------ texture builders
        private static Texture2D NewTex(int n, string name, bool linear)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true, linear) { name = "Wintry_" + name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            return t;
        }

        private static Texture2D BuildBaseColor(WintryLookDefinition look, int n, float[] pattern, float[] emissionMask)
        {
            var t = NewTex(n, "BaseColor", false);
            var px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++)
            {
                float p = pattern[i];
                Color c = Color.Lerp(look.PrimaryColor, look.SecondaryColor, p * look.PatternStrength);
                c = Color.Lerp(c, look.EmissionColor, emissionMask[i] * look.PatternStrength * 0.5f);
                px[i] = c;
            }
            t.SetPixels32(px); t.Apply(true, false);
            return t;
        }

        private static Texture2D BuildNormal(int n, float[] height, float strength)
        {
            var t = NewTex(n, "Normal", true);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float hl = height[y * n + (x - 1 + n) % n], hr = height[y * n + (x + 1) % n];
                    float hd = height[((y - 1 + n) % n) * n + x], hu = height[((y + 1) % n) * n + x];
                    Vector3 nrm = new Vector3(-(hr - hl) * strength, -(hu - hd) * strength, 1f).normalized;
                    px[y * n + x] = new Color(nrm.x * 0.5f + 0.5f, nrm.y * 0.5f + 0.5f, nrm.z * 0.5f + 0.5f, 1f);
                }
            t.SetPixels32(px); t.Apply(true, false);
            return t;
        }

        private static Texture2D BuildGray(int n, Func<int, float> f, string name)
        {
            var t = NewTex(n, name, true);
            var px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++) { byte b = (byte)(Mathf.Clamp01(f(i)) * 255f); px[i] = new Color32(b, b, b, 255); }
            t.SetPixels32(px); t.Apply(true, false);
            return t;
        }

        /// <summary>URP packs metallic in R and smoothness in A of the metallic-gloss map.</summary>
        private static Texture2D BuildMetallicGloss(WintryLookDefinition look, int n, float[] pattern)
        {
            var t = NewTex(n, "MetallicSmoothness", true);
            var px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++)
            {
                float metal = Mathf.Clamp01(look.Metallic + (pattern[i] - 0.5f) * 0.3f * look.PatternStrength);
                float smooth = Mathf.Clamp01(look.Smoothness - pattern[i] * 0.15f * look.PatternStrength);
                px[i] = new Color(metal, metal, metal, smooth);
            }
            t.SetPixels32(px); t.Apply(true, false);
            return t;
        }

        private static Texture2D BuildEmission(WintryLookDefinition look, int n, float[] mask)
        {
            var t = NewTex(n, "Emission", false);
            var px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = look.EmissionColor * mask[i];
            t.SetPixels32(px); t.Apply(true, false);
            return t;
        }

        /// <summary>PNG bytes for caching/export.</summary>
        public static byte[] ToPng(Texture2D t) => t != null ? t.EncodeToPNG() : null;
    }
}
