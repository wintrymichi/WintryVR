using NUnit.Framework;
using UnityEngine;
using WintryVR.Voice;

namespace WintryVR.Tests
{
    public class VoiceTests
    {
        [TestCase("Hey Wintry, what is this?", true, "what is this?")]
        [TestCase("hey wintery what's that", true, "what's that")]
        [TestCase("Ehi Wintry, cosa sto guardando?", true, "cosa sto guardando?")]
        [TestCase("Wintry read this", true, "read this")]
        [TestCase("What is this?", false, "What is this?")]
        public void WakeWordStripping(string input, bool expected, string remainder)
        {
            bool ok = WakeWordDetector.TryStrip(input, out string rest);
            Assert.AreEqual(expected, ok);
            Assert.AreEqual(remainder, rest);
        }

        [Test]
        public void VadSegmentsSpeech()
        {
            var vad = new VoiceActivityDetector { SilenceTailSeconds = 0.3f, MinUtteranceSeconds = 0.1f };
            float[] silence = new float[160]; // 10 ms @ 16k
            float[] speech = new float[160]; for (int i = 0; i < speech.Length; i++) speech[i] = Mathf.Sin(i * 0.3f) * 0.3f;
            for (int i = 0; i < 30; i++) Assert.AreEqual(VoiceActivityDetector.Event.None, vad.Process(silence, 0.01f));
            Assert.AreEqual(VoiceActivityDetector.Event.SpeechStart, vad.Process(speech, 0.01f));
            for (int i = 0; i < 40; i++) vad.Process(speech, 0.01f);
            VoiceActivityDetector.Event last = VoiceActivityDetector.Event.None;
            for (int i = 0; i < 60 && last == VoiceActivityDetector.Event.None; i++) last = vad.Process(silence, 0.01f);
            Assert.AreEqual(VoiceActivityDetector.Event.SpeechEnd, last);
        }

        [Test]
        public void WavRoundTrip()
        {
            float[] samples = new float[1600];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 0.1f) * 0.5f;
            byte[] wav = WavUtility.FromSamples(samples, 16000);
            var clip = WavUtility.ToAudioClip(wav, "t");
            Assert.IsNotNull(clip);
            Assert.AreEqual(16000, clip.frequency);
            Assert.AreEqual(1600, clip.samples);
            var back = new float[1600]; clip.GetData(back, 0);
            Assert.AreEqual(samples[100], back[100], 0.001f);
        }

        [Test]
        public void ResampleChangesLength()
        {
            var input = new float[48000];
            var output = WavUtility.Resample(input, 48000, 16000);
            Assert.AreEqual(16000, output.Length);
        }
    }
}
