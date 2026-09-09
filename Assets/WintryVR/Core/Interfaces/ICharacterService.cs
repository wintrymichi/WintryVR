using System.Threading.Tasks;
using UnityEngine;

namespace WintryVR.Core
{
    public enum FacialExpression { Neutral, Happy, Curious, Thinking, Surprised, Concerned, Excited }

    public enum WintryVariant { Default, Minimal, Cyber, Winter, Neon, Dark, Friendly, Professional }

    public interface ICharacterService
    {
        PresenceForm Form { get; }
        Transform Root { get; }                 // where audio & UI attach
        Vector3 Position { get; }
        WintryVariant Variant { get; }
        bool IsVisible { get; }

        void SetState(AssistantState state);
        void SetExpression(FacialExpression expression);
        Task TransformToCharacterAsync();
        Task CollapseToCoreAsync();
        void Hide();
        void ShowCore();
        void LookAt(Vector3 worldPoint);
        void LookAtUser();
        void PointAt(Vector3 worldPoint);
        void ReturnHome();
        void ApplyVariant(WintryVariant variant);
        void ApplyCustomLook(WintryLookDefinition look);
    }

    /// <summary>A look Wintry can wear. Variants and AI-generated looks both produce one of these.</summary>
    [System.Serializable]
    public class WintryLookDefinition
    {
        public string Name = "Default";
        public Color PrimaryColor = new Color(0.62f, 0.85f, 1f);
        public Color SecondaryColor = new Color(0.12f, 0.16f, 0.28f);
        public Color EmissionColor = new Color(0.45f, 0.8f, 1f);
        public Color EyeColor = new Color(0.7f, 0.95f, 1f);
        public float EmissionStrength = 1.4f;
        public float Metallic = 0.25f;
        public float Smoothness = 0.85f;
        public float PatternScale = 4f;
        public float PatternStrength = 0.25f;
        public bool ShowRing = true;
        public bool ShowParticles = true;
        public float Softness = 0.5f;   // 0 = sharp/tech, 1 = rounded/friendly
        public string TexturePreset = "smooth"; // smooth | frost | circuit | noise
    }
}
