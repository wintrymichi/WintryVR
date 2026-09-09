using System;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.UI
{
    /// <summary>
    /// A small 3D glass button: rounded plate + label + collider. Activated by gaze/hand/controller pointer
    /// through <see cref="UIInteractionManager"/>. No Canvas/EventSystem required.
    /// </summary>
    public class WintryButton : MonoBehaviour
    {
        public Action OnClick;
        public string Label { get; private set; }
        private Material _mat;
        private Color _base = new Color(0.25f, 0.4f, 0.6f);
        private Color _hover = new Color(0.4f, 0.65f, 0.95f);
        private bool _hovered;
        private TextMesh _text;

        public static WintryButton Create(string label, Transform parent, Vector3 localPos, float width, float height, Action onClick, Transform head = null, Color? color = null)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var btn = go.AddComponent<WintryButton>();
            btn.OnClick = onClick; btn.Label = label;
            if (color.HasValue) { btn._base = color.Value; btn._hover = Color.Lerp(color.Value, Color.white, 0.35f); }

            var plate = new GameObject("Plate");
            plate.transform.SetParent(go.transform, false);
            plate.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.RoundedRect(width, height, Mathf.Min(width, height) * 0.35f, 5);
            var mr = plate.AddComponent<MeshRenderer>();
            btn._mat = WintryMaterials.Glass(btn._base, 0.55f, 1.2f);
            mr.sharedMaterial = btn._mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(width, height, 0.01f);
            col.isTrigger = true;

            var tgo = new GameObject("Label");
            tgo.transform.SetParent(go.transform, false);
            tgo.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            tgo.transform.localRotation = Quaternion.identity;
            var tm = tgo.AddComponent<TextMesh>();
            tm.font = WorldLabel.DefaultFont; tm.fontSize = 40; tm.characterSize = height * 0.22f / 0.04f * 0.04f;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.text = label; tm.color = Color.white;
            var tmr = tgo.GetComponent<MeshRenderer>(); if (tmr != null && tm.font != null) tmr.sharedMaterial = tm.font.material;
            btn._text = tm;
            btn.FitText(width, height);
            return btn;
        }

        private void FitText(float width, float height)
        {
            if (_text == null) return;
            _text.characterSize = height * 0.45f * 0.1f;
            var r = _text.GetComponent<Renderer>();
            if (r != null && r.bounds.size.x > width * 0.9f && r.bounds.size.x > 0)
                _text.characterSize *= width * 0.9f / r.bounds.size.x;
        }

        public void SetLabel(string label) { Label = label; if (_text != null) _text.text = label; }

        public void SetHover(bool hovered)
        {
            if (hovered == _hovered) return;
            _hovered = hovered;
            WintryMaterials.SetColor(_mat, hovered ? new Color(_hover.r, _hover.g, _hover.b, 0.75f) : new Color(_base.r, _base.g, _base.b, 0.55f));
            transform.localScale = Vector3.one * (hovered ? 1.06f : 1f);
        }

        public void Click()
        {
            transform.localScale = Vector3.one * 0.95f;
            try { OnClick?.Invoke(); }
            catch (Exception ex) { WintryLog.E("UI", "Button action failed", ex); }
        }
    }
}
