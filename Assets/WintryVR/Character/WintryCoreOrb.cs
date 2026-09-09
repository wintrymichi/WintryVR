using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Character
{
    /// <summary>
    /// The Core: Wintry's minimal form. A small layered orb (inner glow sphere, glass shell, thin halo ring
    /// and a few orbiting motes) with per-state animation, light and colour. Kept tiny and off-centre.
    /// </summary>
    public class WintryCoreOrb : MonoBehaviour
    {
        public float Radius = 0.045f;
        private Transform _inner, _shell, _ring, _motes;
        private Material _innerMat, _shellMat, _ringMat, _moteMat;
        private Light _light;
        private AssistantState _state = AssistantState.Idle;
        private WintryLookDefinition _look;
        private float _stateTime;
        private float _visibility = 1f;
        private readonly Transform[] _moteArr = new Transform[5];
        private float _errorFlicker;
        private bool _reducedMotion;

        public void Build(WintryLookDefinition look)
        {
            _look = look;
            _inner = MakeSphere("Inner", Radius * 0.55f, 2, out _innerMat, look.EmissionColor, look.EmissionColor, look.EmissionStrength * 1.4f, 1f);
            _shell = MakeSphere("Shell", Radius, 3, out _shellMat, look.PrimaryColor, look.EmissionColor, look.EmissionStrength * 0.3f, 0.28f);
            var ringGo = new GameObject("Halo");
            ringGo.transform.SetParent(transform, false);
            ringGo.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Torus(Radius * 1.45f, Radius * 0.05f, 48, 8);
            var rr = ringGo.AddComponent<MeshRenderer>();
            _ringMat = WintryMaterials.Glow(look.EmissionColor, look.EmissionColor, look.EmissionStrength, 0.8f);
            rr.sharedMaterial = _ringMat; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring = ringGo.transform;
            _ring.localRotation = Quaternion.Euler(70f, 0f, 20f);

            _motes = new GameObject("Motes").transform;
            _motes.SetParent(transform, false);
            _moteMat = WintryMaterials.Glow(look.EmissionColor, look.EmissionColor, look.EmissionStrength * 1.2f, 0.9f);
            for (int i = 0; i < _moteArr.Length; i++)
            {
                var m = new GameObject("Mote" + i);
                m.transform.SetParent(_motes, false);
                m.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Icosphere(Radius * 0.09f, 1);
                var mr = m.AddComponent<MeshRenderer>(); mr.sharedMaterial = _moteMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _moteArr[i] = m.transform;
            }
            _motes.gameObject.SetActive(look.ShowParticles);

            var lightGo = new GameObject("CoreLight");
            lightGo.transform.SetParent(transform, false);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point; _light.range = 0.6f; _light.intensity = 0.6f; _light.color = look.EmissionColor;
            _light.shadows = LightShadows.None;
        }

        private Transform MakeSphere(string name, float r, int subdiv, out Material mat, Color color, Color emission, float strength, float alpha)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Icosphere(r, subdiv);
            var mr = go.AddComponent<MeshRenderer>();
            mat = WintryMaterials.Glow(color, emission, strength, alpha, alpha < 1 ? 2.5f : 1f);
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        public void ApplyLook(WintryLookDefinition look)
        {
            _look = look;
            WintryMaterials.SetColor(_innerMat, look.EmissionColor); WintryMaterials.SetEmission(_innerMat, look.EmissionColor * look.EmissionStrength * 1.4f);
            WintryMaterials.SetColor(_shellMat, look.PrimaryColor); WintryMaterials.SetEmission(_shellMat, look.EmissionColor * look.EmissionStrength * 0.3f);
            WintryMaterials.SetColor(_ringMat, look.EmissionColor); WintryMaterials.SetEmission(_ringMat, look.EmissionColor * look.EmissionStrength);
            WintryMaterials.SetColor(_moteMat, look.EmissionColor); WintryMaterials.SetEmission(_moteMat, look.EmissionColor * look.EmissionStrength * 1.2f);
            if (_light != null) _light.color = look.EmissionColor;
            if (_motes != null) _motes.gameObject.SetActive(look.ShowParticles);
        }

        public void SetState(AssistantState state)
        {
            if (state != _state) _stateTime = 0f;
            _state = state;
        }

        public void SetReducedMotion(bool reduced) { _reducedMotion = reduced; }

        /// <summary>0 = invisible, 1 = fully visible (used when morphing to/from the character).</summary>
        public void SetVisibility(float v)
        {
            _visibility = Mathf.Clamp01(v);
            transform.localScale = Vector3.one * Mathf.Max(0.001f, _visibility);
            WintryMaterials.SetAlpha(_shellMat, 0.28f * _visibility);
            WintryMaterials.SetAlpha(_ringMat, 0.8f * _visibility);
            WintryMaterials.SetAlpha(_innerMat, _visibility);
            if (_light != null) _light.enabled = _visibility > 0.05f;
        }

        private void Update()
        {
            if (_inner == null) return;
            _stateTime += Time.deltaTime;
            float t = Time.time;
            float motion = _reducedMotion ? 0.35f : 1f;
            float pulse = 1f, ringSpeed = 20f, moteSpeed = 40f, glow = 1f, spread = 1.35f;
            Color emission = _look.EmissionColor;

            switch (_state)
            {
                case AssistantState.Idle: pulse = 1f + 0.04f * Mathf.Sin(t * 1.6f); glow = 0.8f + 0.1f * Mathf.Sin(t * 1.6f); break;
                case AssistantState.Listening: pulse = 1f + 0.10f * Mathf.Sin(t * 5f); glow = 1.3f; ringSpeed = 60f; spread = 1.6f; break;
                case AssistantState.Thinking: pulse = 1f + 0.06f * Mathf.Sin(t * 3f); ringSpeed = 140f; moteSpeed = 160f; glow = 1.1f; break;
                case AssistantState.Vision: pulse = 1.08f; ringSpeed = 90f; glow = 1.5f; emission = Color.Lerp(_look.EmissionColor, Color.white, 0.4f); spread = 1.9f; break;
                case AssistantState.Searching: pulse = 1f + 0.05f * Mathf.Sin(t * 8f); moteSpeed = 260f; ringSpeed = 200f; glow = 1.2f; spread = 2.2f; break;
                case AssistantState.Speaking: pulse = 1f + 0.08f * Mathf.Abs(Mathf.Sin(t * 9f)); glow = 1.4f; ringSpeed = 40f; break;
                case AssistantState.Error: _errorFlicker = Mathf.PerlinNoise(t * 12f, 0f); pulse = 0.95f; glow = 0.5f + 0.4f * _errorFlicker; emission = Color.Lerp(_look.EmissionColor, new Color(1f, 0.55f, 0.35f), 0.7f); ringSpeed = 5f; break;
                case AssistantState.Offline: pulse = 0.9f; glow = 0.35f; emission = Color.Lerp(_look.EmissionColor, Color.gray, 0.75f); ringSpeed = 0f; moteSpeed = 0f; break;
            }
            pulse = Mathf.Lerp(1f, pulse, motion);
            _inner.localScale = Vector3.one * pulse;
            _shell.localScale = Vector3.one * (1f + (pulse - 1f) * 0.4f);
            _ring.Rotate(Vector3.up, ringSpeed * motion * Time.deltaTime, Space.Self);
            WintryMaterials.SetEmission(_innerMat, emission * _look.EmissionStrength * 1.4f * glow);
            WintryMaterials.SetEmission(_ringMat, emission * _look.EmissionStrength * glow);
            if (_light != null) { _light.color = emission; _light.intensity = 0.6f * glow * _visibility; }
            if (_motes != null && _motes.gameObject.activeSelf)
            {
                for (int i = 0; i < _moteArr.Length; i++)
                {
                    float a = t * moteSpeed * motion * Mathf.Deg2Rad + i * Mathf.PI * 2f / _moteArr.Length;
                    float y = Mathf.Sin(t * 1.3f + i) * Radius * 0.6f;
                    _moteArr[i].localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Radius * spread + Vector3.up * y;
                }
            }
            // gentle float
            transform.localPosition = Vector3.up * Mathf.Sin(t * 1.1f) * 0.006f * motion;
        }
    }
}
