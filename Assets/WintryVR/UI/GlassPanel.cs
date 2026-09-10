using System;
using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;

namespace WintryVR.UI
{
    /// <summary>
    /// A floating glass panel: rounded translucent plate with a subtle edge glow, a title, a body text and an
    /// optional button row. Spatially anchored, billboarded toward the user, movable (pinch-hold on the title
    /// bar), resizable (± buttons) and closable. The base for information cards, history, settings, debug.
    /// </summary>
    public class GlassPanel : MonoBehaviour
    {
        public float Width { get; private set; }
        public float Height { get; private set; }
        public bool Movable = true;
        public bool Resizable = true;
        public bool Closable = true;
        public bool Billboard = true;
        public Action OnClosed;
        public Transform Head;

        private Transform _plate;
        private MeshFilter _plateMesh;
        private Material _plateMat;
        private WintryText _title, _body;
        private readonly List<WintryButton> _buttons = new List<WintryButton>();
        private WintryButton _close, _bigger, _smaller;
        private BoxCollider _dragHandle;
        private float _scale = 1f;
        private float _fade = 0f;
        private bool _closing;
        private Color _tint = new Color(0.08f, 0.12f, 0.2f);
        private bool _contrastApplied;

        public static GlassPanel Create(string name, float width, float height, Transform head, Color? tint = null)
        {
            var go = new GameObject(name);
            var p = go.AddComponent<GlassPanel>();
            p.Head = head;
            if (tint.HasValue) p._tint = tint.Value;
            p.Build(width, height);
            return p;
        }

        private void Build(float width, float height)
        {
            Width = width; Height = height;
            var plate = new GameObject("Plate");
            plate.transform.SetParent(transform, false);
            _plateMesh = plate.AddComponent<MeshFilter>();
            const float corner = 0.032f;    // generous, continuous corners: the panel should read as carved
            _plateMesh.sharedMesh = ProceduralMeshes.RoundedRect(width, height, corner, 12);
            var mr = plate.AddComponent<MeshRenderer>();
            _plateMat = WintryMaterials.Glass(_tint, 0.62f, 1.4f);
            WintryMaterials.SetPanelShape(_plateMat, width, height, corner, 0.0016f);
            mr.sharedMaterial = _plateMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _plate = plate.transform;

            // drag handle over the title bar
            _dragHandle = gameObject.AddComponent<BoxCollider>();
            _dragHandle.isTrigger = true;
            _dragHandle.center = new Vector3(0f, height * 0.5f - 0.02f, 0f);
            _dragHandle.size = new Vector3(width - 0.12f, 0.04f, 0.01f);

            float pad = 0.022f;
            _title = WintryText.Create("Title", transform, new Vector3(-width * 0.5f + pad, height * 0.5f - pad, -0.003f),
                                       TitleEm, TextRole.Title, TextAnchor.UpperLeft, new Color(0.94f, 0.97f, 1f));
            _title.SetArea(new Vector2(width - pad * 2f - 0.03f, TitleEm * 1.4f));
            _body = WintryText.Create("Body", transform, new Vector3(-width * 0.5f + pad, height * 0.5f - pad - TitleEm * 1.9f, -0.003f),
                                      BodyEm, TextRole.Body, TextAnchor.UpperLeft, new Color(0.80f, 0.86f, 0.94f));
            _body.SetArea(BodyArea());

            float bs = 0.026f;
            if (Closable) _close = WintryButton.Create("×", transform, new Vector3(width * 0.5f - 0.02f, height * 0.5f - 0.02f, -0.003f), bs, bs, Close, Head, new Color(0.5f, 0.25f, 0.3f));
            if (Resizable)
            {
                _bigger = WintryButton.Create("+", transform, new Vector3(width * 0.5f - 0.02f, -height * 0.5f + 0.02f, -0.003f), bs, bs, () => SetScale(_scale * 1.15f), Head);
                _smaller = WintryButton.Create("−", transform, new Vector3(width * 0.5f - 0.05f, -height * 0.5f + 0.02f, -0.003f), bs, bs, () => SetScale(_scale / 1.15f), Head);
            }
            transform.localScale = Vector3.zero;
        }

