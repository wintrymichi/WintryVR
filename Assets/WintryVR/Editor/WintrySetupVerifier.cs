using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace WintryVR.EditorTools
{
    /// <summary>
    /// Checks the project settings WintryVR depends on and reports what is still missing, then offers to fix
    /// the parts that can be fixed from a script. The XR loader and the render pipeline asset live in packages
    /// that may not be installed, so they are reached through reflection: when the package is absent the check
    /// degrades to an explanation instead of failing to compile.
    /// </summary>
    public static class WintrySetupVerifier
    {
        private const string ScenePath = "Assets/WintryVR/Scenes/WintryVR_Main.unity";
        private const string OpenXrLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string OculusLoader = "Unity.XR.Oculus.OculusLoader";

        private struct Check
        {
            public bool Ok;
            public string Label;
            public string Detail;
            public Check(bool ok, string label, string detail) { Ok = ok; Label = label; Detail = detail; }
        }

        [MenuItem("WintryVR/Setup/Verify project setup", false, 20)]
        public static void Verify()
        {
            var checks = Collect();
            var sb = new StringBuilder("[WintryVR] Setup check\n");
            foreach (var c in checks) sb.Append(c.Ok ? "  OK   " : "  TODO ").Append(c.Label).Append(" — ").AppendLine(c.Detail);
            int todo = checks.Count(c => !c.Ok);
            sb.Append(todo == 0 ? "Everything WintryVR needs is in place." : todo + " item(s) still need attention.");
            if (todo == 0) Debug.Log(sb.ToString()); else Debug.LogWarning(sb.ToString());
        }

        [MenuItem("WintryVR/Setup/Enable XR loader for Android", false, 21)]
        public static void EnableXrLoader()
        {
            string message;
            if (TryEnableLoader(out message)) Debug.Log("[WintryVR] " + message);
            else Debug.LogWarning("[WintryVR] " + message + "\nEnable it by hand in Project Settings → XR Plug-in Management → Android.");
        }

        private static List<Check> Collect()
        {
            var checks = new List<Check>
            {
                new Check(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android,
                    "Build target", "expected Android, found " + EditorUserBuildSettings.activeBuildTarget),
                new Check(PlayerSettings.colorSpace == ColorSpace.Linear,
                    "Colour space", "expected Linear, found " + PlayerSettings.colorSpace),
                new Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP,
                    "Scripting backend", "expected IL2CPP, found " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)),
                new Check(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64,
                    "Android architecture", "expected ARM64, found " + PlayerSettings.Android.targetArchitectures),
                new Check((int)PlayerSettings.Android.minSdkVersion >= 32,
                    "Minimum SDK", "expected 32 or higher, found " + (int)PlayerSettings.Android.minSdkVersion),
                new Check(FirstGraphicsApiIsVulkan(),
                    "Graphics API", "expected Vulkan first for Android"),
                new Check(File.Exists(Path.Combine(Application.dataPath, "StreamingAssets/wintry.config.json")),
                    "Runtime config", "StreamingAssets/wintry.config.json (run Setup → Create wintry.config.json)"),
                new Check(SceneIsInBuild(), "Main scene", ScenePath + " listed and enabled in Build Settings"),
                RenderPipelineCheck(),
                ShaderCheck(),
                XrLoaderCheck()
            };
            return checks;
        }

        private static bool FirstGraphicsApiIsVulkan()
        {
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            return apis != null && apis.Length > 0 && apis[0] == GraphicsDeviceType.Vulkan;
        }

        private static bool SceneIsInBuild()
        {
            var scenes = EditorBuildSettings.scenes;
            return scenes != null && scenes.Any(s => s != null && s.enabled && s.path == ScenePath);
        }

        /// <summary>
        /// Compiles Wintry's own shaders and reports what the compiler said. A broken shader does not stop a
        /// build or fail a test: it renders magenta on the headset and nowhere else, which is the most
        /// expensive place to find out. The offline harness cannot see this at all, since it never runs a
        /// shader compiler.
        /// </summary>
        private static Check ShaderCheck()
        {
            string[] names = { "WintryVR/Glass", "WintryVR/Glow" };
            var problems = new List<string>();
            var missing = new List<string>();
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null) { missing.Add(name); continue; }
                if (!ShaderUtil.ShaderHasError(shader)) continue;
                var messages = ShaderUtil.GetShaderMessages(shader);
                foreach (var m in messages)
                    problems.Add(name + ": " + m.message + (string.IsNullOrEmpty(m.file) ? "" : " (" + m.file + ":" + m.line + ")"));
                if (messages == null || messages.Length == 0) problems.Add(name + ": reports an error with no message");
            }
            if (missing.Count > 0)
                return new Check(false, "Shaders", "not found: " + string.Join(", ", missing) +
                    " — the UI falls back to built-in unlit shaders and looks flat");
            if (problems.Count > 0)
                return new Check(false, "Shaders", string.Join(" | ", problems));
            return new Check(true, "Shaders", string.Join(", ", names) + " compile clean");
        }

        private static Check RenderPipelineCheck()
        {
            var asset = GraphicsSettings.defaultRenderPipeline;
            if (asset == null) asset = QualitySettings.renderPipeline;
            if (asset == null)
                return new Check(false, "Render pipeline",
                    "no pipeline asset assigned; create a URP asset and set it in Project Settings → Graphics");
            return new Check(true, "Render pipeline", asset.name);
        }

        private static Check XrLoaderCheck()
        {
            string detail;
            bool active = TryDescribeActiveLoaders(out detail);
            return new Check(active, "XR loader", detail);
        }

        // --- XR Plug-in Management, reached by reflection so the package stays optional -------------------

        private static bool TryDescribeActiveLoaders(out string detail)
        {
            object settings;
            if (!TryGetAndroidXrSettings(out settings, out detail)) return false;

            var manager = GetProperty(settings, "Manager") ?? GetProperty(settings, "AssignedSettings");
            if (manager == null) { detail = "XR Plug-in Management is installed but has no manager for Android"; return false; }

            var loaders = GetProperty(manager, "activeLoaders") as System.Collections.IEnumerable
                       ?? GetProperty(manager, "loaders") as System.Collections.IEnumerable;
            var names = new List<string>();
            if (loaders != null)
                foreach (var l in loaders) if (l != null) names.Add(l.GetType().FullName);

            if (names.Count == 0) { detail = "no loader enabled for Android"; return false; }
            detail = "active: " + string.Join(", ", names);
            return true;
        }

        private static bool TryEnableLoader(out string message)
        {
            object settings;
            if (!TryGetAndroidXrSettings(out settings, out message)) return false;

            var manager = GetProperty(settings, "Manager") ?? GetProperty(settings, "AssignedSettings");
            if (manager == null) { message = "XR Plug-in Management has no manager object for Android"; return false; }

            var store = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
            if (store == null) { message = "XRPackageMetadataStore not found in this version of XR Plug-in Management"; return false; }

            var assign = store.GetMethod("AssignLoader", BindingFlags.Public | BindingFlags.Static);
            if (assign == null) { message = "XRPackageMetadataStore.AssignLoader not found"; return false; }

            // Oculus first on Android. The OpenXR loader also drives a Quest, but only once the Meta XR feature
            // group is ticked in the OpenXR settings, and a headless build has no way to tick it — so picking
            // OpenXR here produced an APK with a loader that initialises into nothing on the headset. The
            // Oculus provider needs no such companion setting and is what com.unity.xr.oculus exists for.
            var preferred = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
                ? new[] { OculusLoader, OpenXrLoader }
                : new[] { OpenXrLoader, OculusLoader };
            foreach (var loaderName in preferred)
            {
                if (FindType(loaderName) == null) continue;
                try
                {
                    var result = assign.Invoke(null, new object[] { manager, loaderName, BuildTargetGroup.Android });
                    if (result is bool && (bool)result)
                    {
                        EditorUtility.SetDirty(settings as UnityEngine.Object);
                        AssetDatabase.SaveAssets();
                        message = "Enabled " + loaderName + " for Android.";
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    message = "AssignLoader failed for " + loaderName + ": " + ex.Message;
                    return false;
                }
            }
            message = "Neither the OpenXR nor the Oculus loader is installed";
            return false;
        }

        private static bool TryGetAndroidXrSettings(out object settings, out string problem)
        {
            settings = null;
            var perTarget = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            if (perTarget == null) { problem = "XR Plug-in Management package is not installed"; return false; }

            object container = null;
            var tryGet = perTarget.GetMethod("TryGetSettingsForBuildTarget", BindingFlags.Public | BindingFlags.Static);
            var args = new object[] { BuildTargetGroup.Android, null };
            if (tryGet != null)
            {
                try
                {
                    var ok = tryGet.Invoke(null, args);
                    if (ok is bool && (bool)ok) container = args[1];
                }
                catch (Exception ex) { problem = "TryGetSettingsForBuildTarget failed: " + ex.Message; return false; }
            }

            if (container == null)
            {
                // The settings asset is normally created as a side effect of opening the XR Plug-in Management
                // page, which a batch build never does — so a headless build would find no loader and produce
                // an APK that runs flat on the headset. GetOrCreate is what that page itself calls.
                var getOrCreate = perTarget.GetMethod("GetOrCreate",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (getOrCreate == null)
                {
                    problem = "no XR settings object for Android yet; open Project Settings → XR Plug-in Management once";
                    return false;
                }
                try { container = getOrCreate.Invoke(null, null); }
                catch (Exception ex) { problem = "could not create the XR settings asset: " + ex.Message; return false; }
                if (container == null) { problem = "XR Plug-in Management would not create its settings asset"; return false; }
            }

            // container is the per-build-target asset; the caller wants the Android entry, which needs a
            // manager object of its own before a loader can be assigned to it
            var hasManager = container.GetType().GetMethod("HasManagerSettingsForBuildTarget");
            var createManager = container.GetType().GetMethod("CreateDefaultManagerSettingsForBuildTarget");
            var forTarget = container.GetType().GetMethod("SettingsForBuildTarget");
            if (hasManager != null && createManager != null)
            {
                try
                {
                    var has = hasManager.Invoke(container, new object[] { BuildTargetGroup.Android });
                    if (has is bool && !(bool)has)
                        createManager.Invoke(container, new object[] { BuildTargetGroup.Android });
                }
                catch (Exception ex) { problem = "could not create the Android XR manager: " + ex.Message; return false; }
            }

            if (forTarget != null)
            {
                try
                {
                    var android = forTarget.Invoke(container, new object[] { BuildTargetGroup.Android });
                    if (android != null) container = android;
                }
                catch { /* fall through with the container we have */ }
            }

            settings = container;
            problem = null;
            return true;
        }

        private static object GetProperty(object target, string name)
        {
            if (target == null) return null;
            var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null) { try { return p.GetValue(target, null); } catch { return null; } }
            var f = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) { try { return f.GetValue(target); } catch { return null; } }
            return null;
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = asm.GetType(fullName, false); } catch { }
                if (t != null) return t;
            }
            return null;
        }
    }
}
