using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Character
{
    /// <summary>
    /// Code-driven animation for the character (no clips needed): breathing/float in IDLE, gaze toward the
    /// user when LISTENING, ring spin and head tilt when THINKING, a scanning band when in VISION, orbiting
    /// motes when SEARCHING, voice-synced sway when SPEAKING, a discreet dim/flicker on ERROR and a dormant
    /// pose when OFFLINE. Reduced-motion setting scales every amplitude down.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public Transform Body, Head, Ring, ScanBand, Motes;
        public Material RingMaterial, ScanMaterial, BodyMaterial;
        public FacialExpressionController Face;
        public WintryLookDefinition Look;
        public bool ReducedMotion;

        private AssistantState _state = AssistantState.Idle;
        private float _stateTime;
        private Vector3 _lookTarget;
        private bool _hasLookTarget;
        private Transform _user;
        private Quaternion _headTarget = Quaternion.identity;
        private float _ringSpin;
        private Vector3 _bodyBaseScale = Vector3.one;
        private Vector3 _bodyBasePos;
        public LipSyncController LipSync;

        public void Initialize(Transform user) { _user = user; if (Body != null) { _bodyBaseScale = Body.localScale; _bodyBasePos = Body.localPosition; } }

        public void SetState(AssistantState s)
        {
            if (s != _state) _stateTime = 0f;
            _state = s;
            if (Face == null) return;
            switch (s)
            {
                case AssistantState.Idle: Face.Set(FacialExpression.Neutral); break;
                case AssistantState.Listening: Face.Set(FacialExpression.Curious); break;
                case AssistantState.Thinking: Face.Set(FacialExpression.Thinking); break;
                case AssistantState.Vision: Face.Set(FacialExpression.Curious); break;
                case AssistantState.Searching: Face.Set(FacialExpression.Thinking); break;
                case AssistantState.Speaking: Face.Set(FacialExpression.Happy); break;
                case AssistantState.Error: Face.Set(FacialExpression.Concerned); break;
                case AssistantState.Offline: Face.Set(FacialExpression.Neutral); break;
            }
        }

        public void LookAt(Vector3 worldPoint) { _lookTarget = worldPoint; _hasLookTarget = true; }
        public void LookAtUser() { _hasLookTarget = false; }

        private void Update()
        {
            if (Body == null) return;
            _stateTime += Time.deltaTime;
            float t = Time.time;
            float m = ReducedMotion ? 0.35f : 1f;

            // ---- body float & breathing
            float floatAmp = 0.008f, breath = 0.015f, ringSpeed = 15f, glow = 1f;
            bool scan = false, motes = false;
            Color emission = Look != null ? Look.EmissionColor : Color.cyan;
            switch (_state)
            {
                case AssistantState.Listening: floatAmp = 0.004f; breath = 0.02f; ringSpeed = 40f; glow = 1.25f; break;
                case AssistantState.Thinking: ringSpeed = 120f; glow = 1.1f; break;
                case AssistantState.Vision: scan = true; glow = 1.4f; ringSpeed = 70f; break;
                case AssistantState.Searching: motes = true; ringSpeed = 180f; glow = 1.2f; break;
                case AssistantState.Speaking: floatAmp = 0.006f; ringSpeed = 30f; glow = 1.35f; break;
                case AssistantState.Error: glow = 0.55f + 0.35f * Mathf.PerlinNoise(t * 10f, 1f); emission = Color.Lerp(emission, new Color(1f, 0.55f, 0.35f), 0.6f); ringSpeed = 4f; floatAmp = 0.002f; break;
                case AssistantState.Offline: glow = 0.3f; emission = Color.Lerp(emission, Color.gray, 0.8f); ringSpeed = 0f; floatAmp = 0.001f; breath = 0.004f; break;
            }
            Body.localPosition = _bodyBasePos + Vector3.up * Mathf.Sin(t * 1.2f) * floatAmp * m;
            Body.localScale = _bodyBaseScale * (1f + Mathf.Sin(t * 1.7f) * breath * m);

            // ---- head orientation: look at user or target, with a thinking tilt
            if (Head != null)
            {
                Vector3 target = _hasLookTarget ? _lookTarget : (_user != null ? _user.position : Head.position + transform.forward);
                Vector3 dir = target - Head.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    Quaternion local = Quaternion.Inverse(Head.parent != null ? Head.parent.rotation : Quaternion.identity) * look;
                    // limit the neck
                    Vector3 e = local.eulerAngles;
                    e.x = Mathf.Clamp(Mathf.DeltaAngle(0, e.x), -35f, 35f); e.y = Mathf.Clamp(Mathf.DeltaAngle(0, e.y), -60f, 60f);
                    e.z = _state == AssistantState.Thinking ? 8f * m : 0f;
                    if (LipSync != null) e.x += LipSync.NodDegrees;
                    _headTarget = Quaternion.Euler(e);
                }
                Head.localRotation = Quaternion.Slerp(Head.localRotation, _headTarget, 1f - Mathf.Exp(-6f * Time.deltaTime));
            }

            // ---- halo ring
            if (Ring != null)
            {
                _ringSpin += ringSpeed * m * Time.deltaTime;
                Ring.localRotation = Quaternion.Euler(75f + 5f * Mathf.Sin(t), _ringSpin, 0f);
            }
            if (RingMaterial != null && Look != null) WintryMaterials.SetEmission(RingMaterial, emission * Look.EmissionStrength * glow);
            if (BodyMaterial != null && Look != null) WintryMaterials.SetEmission(BodyMaterial, emission * Look.EmissionStrength * 0.35f * glow);

            // ---- vision scan band sweeps down the body
            if (ScanBand != null)
            {
                ScanBand.gameObject.SetActive(scan);
                if (scan)
                {
                    float y = Mathf.PingPong(_stateTime * 0.35f, 0.26f) - 0.13f;
                    ScanBand.localPosition = new Vector3(0f, y, 0f);
                }
            }
            if (Motes != null)
            {
                Motes.gameObject.SetActive(motes && (Look == null || Look.ShowParticles));
                if (motes) Motes.Rotate(Vector3.up, 200f * m * Time.deltaTime, Space.Self);
            }
        }
    }
}
