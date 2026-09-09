using UnityEngine;
using WintryVR.Core;

namespace WintryVR.UI
{
    /// <summary>
    /// Lightweight 3D text label (TextMesh + subtle backing) that billboards toward the user. Used by spatial
    /// pointers, highlights and translation overlays; no canvas required so it batches cheaply.
    /// </summary>
    public class WorldLabel : MonoBehaviour
    {
        public TextMesh Text { get; private set; }
        private Transform _backing;
        private Transform _head;
        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        public static WorldLabel Create(string text, Transform parent, Vector3 localOffset, float sizeMeters, Transform head, Color? color = null)
        {
            var go = new GameObject("WorldLabel");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;
            var label = go.AddComponent<WorldLabel>();
            label._head = head;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var tm = textGo.AddComponent<TextMesh>();
            tm.font = DefaultFont;
            tm.fontSize = 48;
            tm.characterSize = sizeMeters * 0.04f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color ?? new Color(0.95f, 0.98f, 1f);
            tm.text = text;
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null && tm.font != null && tm.font.material != null)
            {
                mr.sharedMaterial = tm.font.material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            label.Text = tm;

            var backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(backing.GetComponent<Collider>());
            backing.name = "Backing";
            backing.transform.SetParent(go.transform, false);
            backing.transform.localPosition = new Vector3(0, 0, 0.002f);
            backing.GetComponent<MeshRenderer>().sharedMaterial = WintryMaterials.Glass(new Color(0.05f, 0.08f, 0.14f), 0.55f);
            label._backing = backing.transform;
            label.Refit();
            return label;
        }

        public void SetText(string text)
        {
            if (Text != null) { Text.text = text; Refit(); }
        }

        private void Refit()
        {
            if (_backing == null || Text == null) return;
            var r = Text.GetComponent<Renderer>();
            if (r == null) return;
            var size = r.bounds.size;
            // bounds are world-space; convert to local scale (label is uniformly scaled)
            float s = transform.lossyScale.x > 0 ? transform.lossyScale.x : 1f;
            _backing.localScale = new Vector3(size.x / s + 0.03f, size.y / s + 0.02f, 1f);
        }

        private void LateUpdate()
        {
            if (_head == null) { var cam = UnityEngine.Camera.main; if (cam != null) _head = cam.transform; else return; }
            Vector3 toHead = transform.position - _head.position;
            if (toHead.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(toHead, Vector3.up);
        }
    }
}
