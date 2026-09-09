using System.Collections.Generic;
using UnityEngine;

namespace WintryVR.Audio
{
    /// <summary>Small synthesised UI sounds (no audio assets needed): soft, short, non-gamey.</summary>
    public static class ProceduralSfx
    {
        private static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private const int Rate = 22050;

        public static AudioClip Get(string name)
        {
            if (_cache.TryGetValue(name, out var c) && c != null) return c;
            AudioClip clip;
            switch (name)
            {
                case "listen": clip = Tone(new[] { 520f, 780f }, 0.09f, 0.18f, 0.35f); break;
                case "think": clip = Tone(new[] { 440f, 440f, 440f }, 0.05f, 0.35f, 0.15f); break;
                case "vision": clip = Sweep(300f, 900f, 0.35f, 0.25f); break;
                case "search": clip = Tone(new[] { 660f, 880f, 660f, 880f }, 0.05f, 0.4f, 0.2f); break;
                case "speak": clip = Tone(new[] { 700f }, 0.06f, 0.08f, 0.2f); break;
                case "error": clip = Tone(new[] { 330f, 262f }, 0.12f, 0.3f, 0.3f); break;
                case "offline": clip = Tone(new[] { 392f, 330f, 262f }, 0.1f, 0.4f, 0.25f); break;
                case "confirm": clip = Tone(new[] { 587f, 880f }, 0.07f, 0.18f, 0.3f); break;
                case "pointer": clip = Tone(new[] { 988f }, 0.05f, 0.09f, 0.25f); break;
                case "transform": clip = Sweep(200f, 1200f, 0.6f, 0.3f); break;
                case "collapse": clip = Sweep(1000f, 250f, 0.45f, 0.25f); break;
                default: clip = Tone(new[] { 600f }, 0.05f, 0.08f, 0.2f); break;
            }
            _cache[name] = clip;
            return clip;
        }

        private static AudioClip Tone(float[] notes, float noteLen, float total, float gain)
        {
            int n = (int)(total * Rate);
            var s = new float[n];
            float step = total / notes.Length;
            for (int k = 0; k < notes.Length; k++)
            {
                int i0 = (int)(k * step * Rate), i1 = Mathf.Min(n, (int)((k * step + noteLen) * Rate));
                for (int i = i0; i < i1; i++)
                {
                    float t = (i - i0) / (float)Rate;
                    float env = Mathf.Sin(Mathf.PI * (i - i0) / (float)(i1 - i0));
                    s[i] += (Mathf.Sin(2 * Mathf.PI * notes[k] * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * notes[k] * t)) * env * gain;
                }
            }
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(s, 0);
            return clip;
        }

        private static AudioClip Sweep(float f0, float f1, float total, float gain)
        {
            int n = (int)(total * Rate);
            var s = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t * t);
                phase += 2 * Mathf.PI * f / Rate;
                float env = Mathf.Sin(Mathf.PI * t);
                s[i] = Mathf.Sin(phase) * env * gain;
            }
            var clip = AudioClip.Create("sweep", n, 1, Rate, false);
            clip.SetData(s, 0);
            return clip;
        }
    }
}
