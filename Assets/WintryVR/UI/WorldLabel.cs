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
        private bool _needsRefit;
        private Material _backingMat;
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

        /// <summary>
        /// Glyph raster size for every piece of world text in the app.
        /// </summary>
        /// <remarks>
        /// A dynamic font is rasterised into its atlas at <c>fontSize</c> pixels, and <c>characterSize</c> then
        /// scales that bitmap to metres. The two multiply, so the rendered size depends only on their product
        /// while the sharpness depends on <c>fontSize</c> alone. The UI used to raster at 48 px and magnify it,
        /// which at reading distance in a headset put roughly one texel per screen pixel and made the text
        /// shimmer as the head moved. Rastering at 160 px and scaling down by the same factor keeps every
        /// layout byte-for-byte identical and gives the sampler about three times the texel density.
        /// </remarks>
        public const int RasterFontSize = 160;

        /// <summary>
        /// Sets up a TextMesh at the shared raster size. <paramref name="sizeScale"/> is the
        /// <c>characterSize x fontSize</c> product, i.e. the value that actually determines world size.
        /// </summary>
        public static TextMesh Configure(TextMesh tm, float sizeScale, TextAnchor anchor, TextAlignment alignment, Color color)
        {
            tm.font = DefaultFont;
            tm.fontSize = RasterFontSize;
            tm.characterSize = sizeScale / RasterFontSize;
            tm.anchor = anchor;
            tm.alignment = alignment;
            tm.color = color;
            var mr = tm.GetComponent<MeshRenderer>();
            if (mr != null && tm.font != null && tm.font.material != null)
            {
                mr.sharedMaterial = tm.font.material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            return tm;
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
            Configure(tm, sizeMeters * 0.04f * 48f, TextAnchor.MiddleCenter, TextAlignment.Center,
                      color ?? new Color(0.95f, 0.98f, 1f));
            tm.text = text;
            label.Text = tm;

            var backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(backing.GetComponent<Collider>());
            backing.name = "Backing";
            backing.transform.SetParent(go.transform, false);
            backing.transform.localPosition = new Vector3(0, 0, 0.002f);
            label._backingMat = WintryMaterials.Glass(new Color(0.05f, 0.08f, 0.14f), 0.55f);
            backing.GetComponent<MeshRenderer>().sharedMaterial = label._backingMat;
            label._backing = backing.transform;
            label._needsRefit = true;
            return label;
        }

        public void SetText(string text)
        {
            if (Text == null) return;
            Text.text = text;
            _needsRefit = true;   // the mesh for this string does not exist until the end of the frame
        }

        /// <summary>
        /// Sizes the backing plate to the text. A TextMesh rebuilds its mesh at the end of the frame the string
        /// was assigned in, so reading the renderer bounds straight after writing <c>text</c> measures the
        /// previous string — on the first frame, an empty one. Refitting from LateUpdate measures what is
        /// actually on screen, and it retries while the bounds are still degenerate.
        /// </summary>
        private void Refit()
        {
            if (_backing == null || Text == null) return;
            var r = Text.GetComponent<Renderer>();
            if (r == null) return;
            var size = r.bounds.size;
            if (size.x <= 0f && !string.IsNullOrEmpty(Text.text)) return;   // not built yet
            _needsRefit = false;
            float s = Mathf.Abs(transform.lossyScale.x) > 1e-5f ? Mathf.Abs(transform.lossyScale.x) : 1f;
            bool empty = string.IsNullOrEmpty(Text.text);
            _backing.gameObject.SetActive(!empty);
            if (empty) return;
            float w = size.x / s + 0.03f, h = size.y / s + 0.02f;
            _backing.localScale = new Vector3(w, h, 1f);
            // the plate is a unit quad stretched to fit, so the shader needs the stretched size to keep its
            // edge band an even thickness instead of squashing it along the wider axis
            WintryMaterials.SetPanelShape(_backingMat, w, h, Mathf.Min(w, h) * 0.35f, Mathf.Min(w, h) * 0.12f);
        }

        private void LateUpdate()
        {
            if (_needsRefit) Refit();
            if (_head == null) { var cam = UnityEngine.Camera.main; if (cam != null) _head = cam.transform; else return; }
            Vector3 toHead = transform.position - _head.position;
            if (toHead.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(toHead, Vector3.up);
        }
    }
}
