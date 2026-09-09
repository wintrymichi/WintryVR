using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace WintryVR.EditorTools
{
    /// <summary>
    /// One-click Quest configuration: Android target, ARM64 + IL2CPP, Vulkan, linear colour, min SDK 32,
    /// ASTC textures, multithreaded rendering, package name and product name. XR Plug-in Management loaders
    /// (OpenXR / Oculus) still need to be ticked in Project Settings → XR Plug-in Management (see docs).
    /// </summary>
    public static class WintryProjectSetup
    {
        public const string PackageName = "com.wintry.wintryvr";

        [MenuItem("WintryVR/Setup/Configure Player Settings for Quest 3 & 3S")]
        public static void ConfigureForQuest()
        {
            PlayerSettings.productName = "WintryVR";
            PlayerSettings.companyName = "Wintry";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, true);
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.Disable;
            Debug.Log("[WintryVR] Player settings configured for Meta Quest 3 / 3S. Now enable the OpenXR (or Oculus) loader under Project Settings → XR Plug-in Management → Android, and set the Meta XR feature group.");
            CreateStreamingAssetsConfigIfMissing();
        }

        [MenuItem("WintryVR/Setup/Create wintry.config.json from example")]
        public static void CreateStreamingAssetsConfigIfMissing()
        {
            string dir = Path.Combine(Application.dataPath, "StreamingAssets");
            Directory.CreateDirectory(dir);
            string cfg = Path.Combine(dir, "wintry.config.json");
            string example = Path.Combine(dir, "wintry.config.example.json");
            if (!File.Exists(cfg) && File.Exists(example)) { File.Copy(example, cfg); AssetDatabase.Refresh(); Debug.Log("[WintryVR] Created StreamingAssets/wintry.config.json (git-ignored). Edit endpoints/providers there; keys go in wintry.secrets.json or env vars."); }
        }

        [MenuItem("WintryVR/Setup/Open Documentation")]
        public static void OpenDocs()
        {
            string readme = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "README.md");
            if (File.Exists(readme)) EditorUtility.OpenWithDefaultApp(readme);
        }
    }
}
