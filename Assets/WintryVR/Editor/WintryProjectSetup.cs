using System;
using System.IO;
using System.Reflection;
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

        /// <summary>
        /// Imports TextMeshPro's essential resources, which carry the global TMP settings asset.
        /// </summary>
        /// <remarks>
        /// TextMeshPro reads that asset while building a font atlas, and without it
        /// <c>TMP_FontAsset.CreateFontAsset</c> throws from inside TMP rather than returning null. WintryText
        /// catches that and drops to bitmap text, so the app still runs, but every label loses its sharpness.
        /// Running this once fixes it for the project, and the result belongs in source control.
        /// </remarks>
        public const string PipelineAssetPath = "Assets/WintryVR/Rendering/WintryVR_URP.asset";
        public const string RendererAssetPath = "Assets/WintryVR/Rendering/WintryVR_Renderer.asset";

        /// <summary>
        /// Creates a Quest-tuned URP asset and assigns it, if the project has none.
        /// </summary>
        /// <remarks>
        /// Wintry's glass and glow shaders are URP-only, and <c>WintryMaterials.FindShader</c> checks whether a
        /// pipeline asset is actually assigned rather than whether the package is installed — so with no asset
        /// the UI quietly falls back to built-in unlit and loses the bevel, the refraction and the glow. The
        /// opaque texture is switched on because that is what the glass refracts; the rest is chosen for a
        /// standalone headset: no HDR, no shadow cascades, MSAA in place of post-processing.
        /// URP is reached by reflection so this file still compiles in a project without it.
        /// </remarks>
        [MenuItem("WintryVR/Setup/Create and assign URP asset")]
        public static void CreateRenderPipelineAsset()
        {
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                Debug.Log("[WintryVR] A render pipeline asset is already assigned: " + GraphicsSettings.defaultRenderPipeline.name);
                return;
            }
            const string asm = "Unity.RenderPipelines.Universal.Runtime";
            var dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRendererData, " + asm);
            var assetType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, " + asm);
            if (dataType == null || assetType == null)
            {
                Debug.LogWarning("[WintryVR] URP is not installed, so no pipeline asset can be created. " +
                                 "The UI will render with built-in unlit shaders.");
                return;
            }

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "WintryVR/Rendering"));
            var rendererData = ScriptableObject.CreateInstance(dataType);
            AssetDatabase.CreateAsset(rendererData, RendererAssetPath);

            var create = assetType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            if (create == null) { Debug.LogWarning("[WintryVR] URP changed its asset factory; create the asset by hand."); return; }
            var pipeline = create.Invoke(null, new object[] { rendererData }) as RenderPipelineAsset;
            if (pipeline == null) { Debug.LogWarning("[WintryVR] URP would not create a pipeline asset."); return; }
            AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);

            SetIfPresent(assetType, pipeline, "supportsCameraOpaqueTexture", true);   // the glass refracts this
            SetIfPresent(assetType, pipeline, "supportsCameraDepthTexture", false);
            SetIfPresent(assetType, pipeline, "supportsHDR", false);                  // no HDR on a mobile GPU here
            SetIfPresent(assetType, pipeline, "shadowCascadeCount", 1);
            SetIfPresent(assetType, pipeline, "msaaSampleCount", 4);
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log("[WintryVR] Created and assigned " + PipelineAssetPath);
        }

        private static void SetIfPresent(Type t, object target, string property, object value)
        {
            var p = t.GetProperty(property);
            if (p != null && p.CanWrite) { try { p.SetValue(target, value, null); } catch { } }
        }

        [MenuItem("WintryVR/Setup/Import TextMeshPro essentials")]
        public static void ImportTmpEssentials()
        {
            if (Directory.Exists(Path.Combine(Application.dataPath, "TextMesh Pro")))
            {
                Debug.Log("[WintryVR] TextMeshPro essentials are already in the project.");
                return;
            }
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath));
            string found = null;
            foreach (var dir in new[] { Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library/PackageCache") })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories))
                { found = f; break; }
            }
            if (found == null)
            {
                Debug.LogWarning("[WintryVR] Could not find TMP Essential Resources.unitypackage. " +
                                 "Import it from Window → TextMeshPro → Import TMP Essential Resources.");
                return;
            }
            AssetDatabase.ImportPackage(found, false);
            Debug.Log("[WintryVR] Imported TextMeshPro essentials from " + found);
        }

        [MenuItem("WintryVR/Setup/Open Documentation")]
        public static void OpenDocs()
        {
            string readme = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "README.md");
            if (File.Exists(readme)) EditorUtility.OpenWithDefaultApp(readme);
        }
    }
}
