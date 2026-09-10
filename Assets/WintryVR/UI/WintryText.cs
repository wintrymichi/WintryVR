using UnityEngine;
using WintryVR.Core;
#if WINTRY_TMP
using TMPro;
#endif

namespace WintryVR.UI
{
    /// <summary>How a piece of text is used, which decides its weight and tracking.</summary>
    public enum TextRole
    {
        /// <summary>Panel titles and headings: Inter Display SemiBold, tightened.</summary>
        Title,
        /// <summary>Running copy: Inter Regular.</summary>
        Body,
        /// <summary>Buttons, chips and status: Inter Medium, opened up slightly.</summary>
        Ui
    }

    /// <summary>
    /// Every piece of world-space text in WintryVR goes through here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Typography is most of what separates an interface that looks designed from one that looks generated, and
    /// two things gave this one away: the typeface and the rasteriser. It drew with Unity's built-in Arial
    /// through <c>TextMesh</c> — a bitmap atlas rendered at one size and then magnified, which at reading
    /// distance in a headset crawls along the glyph edges as the head moves. It now draws Inter through
    /// TextMeshPro's signed-distance-field pipeline, which stays sharp at any size and angle because a glyph is
    /// stored as distances rather than pixels.
    /// </para>
    /// <para>
    /// Inter is a deliberate choice: a UI typeface with a tall x-height and open apertures, which is what keeps
    /// short labels legible at low angular resolution, and it is under the Open Font License so it can ship in
    /// the repository. Apple's SF Pro cannot be redistributed, so it was never an option. Titles take Inter
    /// Display — the optical size drawn for large text — with tracking pulled in the way display type wants,
    /// while small UI text gets tracking pushed out. That is the same direction optical sizes move in Apple's
    /// own type, and it is why a title and a button label here do not look like the same font scaled.
    /// </para>
    /// <para>
    /// The SDF atlas is built from the TTF at runtime, so nothing but the font itself has to be committed.
    /// If TextMeshPro is missing, or its settings asset has not been imported, this falls back to
    /// <c>TextMesh</c> rather than throwing: a font problem should cost sharpness, never the whole interface.
    /// </para>
    /// </remarks>
    public class WintryText : MonoBehaviour
    {
        private const int AtlasPointSize = 64;
        private const int AtlasSize = 1024;

        /// <summary>Metres of em height per unit of characterSize x fontSize, measured from a render.</summary>
        private const float EmPerSizeUnit = 0.048f;

        private static Font _regular, _medium, _display;

        private TextRole _role;
        private float _em = 0.012f;
        private Vector2 _area;              // zero = unbounded
        private string _raw = "";
        private TextMesh _legacy;           // used when SDF text is unavailable

        public string Text => _raw;

        // ------------------------------------------------------------------ fonts

        private static Font Load(ref Font slot, string file, params string[] fallbacks)
        {
            if (slot != null) return slot;
            slot = Resources.Load<Font>("Fonts/" + file);
            if (slot == null)
                foreach (var f in fallbacks) { slot = Resources.Load<Font>("Fonts/" + f); if (slot != null) break; }
            if (slot == null) slot = WorldLabel.DefaultFont;   // last resort, keeps text on screen
            return slot;
        }

        public static Font RegularFont => Load(ref _regular, "Inter-Regular");
        public static Font MediumFont => Load(ref _medium, "Inter-Medium", "Inter-Regular");
        public static Font DisplayFont => Load(ref _display, "InterDisplay-SemiBold", "Inter-Medium", "Inter-Regular");

        private static Font FontFor(TextRole role)
        {
            switch (role)
            {
                case TextRole.Title: return DisplayFont;
                case TextRole.Ui: return MediumFont;
                default: return RegularFont;
            }
        }

        /// <summary>
        /// Letter spacing per role, in TextMeshPro's character-spacing units — roughly a hundredth of an em
        /// each, so single digits are the whole usable range. Display type is tightened and small UI type is
        /// opened up, which is the direction optical sizes move in. Overdo it and the spaces between words go
        /// first: at -18 a title set solid, reading CanonEOSR6MarkII.
        /// </summary>
        private static float TrackingFor(TextRole role)
        {
            switch (role)
            {
                case TextRole.Title: return -2f;
                case TextRole.Ui: return 1.5f;
                default: return 0f;
            }
        }

        public static WintryText Create(string name, Transform parent, Vector3 localPos, float emMetres,
                                        TextRole role, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<WintryText>();
            t._role = role;
            t._em = emMetres;
            t.Init(anchor, color);
            return t;
        }

