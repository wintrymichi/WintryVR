using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Voice.Providers
{
    /// <summary>
    /// Offline "voice": synthesises a soft, syllable-paced tone pattern whose length matches the text so the
    /// lip sync, spatial audio and subtitles behave exactly as with a real voice. Subtitles carry the words.
    /// </summary>
    public class MockTtsProvider : ITextToSpeechProvider
    {
        public string Name => "MockTTS";
        public bool RequiresNetwork => false;
        public bool IsConfigured => true;

        public Task<TtsResult> SynthesizeAsync(string text, string languageCode, string voice, float speed, CancellationToken ct)
        {
            int syllables = Mathf.Max(2, CountSyllables(text));
            float secPerSyl = 0.16f / Mathf.Max(0.5f, speed);
            float duration = syllables * secPerSyl + 0.25f;
            int rate = 22050;
            int n = Mathf.CeilToInt(duration * rate);
            var samples = new float[n];
            var rnd = new System.Random(text.GetHashCode());
            float basePitch = 220f + (voice != null && voice.Contains("low") ? -40f : 0f);
            for (int s = 0; s < syllables; s++)
            {
                float start = s * secPerSyl; float len = secPerSyl * 0.75f;
                float f0 = basePitch * (0.9f + 0.25f * (float)rnd.NextDouble());
                int i0 = (int)(start * rate), i1 = Mathf.Min(n, (int)((start + len) * rate));
                for (int i = i0; i < i1; i++)
                {
                    float t = (i - i0) / (float)rate;
                    float env = Mathf.Sin(Mathf.PI * (i - i0) / (float)(i1 - i0));
                    float v = Mathf.Sin(2 * Mathf.PI * f0 * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * f0 * 2f * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * f0 * 3f * t) * 0.1f;
                    samples[i] += v * env * 0.28f;
                }
            }
            var clip = AudioClip.Create("mock-tts", n, 1, rate, false);
            clip.SetData(samples, 0);
            return Task.FromResult(new TtsResult { Success = true, Clip = clip, DurationSeconds = duration });
        }

        public void Stop() { }

        public static int CountSyllables(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int count = 0; bool prevVowel = false;
            foreach (char ch in text.ToLowerInvariant())
            {
                bool vowel = "aeiouyàèéìòùäöüáíóúâêîôû".IndexOf(ch) >= 0;
                if (vowel && !prevVowel) count++;
                prevVowel = vowel;
            }
            return count;
        }
    }
}
