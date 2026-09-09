using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WintryVR.Core;

namespace WintryVR.EditorTools
{
    /// <summary>
    /// Creates (or repairs) the main MR scene: a single GameObject with <see cref="WintryBootstrap"/>.
    /// Everything else (rig, passthrough, Wintry, UI) is built at runtime from the detected capabilities,
    /// so the scene stays trivial and version-control friendly.
    /// </summary>
    public static class WintrySceneBuilder
    {
        public const string ScenePath = "Assets/WintryVR/Scenes/WintryVR_Main.unity";

        [MenuItem("WintryVR/Setup/Create or Repair Main Scene")]
        public static void CreateMainScene()
        {
            Scene scene;
            if (File.Exists(ScenePath)) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var existing = Object.FindFirstObjectByType<WintryBootstrap>();
            if (existing == null)
            {
                var go = new GameObject("WintryVR");
                go.AddComponent<WintryBootstrap>();
                Undo.RegisterCreatedObjectUndo(go, "Create WintryVR");
            }
            // ambient light suited to passthrough (neutral, slightly cool)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.64f);
            RenderSettings.fog = false;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log("[WintryVR] Main scene ready at " + ScenePath);
        }

        private static void AddToBuildSettings()
        {
            foreach (var s in EditorBuildSettings.scenes) if (s.path == ScenePath) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes) { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
