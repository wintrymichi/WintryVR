using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.UI;

namespace WintryVR.Spatial
{
    /// <summary>
    /// Smart Highlight: a restrained outline/glow + label on identified objects. Budgeted (few at a time),
    /// time-limited, and confidence-aware (low confidence renders dimmer with a "?" suffix).
    /// </summary>
    public class SmartHighlightManager : MonoBehaviour
    {
        public int MaxSimultaneous = 3;
        public float DefaultSeconds = 10f;
        public ISpatialService Spatial;
        public Color Color = new Color(0.6f, 0.9f, 1f);
        public bool Enabled = true;

        private class Entry { public string ObjectId; public GameObject Root; public float Expires; public Material Mat; public float Alpha; }
        private readonly List<Entry> _entries = new List<Entry>();

        public void Highlight(ObservedObject obj, float seconds = -1f)
        {
            if (!Enabled || obj == null || !obj.HasPosition) return;
            if (seconds < 0) seconds = DefaultSeconds;
            var existing = _entries.Find(e => e.ObjectId == obj.Id);
            if (existing != null) { existing.Expires = Time.time + seconds; existing.Root.transform.position = obj.WorldPosition; return; }
            while (_entries.Count >= MaxSimultaneous) { Destroy(_entries[0].Root); _entries.RemoveAt(0); }

            float alpha = obj.Confidence < 0.6f ? 0.45f : 0.85f;
            var root = new GameObject("Highlight_" + obj.DisplayName);
            root.transform.SetParent(transform, false);
            root.transform.position = obj.WorldPosition;

            // outline shell: wireframe-ish glowing ring set scaled to the object extents
            float r = Mathf.Max(0.06f, Mathf.Max(obj.Extents.x, obj.Extents.z) * 0.9f);
            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(root.transform, false);
            ringGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ringGo.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Ring(r, r + 0.012f, 40);
            var mat = WintryMaterials.Glow(Color, Color, 2f, alpha);
            var mr = ringGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // glow marker above the object
            var dot = new GameObject("Marker");
            dot.transform.SetParent(root.transform, false);
            dot.transform.localPosition = new Vector3(0f, obj.Extents.y + 0.05f, 0f);
            dot.transform.localScale = Vector3.one * 0.025f;
            dot.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Icosphere(1f, 1);
            var dmr = dot.AddComponent<MeshRenderer>();
            dmr.sharedMaterial = mat;
            dmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            string label = obj.DisplayName + (obj.Confidence < 0.6f ? " ?" : "");
            WorldLabel.Create(label, root.transform, new Vector3(0f, obj.Extents.y + 0.11f, 0f), 1f, Spatial != null ? Spatial.Head : null, Color);

            _entries.Add(new Entry { ObjectId = obj.Id, Root = root, Expires = Time.time + seconds, Mat = mat, Alpha = alpha });
        }

        public void Remove(string objectId)
        {
            var e = _entries.Find(x => x.ObjectId == objectId);
            if (e != null) { Destroy(e.Root); _entries.Remove(e); }
        }

        public void ClearAll()
        {
            foreach (var e in _entries) if (e.Root != null) Destroy(e.Root);
            _entries.Clear();
        }

        private void Update()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                float remaining = e.Expires - Time.time;
                if (remaining <= 0f) { Destroy(e.Root); _entries.RemoveAt(i); continue; }
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 3f + i);
                WintryMaterials.SetAlpha(e.Mat, e.Alpha * pulse * Mathf.Clamp01(remaining / 1.2f));
            }
        }
    }
}
