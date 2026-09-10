using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;

namespace WintryVR.UI
{
    /// <summary>
    /// The pointer's visible end: a small ring where the ray meets a control, and a faint beam running back to
    /// the hand or controller.
    /// </summary>
    /// <remarks>
    /// Without it a spatial pointer is invisible until it happens to land on a button, so aiming becomes guesswork
    /// and near-misses look like the app ignoring you. Both parts stay deliberately quiet — the beam is thin and
    /// mostly transparent, the ring is a few millimetres — because the room is meant to stay the subject. The
    /// whole thing hides the moment there is nothing to point at, so it never floats over the world unprompted.
    /// </remarks>
    public class UIPointerCursor : MonoBehaviour
    {
        private const float RingInner = 0.0045f;
        private const float RingOuter = 0.0075f;
        private const float BeamThickness = 0.0022f;

        private Transform _ring, _beam;
        private Material _ringMat, _beamMat;
        private Transform _head;
        private float _visible;        // 0 hidden, 1 fully shown
        private float _engaged;        // 0 pointing at nothing, 1 over an interactive control
        private Color _idle = new Color(0.55f, 0.78f, 1f);
        private Color _active = new Color(0.75f, 0.92f, 1f);

        public static UIPointerCursor Create(Transform parent, Transform head)
        {
            var go = new GameObject("PointerCursor");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<UIPointerCursor>();
            c._head = head;

            var ring = new GameObject("Ring");
            ring.transform.SetParent(go.transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Ring(RingInner, RingOuter, 28);
            var rmr = ring.AddComponent<MeshRenderer>();
            c._ringMat = WintryMaterials.Glow(c._idle, c._idle, 1.8f, 0.9f);
            rmr.sharedMaterial = c._ringMat;
            rmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rmr.receiveShadows = false;
            c._ring = ring.transform;

            var beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            beam.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Quad(1f, 1f);
            var bmr = beam.AddComponent<MeshRenderer>();
            c._beamMat = WintryMaterials.Glow(c._idle, c._idle, 0.9f, 0.16f);
            bmr.sharedMaterial = c._beamMat;
            bmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bmr.receiveShadows = false;
            c._beam = beam.transform;

            c.SetShown(false);
            return c;
        }

        /// <summary>Places the cursor for this frame. <paramref name="overControl"/> brightens it.</summary>
        public void Point(Ray ray, float distance, bool overControl)
        {
            _targetVisible = 1f;
            _targetEngaged = overControl ? 1f : 0f;
            _ray = ray;
            _distance = Mathf.Max(0.05f, distance);
        }

        public void Hide() { _targetVisible = 0f; }

        private Ray _ray;
        private float _distance = 1f;
        private float _targetVisible;
        private float _targetEngaged;

        private void SetShown(bool shown)
        {
            if (_ring != null) _ring.gameObject.SetActive(shown);
            if (_beam != null) _beam.gameObject.SetActive(shown);
        }

        private void LateUpdate()
        {
            bool reduced = WintrySettings.Current.Accessibility.ReducedMotion;
            float k = reduced ? 1f : 1f - Mathf.Exp(-16f * Time.deltaTime);
            _visible = Mathf.Lerp(_visible, _targetVisible, k);
            _engaged = Mathf.Lerp(_engaged, _targetEngaged, k);
            // the caller re-asserts Point() every frame it wants the cursor; otherwise it fades out
            _targetVisible = 0f;

            bool shown = _visible > 0.01f;
            if (_ring == null || _beam == null) return;
            if (_ring.gameObject.activeSelf != shown) SetShown(shown);
            if (!shown) return;

            Vector3 hit = _ray.GetPoint(_distance);
            Vector3 head = _head != null ? _head.position : hit - _ray.direction;

            // ring: at the hit point, facing the viewer, a touch larger when it is over something clickable
            Vector3 toHead = head - hit;
            _ring.position = hit - _ray.direction * 0.004f;   // lift off the surface so it does not z-fight
            if (toHead.sqrMagnitude > 1e-6f) _ring.rotation = Quaternion.LookRotation(-toHead.normalized, Vector3.up);
            _ring.localScale = Vector3.one * (_visible * Mathf.Lerp(1f, 1.5f, _engaged));

            // beam: a billboarded strip from the pointer origin to the hit point
            Vector3 mid = (_ray.origin + hit) * 0.5f;
            Vector3 awayFromHead = (mid - head);
            _beam.position = mid;
            if (awayFromHead.sqrMagnitude > 1e-6f)
                _beam.rotation = Quaternion.LookRotation(awayFromHead.normalized, _ray.direction);
            _beam.localScale = new Vector3(BeamThickness * Mathf.Lerp(1f, 1.6f, _engaged), _distance, 1f);

            Color c = Color.Lerp(_idle, _active, _engaged);
            WintryMaterials.SetColor(_ringMat, c);
            WintryMaterials.SetEmission(_ringMat, c * Mathf.Lerp(1.4f, 2.6f, _engaged));
            WintryMaterials.SetAlpha(_ringMat, _visible * Mathf.Lerp(0.55f, 1f, _engaged));
            WintryMaterials.SetColor(_beamMat, c);
            WintryMaterials.SetAlpha(_beamMat, _visible * Mathf.Lerp(0.07f, 0.2f, _engaged));
        }
    }
}