        /// <summary>Em height of the title and the body, in metres. One place, so panels stay a family.</summary>
        public const float TitleEm = 0.0132f;
        public const float BodyEm = 0.0096f;

        /// <summary>The box the body text has to live in: below the title, above the button row.</summary>
        private Vector2 BodyArea()
        {
            float bottom = _buttons.Count > 0 ? 0.052f : 0.024f;
            return new Vector2(Width - 0.044f - 0.03f, Mathf.Max(BodyEm, Height - 0.022f - TitleEm * 1.9f - bottom));
        }

        public void SetTitle(string text) { _title.SetText(text ?? ""); }
        public void SetBody(string text)
        {
            _body.SetArea(BodyArea());
            _body.SetText(text ?? "");
        }

        public void SetBodyColor(Color c) { _body.SetColor(c); }

        public WintryButton AddButton(string label, Action onClick, float width = 0.1f)
        {
            float bh = 0.032f;
            float x = -Width * 0.5f + 0.022f + width * 0.5f + _buttons.Count * (width + 0.012f);
            var b = WintryButton.Create(label, transform, new Vector3(x, -Height * 0.5f + 0.028f, -0.004f), width, bh, onClick, Head);
            _buttons.Add(b);
            if (_buttons.Count == 1) _body.SetArea(BodyArea());   // the row just took height off the body box
            return b;
        }

        public void ClearButtons()
        {
            foreach (var b in _buttons) if (b != null) Destroy(b.gameObject);
            _buttons.Clear();
        }

        public void SetScale(float s)
        {
            _scale = Mathf.Clamp(s, 0.5f, 2.2f);
        }

        public void Close()
        {
            if (_closing) return;
            _closing = true;
        }

        public void Move(Vector3 worldPos) { transform.position = worldPos; }

        private void Update()
        {
            float target = _closing ? 0f : 1f;
            _fade = Mathf.MoveTowards(_fade, target, Time.deltaTime * (WintrySettings.Current.Accessibility.ReducedMotion ? 10f : 5f));
            float ui = WintrySettings.Current.MR.UISize;
            transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, _fade) * _scale * ui;
            if (_closing && _fade <= 0f) { OnClosed?.Invoke(); Destroy(gameObject); return; }
            if (Billboard)
            {
                var head = Head != null ? Head : (UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform : null);
                if (head != null)
                {
                    Vector3 to = transform.position - head.position;
                    if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime));
                }
            }
            float textScale = WintrySettings.Current.Accessibility.TextSize;
            if (_body != null && Mathf.Abs(_body.transform.localScale.x - textScale) > 0.01f)
            {
                _body.transform.localScale = Vector3.one * textScale;
                _title.transform.localScale = Vector3.one * textScale;
                _body.SetArea(BodyArea() / Mathf.Max(0.2f, textScale));   // a bigger face fits a smaller box
            }
            // High contrast swaps the translucent tint for a near-opaque plate. It has to be applied on the
            // way back out too, otherwise turning the setting off leaves the panel permanently blacked out.
            bool contrast = WintrySettings.Current.Accessibility.HighContrast;
            if (contrast != _contrastApplied)
            {
                _contrastApplied = contrast;
                WintryMaterials.SetColor(_plateMat, contrast ? new Color(0.02f, 0.03f, 0.05f, 0.92f)
                                                             : new Color(_tint.r, _tint.g, _tint.b, 0.62f));
                _title.SetColor(contrast ? Color.white : new Color(0.94f, 0.97f, 1f));
                _body.SetColor(contrast ? Color.white : new Color(0.80f, 0.86f, 0.94f));
            }
        }

        public static string Wrap(string text, int maxChars)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var paragraph in text.Split('\n'))
            {
                int len = 0;
                foreach (var word in paragraph.Split(' '))
                {
                    if (len + word.Length + 1 > maxChars && len > 0) { sb.Append('\n'); len = 0; }
                    if (len > 0) { sb.Append(' '); len++; }
                    sb.Append(word); len += word.Length;
                }
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }
    }
}
