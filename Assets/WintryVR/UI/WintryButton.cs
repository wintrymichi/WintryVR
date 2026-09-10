using System;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;

namespace WintryVR.UI
{
    /// <summary>
    /// A small 3D glass button: rounded plate + label + collider. Activated by gaze/hand/controller pointer
    /// through <see cref="UIInteractionManager"/>. No Canvas/EventSystem required.
    /// Hover and press are animated rather than snapped, so a button reads as a physical control: it lifts
    /// slightly toward the user on hover and dips on press.
    /// </summary>
    public class WintryButton : MonoBehaviour
    {
        public Action OnClick;
        public string Label { get; private set; }

        private const float HoverScale = 1.06f;
        private const float PressScale = 0.93f;
        private const float PressSeconds = 0.12f;

        private Material _mat;
        private Color _base = new Color(0.25f, 0.4f, 0.6f);
        private Color _hover = new Color(0.4f, 0.65f, 0.95f);
        private bool _hovered;
        private float _press;          // 1 right after a click, decaying to 0
        private float _scale = 1f;     // smoothed, so hover and press never fight over localScale
        private TextMesh _text;
        private float _width, _height;
        private bool _needsFit;

        public static WintryButton Create(string label, Transform parent, Vector3 localPos, float width, float height, Action onClick, Transform head = null, Color? color = null)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var btn = go.AddComponent<WintryButton>();
            btn.OnClick = onClick; btn.Label = label;
            btn._width = width; btn._height = height;
            if (color.HasValue) { btn._base = color.Value; btn._hover = Color.Lerp(color.Value, Color.white, 0.35f); }

            var plate = new GameObject("Plate");
            plate.transform.SetParent(go.transform, false);
            plate.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.RoundedRect(width, height, Mathf.Min(width, height) * 0.35f, 5);
            var mr = plate.AddComponent<MeshRenderer>();
            btn._mat = WintryMaterials.Glass(btn._base, 0.55f, 1.2f);
            WintryMaterials.SetPanelShape(btn._mat, width, height, Mathf.Min(width, height) * 0.35f, 0.004f);
            mr.sharedMaterial = btn._mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(width, height, 0.01f);
            col.isTrigger = true;

            var tgo = new GameObject("Label");
            tgo.transform.SetParent(go.transform, false);
            tgo.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            var tm = tgo.AddComponent<TextMesh>();
            // height * 0.045 characterSize at the old 40 px raster is the size this label has always had
            WorldLabel.Configure(tm, height * 0.045f * 40f, TextAnchor.MiddleCenter, TextAlignment.Center, Color.white);
            tm.text = label;
            btn._text = tm;
            btn._needsFit = true;
            return btn;
        }

        /// <summary>
        /// Shrinks the label until it fits the plate. A TextMesh does not rebuild its mesh until the end of the
        /// frame it was written in, so its renderer bounds are still the previous string's at this point:
        /// the fit has to wait for bounds that describe the text actually on screen.
        /// </summary>
        private void FitText()
        {
            if (_text == null) return;
            var r = _text.GetComponent<Renderer>();
            if (r == null) return;
            float textWidth = r.bounds.size.x;
            if (textWidth <= 0f) return;                       // mesh not built yet, try again next frame
            _needsFit = false;
            float lossy = Mathf.Abs(transform.lossyScale.x) > 1e-5f ? Mathf.Abs(transform.lossyScale.x) : 1f;
            float allowed = _width * 0.9f * lossy;
            if (textWidth > allowed) _text.characterSize *= allowed / textWidth;
        }

        public void SetLabel(string label)
        {
            Label = label;
            if (_text == null) return;
            _text.text = label;
            _needsFit = true;
        }

        public void SetHover(bool hovered)
        {
            if (hovered == _hovered) return;
            _hovered = hovered;
            WintryMaterials.SetColor(_mat, hovered ? new Color(_hover.r, _hover.g, _hover.b, 0.75f)
                                                   : new Color(_base.r, _base.g, _base.b, 0.55f));
        }

        public void Click()
        {
            _press = 1f;
            try { OnClick?.Invoke(); }
            catch (Exception ex) { WintryLog.E("UI", "Button action failed", ex); }
        }

        private void LateUpdate()
        {
            if (_needsFit) FitText();

            bool reduced = WintrySettings.Current.Accessibility.ReducedMotion;
            if (_press > 0f) _press = Mathf.Max(0f, _press - Time.deltaTime / PressSeconds);

            float target = Mathf.Lerp(_hovered ? HoverScale : 1f, PressScale, _press);
            _scale = reduced ? target : Mathf.Lerp(_scale, target, 1f - Mathf.Exp(-18f * Time.deltaTime));
            transform.localScale = Vector3.one * _scale;
        }
    }
}
