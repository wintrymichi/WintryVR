using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Character
{
    /// <summary>
    /// Wintry's Core Identity and the built-in look variants. Every variant (and every AI-generated look) is
    /// passed through <see cref="CoreIdentity.Constrain"/> so Wintry always stays recognisably Wintry:
    /// the same silhouette, the same two-lens eyes, the same halo ring and a cool-toned emissive accent.
    /// </summary>
    public static class CoreIdentity
    {
        public static readonly Color SignatureAccent = new Color(0.45f, 0.82f, 1f);
        public const float MinEmission = 0.6f;
        public const float MaxEmission = 2.6f;
        public const float MaxSaturationShift = 0.35f;

        public static WintryLookDefinition Constrain(WintryLookDefinition look)
        {
            if (look == null) look = new WintryLookDefinition();
            look.EmissionStrength = Mathf.Clamp(look.EmissionStrength, MinEmission, MaxEmission);
            look.Metallic = Mathf.Clamp01(look.Metallic);
            look.Smoothness = Mathf.Clamp(look.Smoothness, 0.3f, 0.97f);
            look.PatternStrength = Mathf.Clamp(look.PatternStrength, 0f, 0.6f);
            look.PatternScale = Mathf.Clamp(look.PatternScale, 1f, 12f);
            look.Softness = Mathf.Clamp01(look.Softness);
            // Eyes always keep a luminous, cool-leaning tint so the gaze reads as Wintry's
            Color.RGBToHSV(look.EyeColor, out float h, out float s, out float v);
            v = Mathf.Max(v, 0.8f); s = Mathf.Min(s, 0.6f);
            look.EyeColor = Color.HSVToRGB(h, s, v);
            // Emission never fully abandons the signature accent
            look.EmissionColor = Color.Lerp(look.EmissionColor, SignatureAccent, 0.2f);
            if (string.IsNullOrEmpty(look.TexturePreset)) look.TexturePreset = "smooth";
            return look;
        }
    }

    public static class CharacterVariants
    {
        public static WintryLookDefinition GetLook(WintryVariant v)
        {
            WintryLookDefinition l;
            switch (v)
            {
                case WintryVariant.Minimal:
                    l = new WintryLookDefinition { Name = "Minimal", PrimaryColor = new Color(0.92f, 0.94f, 0.97f), SecondaryColor = new Color(0.75f, 0.78f, 0.84f), EmissionColor = new Color(0.6f, 0.85f, 1f), EyeColor = new Color(0.75f, 0.9f, 1f), EmissionStrength = 0.8f, Metallic = 0.05f, Smoothness = 0.7f, PatternStrength = 0f, ShowParticles = false, Softness = 0.6f, TexturePreset = "smooth" };
                    break;
                case WintryVariant.Cyber:
                    l = new WintryLookDefinition { Name = "Cyber", PrimaryColor = new Color(0.1f, 0.12f, 0.18f), SecondaryColor = new Color(0.05f, 0.06f, 0.1f), EmissionColor = new Color(0.2f, 0.95f, 0.9f), EyeColor = new Color(0.4f, 1f, 0.95f), EmissionStrength = 2.2f, Metallic = 0.7f, Smoothness = 0.9f, PatternScale = 8f, PatternStrength = 0.5f, Softness = 0.15f, TexturePreset = "circuit" };
                    break;
                case WintryVariant.Winter:
                    l = new WintryLookDefinition { Name = "Winter", PrimaryColor = new Color(0.85f, 0.93f, 1f), SecondaryColor = new Color(0.55f, 0.7f, 0.9f), EmissionColor = new Color(0.7f, 0.9f, 1f), EyeColor = new Color(0.8f, 0.95f, 1f), EmissionStrength = 1.3f, Metallic = 0.1f, Smoothness = 0.95f, PatternScale = 5f, PatternStrength = 0.35f, Softness = 0.7f, TexturePreset = "frost" };
                    break;
                case WintryVariant.Neon:
                    l = new WintryLookDefinition { Name = "Neon", PrimaryColor = new Color(0.15f, 0.08f, 0.25f), SecondaryColor = new Color(0.08f, 0.04f, 0.14f), EmissionColor = new Color(0.9f, 0.3f, 1f), EyeColor = new Color(1f, 0.6f, 1f), EmissionStrength = 2.5f, Metallic = 0.3f, Smoothness = 0.85f, PatternScale = 6f, PatternStrength = 0.45f, Softness = 0.3f, TexturePreset = "noise" };
                    break;
                case WintryVariant.Dark:
                    l = new WintryLookDefinition { Name = "Dark", PrimaryColor = new Color(0.08f, 0.09f, 0.11f), SecondaryColor = new Color(0.03f, 0.03f, 0.04f), EmissionColor = new Color(0.35f, 0.6f, 0.9f), EyeColor = new Color(0.6f, 0.85f, 1f), EmissionStrength = 1.1f, Metallic = 0.5f, Smoothness = 0.6f, PatternStrength = 0.15f, Softness = 0.35f, ShowParticles = false, TexturePreset = "noise" };
                    break;
                case WintryVariant.Friendly:
                    l = new WintryLookDefinition { Name = "Friendly", PrimaryColor = new Color(0.98f, 0.92f, 0.85f), SecondaryColor = new Color(0.95f, 0.75f, 0.65f), EmissionColor = new Color(1f, 0.8f, 0.6f), EyeColor = new Color(0.9f, 0.95f, 1f), EmissionStrength = 1f, Metallic = 0f, Smoothness = 0.75f, PatternStrength = 0.1f, Softness = 0.95f, TexturePreset = "smooth" };
                    break;
                case WintryVariant.Professional:
                    l = new WintryLookDefinition { Name = "Professional", PrimaryColor = new Color(0.2f, 0.24f, 0.3f), SecondaryColor = new Color(0.12f, 0.14f, 0.18f), EmissionColor = new Color(0.55f, 0.75f, 0.95f), EyeColor = new Color(0.7f, 0.85f, 1f), EmissionStrength = 0.9f, Metallic = 0.35f, Smoothness = 0.8f, PatternStrength = 0.08f, Softness = 0.4f, ShowParticles = false, TexturePreset = "smooth" };
                    break;
                default:
                    l = new WintryLookDefinition { Name = "Default" };
                    break;
            }
            return CoreIdentity.Constrain(l);
        }
    }
}
