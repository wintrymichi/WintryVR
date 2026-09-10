// Additional faithful-signature UnityEditor stubs (round 2).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    public partial class EditorGUIExtras { }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; }
    }

    public sealed class EditorBuildSettingsScene
    {
        public EditorBuildSettingsScene() { }
        public EditorBuildSettingsScene(string path, bool enabled) { }
        public string path { get; set; }
        public bool enabled { get; set; }
        public GUID guid { get; set; }
    }

    public struct GUID { public override string ToString() { return ""; } }

    public struct NamedBuildTarget
    {
        public static NamedBuildTarget Standalone { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget Android { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget iOS { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget WebGL { get { return default(NamedBuildTarget); } }
        public static NamedBuildTarget FromBuildTargetGroup(BuildTargetGroup buildTargetGroup) { return default(NamedBuildTarget); }
        public string TargetName { get { return ""; } }
    }

    public enum ScriptingImplementation { Mono2x = 0, IL2CPP = 1, WinRTDotNET = 2 }
    public enum ManagedStrippingLevel { Disabled = 0, Low = 1, Medium = 2, High = 3, Minimal = 4 }
    public enum UIOrientation { Portrait = 0, PortraitUpsideDown = 1, LandscapeRight = 2, LandscapeLeft = 3, AutoRotation = 4 }
    public enum MobileTextureSubtarget { Generic = 0, DXT = 1, ETC = 3, ETC2 = 5, ASTC = 6 }
    public enum AndroidBuildType { Debug, Development, Release }
    public enum TextureImporterType { Default = 0, NormalMap = 1, GUI = 2, Sprite = 8, Cursor = 7, Cookie = 4, Lightmap = 6, SingleChannel = 10 }
    public enum TextureImporterCompression { Uncompressed = 0, Compressed = 1, CompressedHQ = 2, CompressedLQ = 3 }
    public enum TextureImporterShape { Texture2D = 1, TextureCube = 2 }
    public enum TextureImporterAlphaSource { None = 0, FromInput = 1, FromGrayScale = 2 }
    public enum AndroidPreferredInstallLocation { Auto, PreferExternal, ForceInternal }
}

namespace UnityEditor
{
    public static class QualitySettingsHelperMarker { }
}

namespace UnityEditor
{
    public struct EditorGUIDisabledScopeMarker { }
}
