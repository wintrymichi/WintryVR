using UnityEngine;

namespace WintryVR.Voice
{
    /// <summary>
    /// Energy-based voice activity detection with adaptive noise floor. Segments microphone audio into
    /// utterances: speech starts when RMS rises well above the floor and ends after a silence tail.
    /// </summary>
    public class VoiceActivityDetector
    {
        public float NoiseFloor { get; private set; } = 0.005f;
        public float SpeechThresholdMultiplier = 3.5f;
        public float MinSpeechThreshold = 0.012f;
        public float SilenceTailSeconds = 0.9f;
        public float MinUtteranceSeconds = 0.35f;
        public float MaxUtteranceSeconds = 12f;
        public float CurrentLevel { get; private set; }
        public bool InSpeech { get; private set; }

        private float _silenceTimer;
        private float _speechTimer;

        public enum Event { None, SpeechStart, SpeechEnd, Timeout }

        public Event Process(float[] chunk, float chunkSeconds)
        {
            float sum = 0f;
            for (int i = 0; i < chunk.Length; i++) sum += chunk[i] * chunk[i];
            float rms = Mathf.Sqrt(sum / Mathf.Max(1, chunk.Length));
            CurrentLevel = rms;

            float threshold = Mathf.Max(MinSpeechThreshold, NoiseFloor * SpeechThresholdMultiplier);
            if (!InSpeech)
            {
                // adapt the floor only when not speaking
                NoiseFloor = Mathf.Lerp(NoiseFloor, rms, rms < NoiseFloor ? 0.3f : 0.02f);
                if (rms > threshold)
                {
                    InSpeech = true; _speechTimer = 0f; _silenceTimer = 0f;
                    return Event.SpeechStart;
                }
                return Event.None;
            }

            _speechTimer += chunkSeconds;
            if (rms > threshold * 0.6f) _silenceTimer = 0f; else _silenceTimer += chunkSeconds;
            if (_speechTimer > MaxUtteranceSeconds) { InSpeech = false; return Event.Timeout; }
            if (_silenceTimer > SilenceTailSeconds)
            {
                InSpeech = false;
                return _speechTimer >= MinUtteranceSeconds + SilenceTailSeconds ? Event.SpeechEnd : Event.None;
            }
            return Event.None;
        }

        public void Reset() { InSpeech = false; _silenceTimer = 0; _speechTimer = 0; }
    }
}
