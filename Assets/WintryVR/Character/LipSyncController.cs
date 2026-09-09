using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Character
{
    /// <summary>
    /// Amplitude-driven lip sync. Wintry's "mouth" is a luminous slit under the eyes that opens with the
    /// voice envelope; the head adds micro-nods and the eyes brighten slightly on stressed syllables.
    /// Works with any TTS because it reads the AudioSource output, not phoneme data.
    /// </summary>
    public class LipSyncController : MonoBehaviour
    {
        public Transform Mouth;
        /// <summary>Head nod (degrees) driven by the voice envelope; applied by CharacterAnimator.</summary>
        public float NodDegrees { get; private set; }
        public Material MouthMaterial;
        public Color MouthColor = new Color(0.6f, 0.9f, 1f);
        public float MaxOpen = 0.012f;
        public float Smoothing = 18f;

        private float _amp, _target, _nod;
        private Vector3 _mouthBase;
        private bool _subscribed;

        private void OnEnable()
        {
            if (!_subscribed) { WintryEvents.Subscribe<SpeechAmplitudeEvent>(OnAmp); _subscribed = true; }
            if (Mouth != null) _mouthBase = Mouth.localScale;
        }

        private void OnDisable()
        {
            if (_subscribed) { WintryEvents.Unsubscribe<SpeechAmplitudeEvent>(OnAmp); _subscribed = false; }
        }

        private void OnAmp(SpeechAmplitudeEvent e) { _target = e.Amplitude; }

        private void Update()
        {
            float k = 1f - Mathf.Exp(-Smoothing * Time.deltaTime);
            _amp = Mathf.Lerp(_amp, _target, k);
            if (Mouth != null)
            {
                float open = MaxOpen * _amp;
                Mouth.localScale = new Vector3(_mouthBase.x * (1f + _amp * 0.25f), Mathf.Max(0.0015f, _mouthBase.y + open), _mouthBase.z);
            }
            if (MouthMaterial != null) WintryMaterials.SetEmission(MouthMaterial, MouthColor * (0.6f + 1.8f * _amp));
            _nod = Mathf.Lerp(_nod, _amp * 2.5f, k * 0.5f);
            NodDegrees = _nod * Mathf.Sin(Time.time * 6f);
        }
    }
}