        // ------------------------------------------------------------------ construction

        private void Init(TextAnchor anchor, Color color)
        {
#if WINTRY_TMP
            if (TryInitSdf(anchor, color)) return;
#endif
            InitLegacy(anchor, color);
        }

        private void InitLegacy(TextAnchor anchor, Color color)
        {
            _legacy = gameObject.AddComponent<TextMesh>();
            WorldLabel.Configure(_legacy, _em / EmPerSizeUnit, anchor, Alignment(anchor), color);
            var font = FontFor(_role);
            if (font != null) _legacy.font = font;
            var mr = GetComponent<MeshRenderer>();
            if (mr != null && _legacy.font != null) mr.sharedMaterial = _legacy.font.material;
            _legacy.lineSpacing = 1.0f;
        }

        private static TextAlignment Alignment(TextAnchor a)
        {
            if (a == TextAnchor.UpperLeft || a == TextAnchor.MiddleLeft || a == TextAnchor.LowerLeft) return TextAlignment.Left;
            if (a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight) return TextAlignment.Right;
            return TextAlignment.Center;
        }

        // ------------------------------------------------------------------ public surface

        public void SetText(string text)
        {
            _raw = text ?? "";
#if WINTRY_TMP
            if (_tmp != null) { _tmp.text = _raw; _tmp.ForceMeshUpdate(); return; }
#endif
            _legacy.text = _area.x > 0f ? Reflow(_raw) : _raw;
        }

        public void SetColor(Color c)
        {
#if WINTRY_TMP
            if (_tmp != null) { _tmp.color = c; return; }
#endif
            _legacy.color = c;
        }

        public void SetSize(float emMetres)
        {
            _em = emMetres;
#if WINTRY_TMP
            if (_tmp != null) { _tmp.fontSize = _em * MetreToPoint; _tmp.ForceMeshUpdate(); return; }
#endif
            _legacy.characterSize = (_em / EmPerSizeUnit) / Mathf.Max(1, _legacy.fontSize);
            if (_area.x > 0f) _legacy.text = Reflow(_raw);
        }

        /// <summary>
        /// Constrains the text to a box: it wraps inside it and ellipsises what will not fit. Passing
        /// <see cref="Vector2.zero"/> lets it run free.
        /// </summary>
        public void SetArea(Vector2 metres)
        {
            _area = metres;
            bool bounded = metres.x > 0f && metres.y > 0f;
#if WINTRY_TMP
            if (_tmp != null)
            {
                _tmp.enableWordWrapping = bounded;
                _tmp.overflowMode = bounded ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
                _tmp.rectTransform.sizeDelta = bounded ? metres : new Vector2(10f, 10f);
                _tmp.ForceMeshUpdate();
                return;
            }
#endif
            _legacy.text = bounded ? Reflow(_raw) : _raw;
        }

        /// <summary>Rendered size in local metres.</summary>
        public Vector2 RenderedSize
        {
            get
            {
#if WINTRY_TMP
                if (_tmp != null)
                {
                    _tmp.ForceMeshUpdate();
                    var tb = _tmp.textBounds;
                    return new Vector2(tb.size.x, tb.size.y);
                }
#endif
                var r = GetComponent<Renderer>();
                if (r == null) return Vector2.zero;
                float s = Mathf.Abs(transform.lossyScale.x) > 1e-5f ? Mathf.Abs(transform.lossyScale.x) : 1f;
                var b = r.bounds.size;
                return new Vector2(b.x / s, b.y / s);
            }
        }

        /// <summary>
        /// Whether <see cref="RenderedSize"/> describes the current string. SDF text is measured on demand and
        /// is always ready; a TextMesh only rebuilds at the end of the frame its string was set in, so callers
        /// that lay out from the size have to wait for it.
        /// </summary>
        public bool Measured
        {
            get
            {
#if WINTRY_TMP
                if (_tmp != null) return true;
#endif
                var r = GetComponent<Renderer>();
                return r != null && (r.bounds.size.x > 0f || string.IsNullOrEmpty(_raw));
            }
        }

