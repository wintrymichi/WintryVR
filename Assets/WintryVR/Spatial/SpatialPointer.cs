using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.UI;

namespace WintryVR.Spatial
{
    /// <summary>
    /// An indicator anchored to a world position: a marker ring on the target, a soft beam from Wintry, a
    /// direction arrow when the target is out of view, and a label. Updates as the user moves.
    /// </summary>
    public class SpatialPointer : MonoBehaviour
    {
        public Vector3 Target;
        public string Label;
        public float Lifetime = 8f;
        public Transform Origin;      // Wintry's position (beam start)
        public ISpatialService Spatial;

        private Transform _marker;
        private LineRenderer _beam;
        private Transform _arrow;
        private WorldLabel _label;
        private float _age;
        private Material _markerMat;
        private Color _color = new Color(0.55f, 0.85f, 1f);

        public void Build(Color color)
        {
            _color = color;
            // marker: flat ring on the target
            var marker = new GameObject("Marker");
            marker.transform.SetParent(transform, false);
            var mf = marker.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshes.Ring(0.06f, 0.085f, 32);
            var mr = marker.AddComponent<MeshRenderer>();
            _markerMat = WintryMaterials.Glow(color, color, 2f, 0.9f);
            mr.sharedMaterial = _markerMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _marker = marker.transform;

            // beam
            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.AddComponent<LineRenderer>();
            _beam.positionCount = 12;
            _beam.startWidth = 0.006f; _beam.endWidth = 0.012f;
            _beam.material = WintryMaterials.Glow(color, color, 1.5f, 0.35f);
            _beam.startColor = new Color(color.r, color.g, color.b, 0.05f);
            _beam.endColor = new Color(color.r, color.g, color.b, 0.6f);
            _beam.useWorldSpace = true;
            _beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // arrow (used when target is outside the view)
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(transform, false);
            var amf = arrow.AddComponent<MeshFilter>();
            amf.sharedMesh = ProceduralMeshes.Arrow(0.05f, 0.09f);
            var amr = arrow.AddComponent<MeshRenderer>();
            amr.sharedMaterial = _markerMat;
            amr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _arrow = arrow.transform;
            _arrow.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(Label))
                _label = WorldLabel.Create(Label, transform, Vector3.zero, 1f, Spatial != null ? Spatial.Head : null, color);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (Lifetime > 0 && _age > Lifetime) { Destroy(gameObject); return; }
            if (Spatial == null) return;

            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 4f);
            _marker.position = Target;
            _marker.rotation = Quaternion.Euler(90f, Time.time * 30f, 0f);
            _marker.localScale = Vector3.one * pulse;

            bool inView = Spatial.IsInFieldOfView(Target, 38f);
            if (_beam != null)
            {
                Vector3 start = Origin != null ? Origin.position : Spatial.ComfortablePosition(0.6f, 20f, -10f);
                Vector3 mid = Vector3.Lerp(start, Target, 0.5f) + Vector3.up * 0.08f;
                for (int i = 0; i < _beam.positionCount; i++)
                {
                    float t = i / (float)(_beam.positionCount - 1);
                    Vector3 p = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * Target;
                    _beam.SetPosition(i, p);
                }
                _beam.enabled = inView;
            }
            if (_arrow != null)
            {
                _arrow.gameObject.SetActive(!inView);
                if (!inView)
                {
                    Vector3 pos = Spatial.ComfortablePosition(0.8f, 0f, -12f);
                    Vector3 dir = (Target - pos);
                    _arrow.position = pos;
                    _arrow.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }
            if (_label != null)
            {
                _label.transform.position = Target + Vector3.up * 0.12f;
                if (!inView) _label.transform.position = _arrow.position + Vector3.up * 0.06f;
            }
            float fade = Lifetime > 0 ? Mathf.Clamp01((Lifetime - _age) / 1.5f) : 1f;
            WintryMaterials.SetAlpha(_markerMat, 0.9f * fade);
        }
    }

    /// <summary>Owns active pointers; enforces a small budget so the room never fills with indicators.</summary>
    public class SpatialPointerManager : MonoBehaviour
    {
        public int MaxPointers = 2;
        public ISpatialService Spatial;
        public Transform Origin;
        public Color Color = new Color(0.55f, 0.85f, 1f);
        private readonly List<SpatialPointer> _active = new List<SpatialPointer>();

        public SpatialPointer PointAt(Vector3 target, string label, float seconds)
        {
            _active.RemoveAll(p => p == null);
            while (_active.Count >= MaxPointers) { Destroy(_active[0].gameObject); _active.RemoveAt(0); }
            var go = new GameObject("SpatialPointer");
            go.transform.SetParent(transform, false);
            var p = go.AddComponent<SpatialPointer>();
            p.Target = target; p.Label = label; p.Lifetime = seconds; p.Origin = Origin; p.Spatial = Spatial;
            p.Build(Color);
            _active.Add(p);
            return p;
        }

        public void ClearAll()
        {
            foreach (var p in _active) if (p != null) Destroy(p.gameObject);
            _active.Clear();
        }
    }
}
