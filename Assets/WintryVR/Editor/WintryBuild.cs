using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WintryVR.EditorTools
{
    /// <summary>
    /// Builds the Quest APK, doing the whole setup first so a fresh clone can produce a package in one command.
    /// </summary>
    /// <remarks>
    /// Everything here is also reachable from the WintryVR menu, but a build has to be repeatable from a
    /// terminal — for CI, and so that "it builds on my machine" is checkable. It runs the same setup steps in
    /// the order they depend on each other, then builds, and refuses rather than shipping something broken if
    /// a step that matters did not take.
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///           -executeMethod WintryVR.EditorTools.WintryBuild.BuildQuestApk \
    ///           -logFile build.log -buildOutput Build/WintryVR.apk
    /// </code>
    /// </remarks>
    public static class WintryBuild
    {
        private const string ScenePath = "Assets/WintryVR/Scenes/WintryVR_Main.unity";
        private const string DefaultOutput = "Build/WintryVR.apk";

        [MenuItem("WintryVR/Build/Build Quest APK", false, 40)]
        public static void BuildQuestApkMenu() { Build(DefaultOutput, false); }

        [MenuItem("WintryVR/Build/Build Quest APK (development)", false, 41)]
        public static void BuildQuestApkDevMenu() { Build(DefaultOutput.Replace(".apk", "-dev.apk"), true); }

        /// <summary>Command-line entry point. Reads -buildOutput and -development from the arguments.</summary>
        public static void BuildQuestApk()
        {
            string output = ArgValue("-buildOutput") ?? DefaultOutput;
            bool development = Environment.GetCommandLineArgs().Contains("-development");
            bool ok = Build(output, development);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        private static bool Build(string output, bool development)
        {
            try
            {
                // order matters: player settings switch the platform, the pipeline asset has to exist before
                // anything asks for a shader, and the loader can only be enabled once XR management is present
                WintryProjectSetup.ConfigureForQuest();
                WintryProjectSetup.CreateRenderPipelineAsset();
                WintryProjectSetup.ImportTmpEssentials();
                WintrySetupVerifier.EnableXrLoader();
                EnsureSceneInBuild();
                AssetDatabase.SaveAssets();

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = development ? (BuildOptions.Development | BuildOptions.AllowDebugging) : BuildOptions.None
                };

                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;

                // The OpenXR package registers its settings during a build and then aborts that same build with
                // "OpenXR Settings found in project but not yet loaded. Please build again." The second attempt
                // in the same session finds them loaded and goes through, which is why this retries once rather
                // than reporting a failure a human would just re-run by hand.
                if (summary.result != BuildResult.Succeeded)
                {
                    Debug.Log("[WintryVR] First build attempt returned " + summary.result + "; retrying once.");
                    report = BuildPipeline.BuildPlayer(options);
                    summary = report.summary;
                }

                if (summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError("[WintryVR] Build " + summary.result + " — see the errors above.");
                    return false;
                }
                Debug.Log(string.Format("[WintryVR] Built {0} ({1:N1} MB) for {2}",
                    summary.outputPath, summary.totalSize / (1024f * 1024f), summary.platform));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[WintryVR] Build failed: " + ex);
                return false;
            }
        }

        private static void EnsureSceneInBuild()
        {
            var scenes = EditorBuildSettings.scenes ?? new EditorBuildSettingsScene[0];
            if (scenes.Any(s => s != null && s.path == ScenePath && s.enabled)) return;
            var kept = scenes.Where(s => s != null && s.path != ScenePath).ToList();
            kept.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = kept.ToArray();
            Debug.Log("[WintryVR] Added " + ScenePath + " to the build settings.");
        }
    }
}
