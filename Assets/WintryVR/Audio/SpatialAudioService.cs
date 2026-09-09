using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;

namespace WintryVR.Audio
{
    /// <summary>
    /// Spatial audio: Wintry's voice and state cues come from the Core/character position; pointer cues can
    /// come from the direction of the object being indicated. Effects are deliberately subtle.
    /// </summary>
    public class SpatialAudioService : MonoBehaviour
    {
        public AudioSource VoiceSource { get; private set; }
        public AudioSource CueSource { get; private set; }
        private AudioSource _pointerSource;
        private AssistantState _lastState = AssistantState.Idle;

        public void Initialize(Transform attachTo)
        {
            VoiceSource = Create("WintryVoice", attachTo, 0.95f);
            CueSource = Create("WintryCues", attachTo, 0.9f);
            var pointerGo = new GameObject("WintryPointerAudio");
            pointerGo.transform.SetParent(transform, false);
            _pointerSource = Create("PointerCue", pointerGo.transform, 1f);
            WintryEvents.Subscribe<StateChangedEvent>(e => PlayState(e.Current));
        }

        private AudioSource Create(string name, Transform parent, float blend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = blend;           // 1 = fully spatial
            src.spatialize = true;              // lets the Meta/Unity spatializer plugin take over when configured
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 0.5f; src.maxDistance = 8f;
            src.dopplerLevel = 0f;
            src.playOnAwake = false;
            src.loop = false;
            return src;
        }

        public void Attach(Transform t)
        {
            if (VoiceSource != null) VoiceSource.transform.SetParent(t, false);
            if (CueSource != null) CueSource.transform.SetParent(t, false);
            if (VoiceSource != null) VoiceSource.transform.localPosition = Vector3.zero;
            if (CueSource != null) CueSource.transform.localPosition = Vector3.zero;
        }

        public void PlayState(AssistantState state)
        {
            if (state == _lastState) return;
            _lastState = state;
            string cue = null;
            switch (state)
            {
                case AssistantState.Listening: cue = "listen"; break;
                case AssistantState.Thinking: cue = "think"; break;
                case AssistantState.Vision: cue = "vision"; break;
                case AssistantState.Searching: cue = "search"; break;
                case AssistantState.Error: cue = "error"; break;
                case AssistantState.Offline: cue = "offline"; break;
            }
            if (cue != null) PlayCue(cue);
        }

        public void PlayCue(string name, float volume = 0.5f)
        {
            if (CueSource == null) return;
            CueSource.PlayOneShot(ProceduralSfx.Get(name), volume * WintrySettings.Current.Voice.Volume);
        }

        /// <summary>Plays a cue from (near) the direction of a world position, e.g. when pointing at an object.</summary>
        public void PlayCueAt(string name, Vector3 worldPosition, float volume = 0.45f)
        {
            if (_pointerSource == null) return;
            var head = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null;
            Vector3 pos = worldPosition;
            if (head != null)
            {
                // keep the cue within ~1.5 m so it is audible but still directional
                Vector3 dir = (worldPosition - head.position);
                if (dir.magnitude > 1.5f) pos = head.position + dir.normalized * 1.5f;
            }
            _pointerSource.transform.position = pos;
            _pointerSource.PlayOneShot(ProceduralSfx.Get(name), volume * WintrySettings.Current.Voice.Volume);
        }
    }
}
