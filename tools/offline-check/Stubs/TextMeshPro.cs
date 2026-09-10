// Stand-in for the TextMeshPro API that WintryVR.UI.WintryText uses, with TMP's real signatures.
//
// Only editor.csproj defines WINTRY_TMP, so this file covers the path that ships. android.csproj leaves the
// symbol undefined on purpose, which compiles the TextMesh fallback instead — between the two configurations
// both branches of WintryText are built. Nothing here rasterises a glyph: it checks that the calls exist and
// type-check, and what the text actually looks like comes from a render (tools/preview).
using UnityEngine;

namespace UnityEngine.TextCore.LowLevel
{
    public enum GlyphRenderMode
    {
        SMOOTH_HINTED = 0, SMOOTH = 1, RASTER_HINTED = 2, RASTER = 3,
        SDF = 4, SDF8 = 5, SDF16 = 6, SDF32 = 7, SDFAA_HINTED = 8, SDFAA = 9
    }
}

namespace TMPro
{
    public enum AtlasPopulationMode { Static = 0, Dynamic = 1, DynamicOS = 2 }

    public enum TextOverflowModes
    {
        Overflow = 0, Ellipsis = 1, Masking = 2, Truncate = 3,
        ScrollRect = 4, Page = 5, Linked = 6
    }

    public enum TextWrappingModes { NoWrap = 0, Normal = 1, PreserveWhitespace = 2, PreserveWhitespaceNoWrap = 3 }

    public enum TextAlignmentOptions
    {
        TopLeft, Top, TopRight, TopJustified, TopFlush, TopGeoAligned,
        Left, Center, Right, Justified, Flush, CenterGeoAligned,
        BottomLeft, Bottom, BottomRight, BottomJustified, BottomFlush, BottomGeoAligned,
        BaselineLeft, Baseline, BaselineRight, BaselineJustified, BaselineFlush, BaselineGeoAligned,
        MidlineLeft, Midline, MidlineRight, MidlineJustified, MidlineFlush, MidlineGeoAligned,
        CaplineLeft, Capline, CaplineRight, CaplineJustified, CaplineFlush, CaplineGeoAligned,
        Converted
    }

    public class TMP_FontAsset : ScriptableObject
    {
        public static TMP_FontAsset CreateFontAsset(Font font) { return null; }

        public static TMP_FontAsset CreateFontAsset(Font font, int samplingPointSize, int atlasPadding,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode renderMode, int atlasWidth, int atlasHeight,
            AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic,
            bool enableMultiAtlasSupport = true)
        {
            return null;
        }
    }

    public abstract class TMP_Text : MonoBehaviour
    {
        public string text { get; set; }
        public float fontSize { get; set; }
        public Color color { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public float characterSpacing { get; set; }
        public float lineSpacing { get; set; }
        public bool enableWordWrapping { get; set; }
        public TextWrappingModes textWrappingMode { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public bool raycastTarget { get; set; }
        public TMP_FontAsset font { get; set; }
        public Bounds textBounds { get { return default(Bounds); } }
        public RectTransform rectTransform { get { return null; } }
        public void ForceMeshUpdate() { }
        public void ForceMeshUpdate(bool ignoreActiveState) { }
    }

    public class TextMeshPro : TMP_Text { }
}