        /// <summary>Word wrap and line clamp for the fallback, which has neither built in.</summary>
        private string Reflow(string text)
        {
            float glyph = _em * 0.55f;                       // Inter's average advance is a little over half an em
            int cols = Mathf.Max(8, Mathf.FloorToInt(_area.x / Mathf.Max(1e-5f, glyph)));
            string wrapped = GlassPanel.Wrap(text ?? "", cols);
            int rows = Mathf.Max(1, Mathf.FloorToInt(_area.y / Mathf.Max(1e-5f, _em * 1.32f)));
            var lines = wrapped.Split('\n');
            if (lines.Length <= rows) return wrapped;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(i == rows - 1 ? lines[i].TrimEnd() + "…" : lines[i]);
            }
            return sb.ToString();
        }

#if WINTRY_TMP
        // ------------------------------------------------------------------ TextMeshPro

        /// <summary>
        /// TextMeshPro sizes world-space text in points, ten to one unit of the transform, so an em of
        /// <c>fontSize / 10</c> comes out in metres.
        /// </summary>
        /// <remarks>
        /// The conversion applies to <c>fontSize</c> and to nothing else. Its RectTransform and its reported
        /// bounds are already in the transform's own units, and scaling those by ten as well made every wrap box
        /// ten metres wide — so text never wrapped — while every measurement came back a fifth of its real size,
        /// which is what left label backings too small for the words sitting on them.
        /// </remarks>
        private const float MetreToPoint = 10f;

        private static TMP_FontAsset _regularSdf, _mediumSdf, _displaySdf;
        private static bool _sdfUnavailable;
        private TextMeshPro _tmp;

        /// <summary>
        /// Builds an SDF font asset from a TTF, once per weight.
        /// </summary>
        /// <remarks>
        /// TextMeshPro reads its global settings asset while doing this, and that asset only exists once the
        /// TMP essential resources have been imported into the project. On a project without them the call
        /// throws a NullReferenceException from inside TMP rather than returning null, which took the whole UI
        /// down at construction. Catching it here turns a missing import into plain text instead of a dead app,
        /// and WintryVR -> Setup -> Verify project setup reports it so it gets fixed rather than lived with.
        /// </remarks>
        private static TMP_FontAsset Sdf(ref TMP_FontAsset slot, Font source)
        {
            if (slot != null) return slot;
            if (_sdfUnavailable || source == null) return null;
            try
            {
                slot = TMP_FontAsset.CreateFontAsset(source, AtlasPointSize, 8,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
            }
            catch (System.Exception ex)
            {
                _sdfUnavailable = true;
                WintryLog.W("UI", "TextMeshPro is present but unusable (" + ex.GetType().Name +
                                  "); falling back to bitmap text. Import the TMP essential resources to get sharp text.");
                return null;
            }
            if (slot == null) { _sdfUnavailable = true; return null; }
            slot.name = source.name + " SDF";
            return slot;
        }

        private static TMP_FontAsset SdfFor(TextRole role)
        {
            switch (role)
            {
                case TextRole.Title: return Sdf(ref _displaySdf, DisplayFont);
                case TextRole.Ui: return Sdf(ref _mediumSdf, MediumFont);
                default: return Sdf(ref _regularSdf, RegularFont);
            }
        }

        private bool TryInitSdf(TextAnchor anchor, Color color)
        {
            var asset = SdfFor(_role);
            if (asset == null) return false;

            _tmp = gameObject.AddComponent<TextMeshPro>();
            _tmp.font = asset;
            _tmp.fontSize = _em * MetreToPoint;
            _tmp.color = color;
            _tmp.alignment = Align(anchor);
            _tmp.characterSpacing = TrackingFor(_role);
            _tmp.lineSpacing = -6f;                     // Inter's default leading is loose for spatial panels
            _tmp.enableWordWrapping = false;
            _tmp.overflowMode = TextOverflowModes.Overflow;
            _tmp.raycastTarget = false;
            var rt = _tmp.rectTransform;
            if (rt != null) { rt.pivot = Pivot(anchor); rt.sizeDelta = new Vector2(10f, 10f); }
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) { mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; }
            return true;
        }

        private static TextAlignmentOptions Align(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static Vector2 Pivot(TextAnchor a)
        {
            float x = (a == TextAnchor.UpperLeft || a == TextAnchor.MiddleLeft || a == TextAnchor.LowerLeft) ? 0f
                    : (a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight) ? 1f : 0.5f;
            float y = (a == TextAnchor.UpperLeft || a == TextAnchor.UpperCenter || a == TextAnchor.UpperRight) ? 1f
                    : (a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight) ? 0f : 0.5f;
            return new Vector2(x, y);
        }

        /// <summary>True when sharp SDF text is actually in use, for the setup verifier to report.</summary>
        public static bool SdfAvailable => !_sdfUnavailable;
#else
        public static bool SdfAvailable => false;
#endif
    }
}
