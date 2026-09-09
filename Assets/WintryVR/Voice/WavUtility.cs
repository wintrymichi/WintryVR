using System;
using System.IO;
using UnityEngine;

namespace WintryVR.Voice
{
    /// <summary>PCM ↔ WAV helpers for STT uploads and TTS downloads.</summary>
    public static class WavUtility
    {
        public static byte[] FromSamples(float[] samples, int sampleRate, int channels = 1)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                int dataLen = samples.Length * 2;
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + dataLen);
                bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16); bw.Write((short)1); bw.Write((short)channels);
                bw.Write(sampleRate); bw.Write(sampleRate * channels * 2); bw.Write((short)(channels * 2)); bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(dataLen);
                foreach (var s in samples) bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), short.MinValue, short.MaxValue));
                return ms.ToArray();
            }
        }

        /// <summary>Decodes 8/16/24/32-bit PCM or 32-bit float WAV into an AudioClip. Returns null on failure.</summary>
        public static AudioClip ToAudioClip(byte[] wav, string name = "wav")
        {
            try
            {
                if (wav == null || wav.Length < 44) return null;
                if (wav[0] != 'R' || wav[1] != 'I' || wav[2] != 'F' || wav[3] != 'F') return null;
                int pos = 12;
                short format = 1, channels = 1, bits = 16; int sampleRate = 16000;
                float[] samples = null;
                while (pos + 8 <= wav.Length)
                {
                    string id = System.Text.Encoding.ASCII.GetString(wav, pos, 4);
                    int size = BitConverter.ToInt32(wav, pos + 4);
                    pos += 8;
                    if (id == "fmt ")
                    {
                        format = BitConverter.ToInt16(wav, pos); channels = BitConverter.ToInt16(wav, pos + 2);
                        sampleRate = BitConverter.ToInt32(wav, pos + 4); bits = BitConverter.ToInt16(wav, pos + 14);
                    }
                    else if (id == "data")
                    {
                        int bytesPerSample = bits / 8;
                        int count = Math.Min(size, wav.Length - pos) / bytesPerSample;
                        samples = new float[count];
                        for (int i = 0; i < count; i++)
                        {
                            int p = pos + i * bytesPerSample;
                            switch (bits)
                            {
                                case 8: samples[i] = (wav[p] - 128) / 128f; break;
                                case 16: samples[i] = BitConverter.ToInt16(wav, p) / 32768f; break;
                                case 24: samples[i] = ((wav[p] | (wav[p + 1] << 8) | ((sbyte)wav[p + 2] << 16))) / 8388608f; break;
                                case 32: samples[i] = format == 3 ? BitConverter.ToSingle(wav, p) : BitConverter.ToInt32(wav, p) / 2147483648f; break;
                            }
                        }
                        break;
                    }
                    pos += size + (size & 1);
                }
                if (samples == null) return null;
                var clip = AudioClip.Create(name, samples.Length / channels, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }
            catch { return null; }
        }

        /// <summary>Simple linear resampler (used to feed 16 kHz to STT).</summary>
        public static float[] Resample(float[] input, int fromRate, int toRate)
        {
            if (fromRate == toRate) return input;
            int outLen = (int)((long)input.Length * toRate / fromRate);
            var output = new float[outLen];
            float ratio = (float)fromRate / toRate;
            for (int i = 0; i < outLen; i++)
            {
                float src = i * ratio; int i0 = (int)src; int i1 = Mathf.Min(i0 + 1, input.Length - 1);
                float t = src - i0;
                output[i] = input[i0] * (1 - t) + input[i1] * t;
            }
            return output;
        }
    }
}
