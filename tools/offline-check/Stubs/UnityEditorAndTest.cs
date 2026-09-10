// Faithful-signature stubs of UnityEditor and NUnit APIs, for offline compile verification only.
#pragma warning disable 0067 // events here are declarations only; the stand-ins never raise them
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    public class EditorWindow : ScriptableObject
    {
        public string title { get; set; }
        public GUIContent titleContent { get; set; }
        public Vector2 minSize { get; set; }
        public Vector2 maxSize { get; set; }
        public Rect position { get; set; }
        public bool wantsMouseMove { get; set; }
        public void Show() { }
        public void ShowUtility() { }
        public void Close() { }
        public void Repaint() { }
        public void Focus() { }
        public static T GetWindow<T>() where T : EditorWindow { return null; }
        public static T GetWindow<T>(string title) where T : EditorWindow { return null; }
        public static T GetWindow<T>(bool utility) where T : EditorWindow { return null; }
        public static T GetWindow<T>(bool utility, string title) where T : EditorWindow { return null; }
        public static EditorWindow GetWindow(Type t) { return null; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    [AttributeUsage(AttributeTargets.Class)] public class InitializeOnLoadAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class InitializeOnLoadMethodAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class CustomEditor : Attribute { public CustomEditor(Type inspectedType) { } }

    public class Editor : ScriptableObject
    {
        public UnityEngine.Object target { get; set; }
        public virtual void OnInspectorGUI() { }
        public void DrawDefaultInspector() { }
    }

    public static class AssetDatabase
    {
        public static void ImportPackage(string packagePath, bool interactive) { }
        public static void Refresh() { }
        public static void SaveAssets() { }
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object { return null; }
        public static UnityEngine.Object LoadAssetAtPath(string assetPath, Type type) { return null; }
        public static UnityEngine.Object LoadMainAssetAtPath(string assetPath) { return null; }
        public static string[] FindAssets(string filter) { return null; }
        public static string[] FindAssets(string filter, string[] searchInFolders) { return null; }
        public static string GUIDToAssetPath(string guid) { return ""; }
        public static string AssetPathToGUID(string path) { return ""; }
        public static string GetAssetPath(UnityEngine.Object assetObject) { return ""; }
        public static bool DeleteAsset(string path) { return false; }
        public static bool CopyAsset(string path, string newPath) { return false; }
        public static string MoveAsset(string oldPath, string newPath) { return ""; }
        public static string CreateFolder(string parentFolder, string newFolderName) { return ""; }
        public static bool IsValidFolder(string path) { return false; }
        public static void ImportAsset(string path) { }
        public static string GenerateUniqueAssetPath(string path) { return ""; }
        public static void StartAssetEditing() { }
        public static void StopAssetEditing() { }
        public static void AddObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object assetObject) { }
    }

    public static class EditorUtility
    {
        public static void DisplayProgressBar(string title, string info, float progress) { }
        public static bool DisplayCancelableProgressBar(string title, string info, float progress) { return false; }
        public static void ClearProgressBar() { }
        public static bool DisplayDialog(string title, string message, string ok) { return false; }
        public static bool DisplayDialog(string title, string message, string ok, string cancel) { return false; }
        public static int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt) { return 0; }
        public static void SetDirty(UnityEngine.Object target) { }
        public static string SaveFilePanel(string title, string directory, string defaultName, string extension) { return ""; }
        public static string OpenFilePanel(string title, string directory, string extension) { return ""; }
        public static string OpenFolderPanel(string title, string folder, string defaultName) { return ""; }
        public static string SaveFilePanelInProject(string title, string defaultName, string extension, string message) { return ""; }
        public static void RevealInFinder(string path) { }
        public static void OpenWithDefaultApp(string fileName) { }
    }

    public static class EditorGUILayout
    {
        public static void LabelField(string label) { }
        public static void LabelField(string label, GUIStyle style) { }
        public static void LabelField(string label, string label2) { }
        public static void HelpBox(string message, MessageType type) { }
        public static void HelpBox(string message, MessageType type, bool wide) { }
        public static void Space() { }
        public static void Space(float width) { }
        public static string TextField(string label, string text) { return ""; }
        public static string TextField(string text) { return ""; }
        public static string TextArea(string text) { return ""; }
        public static int IntField(string label, int value) { return 0; }
        public static float FloatField(string label, float value) { return 0f; }
        public static float Slider(string label, float value, float leftValue, float rightValue) { return 0f; }
        public static bool Toggle(string label, bool value) { return false; }
        public static Enum EnumPopup(string label, Enum selected) { return null; }
        public static int Popup(string label, int selectedIndex, string[] displayedOptions) { return 0; }
        public static Color ColorField(string label, Color value) { return default(Color); }
        public static UnityEngine.Object ObjectField(string label, UnityEngine.Object obj, Type objType, bool allowSceneObjects) { return null; }
        public static void BeginHorizontal(params GUILayoutOption[] options) { }
        public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] options) { }
        public static void EndVertical() { }
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) { return default(Vector2); }
        public static void EndScrollView() { }
        public static bool Foldout(bool foldout, string content) { return false; }
        public static Vector3 Vector3Field(string label, Vector3 value) { return default(Vector3); }
        public static int IntPopup(string label, int selectedValue, string[] displayedOptions, int[] optionValues) { return 0; }
        public static int IntPopup(int selectedValue, string[] displayedOptions, int[] optionValues, params GUILayoutOption[] options) { return 0; }
        public static UnityEngine.Object ObjectField(UnityEngine.Object obj, Type objType, bool allowSceneObjects, params GUILayoutOption[] options) { return null; }
        public static void SelectableLabel(string text, params GUILayoutOption[] options) { }

        public class HorizontalScope : IDisposable
        {
            public HorizontalScope(params GUILayoutOption[] options) { }
            public void Dispose() { }
        }

        public class VerticalScope : IDisposable
        {
            public VerticalScope(params GUILayoutOption[] options) { }
            public void Dispose() { }
        }

        public class ScrollViewScope : IDisposable
        {
            public ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options) { }
            public Vector2 scrollPosition { get { return default(Vector2); } }
            public void Dispose() { }
        }
    }

    public enum MessageType { None, Info, Warning, Error }

    public static class EditorGUI
    {
        public static bool showMixedValue { get; set; }
        public static void BeginDisabledGroup(bool disabled) { }
        public static void EndDisabledGroup() { }
        public static void BeginChangeCheck() { }
        public static bool EndChangeCheck() { return false; }
        public static void LabelField(Rect position, string label) { }
        public static void DrawRect(Rect rect, Color color) { }
        public static int indentLevel { get; set; }

        public class DisabledScope : IDisposable
        {
            public DisabledScope(bool disabled) { }
            public void Dispose() { }
        }

        public class ChangeCheckScope : IDisposable
        {
            public ChangeCheckScope() { }
            public bool changed { get { return false; } }
            public void Dispose() { }
        }

        public class IndentLevelScope : IDisposable
        {
            public IndentLevelScope() { }
            public IndentLevelScope(int increment) { }
            public void Dispose() { }
        }
    }

    public static class EditorStyles
    {
        public static GUIStyle boldLabel { get { return null; } }
        public static GUIStyle largeLabel { get { return null; } }
        public static GUIStyle miniLabel { get { return null; } }
        public static GUIStyle wordWrappedLabel { get { return null; } }
        public static GUIStyle helpBox { get { return null; } }
        public static GUIStyle textField { get { return null; } }
        public static GUIStyle toolbarButton { get { return null; } }
        public static GUIStyle foldout { get { return null; } }
    }

    public static class EditorApplication
    {
        public static bool isPlaying { get; set; }
        public static bool isPaused { get; set; }
        public static bool isCompiling { get { return false; } }
        public static bool isPlayingOrWillChangePlaymode { get { return false; } }
        public static event Action update;
        public static event Action delayCall;
        public static void ExitPlaymode() { }
        public static void EnterPlaymode() { }
        public static void Exit(int returnValue) { }
    }

    public static class EditorPrefs
    {
        public static void SetInt(string key, int value) { }
        public static int GetInt(string key, int defaultValue) { return 0; }
        public static void SetString(string key, string value) { }
        public static string GetString(string key, string defaultValue) { return ""; }
        public static void SetBool(string key, bool value) { }
        public static bool GetBool(string key, bool defaultValue) { return false; }
        public static void SetFloat(string key, float value) { }
        public static float GetFloat(string key, float defaultValue) { return 0f; }
        public static bool HasKey(string key) { return false; }
        public static void DeleteKey(string key) { }
    }

    public static class Selection
    {
        public static UnityEngine.Object activeObject { get; set; }
        public static GameObject activeGameObject { get; set; }
        public static Transform activeTransform { get; set; }
        public static UnityEngine.Object[] objects { get; set; }
        public static GameObject[] gameObjects { get { return null; } }
    }

    public static class Undo
    {
        public static void RecordObject(UnityEngine.Object objectToUndo, string name) { }
        public static void RegisterCreatedObjectUndo(UnityEngine.Object objectToUndo, string name) { }
        public static void DestroyObjectImmediate(UnityEngine.Object objectToUndo) { }
        public static void SetTransformParent(Transform transform, Transform newParent, string name) { }
        public static T AddComponent<T>(GameObject gameObject) where T : Component { return null; }
    }

    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath) { return null; }
        public static GameObject SaveAsPrefabAssetAndConnect(GameObject instanceRoot, string assetPath, InteractionMode action) { return null; }
        public static GameObject InstantiatePrefab(UnityEngine.Object assetComponentOrGameObject) { return null; }
        public static GameObject LoadPrefabContents(string assetPath) { return null; }
        public static void UnloadPrefabContents(GameObject contentsRoot) { }
        public static bool IsPartOfPrefabAsset(UnityEngine.Object componentOrGameObject) { return false; }
    }

    public enum InteractionMode { AutomatedAction, UserAction }

    public static class Handles
    {
        public static Color color { get; set; }
        public static void DrawLine(Vector3 p1, Vector3 p2) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
        public static void DrawWireDisc(Vector3 center, Vector3 normal, float radius) { }
        public static void Label(Vector3 position, string text) { }
        public static void Label(Vector3 position, string text, GUIStyle style) { }
        public static void DrawSolidRectangleWithOutline(Vector3[] verts, Color faceColor, Color outlineColor) { }
    }

    public class SceneView : EditorWindow
    {
        public static SceneView lastActiveSceneView { get { return null; } }
        public Camera camera { get { return null; } }
        public Vector3 pivot { get; set; }
        public Quaternion rotation { get; set; }
        public float size { get; set; }
        public void AlignViewToObject(Transform t) { }
        public static void RepaintAll() { }
        public static event Action<SceneView> duringSceneGui;
    }

    public static class Tools
    {
        public static Tool current { get; set; }
    }

    public enum Tool { View, Move, Rotate, Scale, Rect, Transform, None, Custom }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static string applicationIdentifier { get; set; }
        public static string bundleVersion { get; set; }
        public static ColorSpace colorSpace { get; set; }
        public static bool gpuSkinning { get; set; }
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static void SetApplicationIdentifier(NamedBuildTarget buildTarget, string identifier) { }
        public static string GetApplicationIdentifier(NamedBuildTarget buildTarget) { return ""; }
        public static void SetScriptingBackend(NamedBuildTarget buildTarget, ScriptingImplementation backend) { }
        public static ScriptingImplementation GetScriptingBackend(NamedBuildTarget buildTarget) { return default(ScriptingImplementation); }
        public static UnityEngine.Rendering.GraphicsDeviceType[] GetGraphicsAPIs(BuildTarget platform) { return null; }
        public static void SetManagedStrippingLevel(NamedBuildTarget buildTarget, ManagedStrippingLevel level) { }
        public static void SetMobileMTRendering(NamedBuildTarget buildTarget, bool enable) { }
        public static void SetUseDefaultGraphicsAPIs(BuildTarget platform, bool automatic) { }
        public static void SetGraphicsAPIs(BuildTarget platform, UnityEngine.Rendering.GraphicsDeviceType[] apis) { }
        public static void SetScriptingDefineSymbols(NamedBuildTarget buildTarget, string defines) { }
        public static string GetScriptingDefineSymbols(NamedBuildTarget buildTarget) { return ""; }
        public static string[] GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup) { return null; }
        public static void SetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, string defines) { }

        public static class Android
        {
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static AndroidSdkVersions targetSdkVersion { get; set; }
            public static AndroidArchitecture targetArchitectures { get; set; }
            public static bool forceInternetPermission { get; set; }
            public static int bundleVersionCode { get; set; }
            public static bool forceSDCardPermission { get; set; }
            public static bool androidTVCompatibility { get; set; }
            public static bool androidIsGame { get; set; }
            public static AndroidPreferredInstallLocation preferredInstallLocation { get; set; }
            public static AndroidBuildType androidBuildType { get; set; }
            public static bool optimizedFramePacing { get; set; }
        }
    }

    public enum ColorSpace { Uninitialized = -1, Gamma = 0, Linear = 1 }
    public enum AndroidSdkVersions { AndroidApiLevelAuto = 0, AndroidApiLevel29 = 29, AndroidApiLevel30 = 30, AndroidApiLevel32 = 32, AndroidApiLevel33 = 33 }
    [Flags] public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2, All = 3 }
    public enum BuildTargetGroup { Unknown, Standalone, Android, iOS, WebGL }
    public enum BuildTarget { NoTarget = -2, StandaloneWindows64 = 19, Android = 13, iOS = 9, WebGL = 20 }

    // UnityEditor.ShaderUtil, with the signatures WintrySetupVerifier relies on. The stand-in never runs a
    // shader compiler, so it reports "no error" — the harness checks that the call sites compile, and the
    // real answer only comes from Unity (see tools/offline-check/README.md).
    public struct ShaderMessage
    {
        public string message { get; set; }
        public string messageDetails { get; set; }
        public string file { get; set; }
        public int line { get; set; }
    }

    public static class ShaderUtil
    {
        public static bool ShaderHasError(Shader shader) { return false; }
        public static ShaderMessage[] GetShaderMessages(Shader shader) { return new ShaderMessage[0]; }
    }

    public static class EditorUserBuildSettings
    {
        public static BuildTarget activeBuildTarget { get { return default(BuildTarget); } }
        public static BuildTargetGroup selectedBuildTargetGroup { get { return default(BuildTargetGroup); } }
        public static bool development { get; set; }
        public static MobileTextureSubtarget androidBuildSubtarget { get; set; }
        public static AndroidBuildType androidBuildType { get; set; }
        public static bool exportAsGoogleAndroidProject { get; set; }
        public static bool buildAppBundle { get; set; }
        public static bool SwitchActiveBuildTarget(BuildTargetGroup targetGroup, BuildTarget target) { return false; }
    }

    public struct BuildPlayerOptions
    {
        public string[] scenes { get; set; }
        public string locationPathName { get; set; }
        public string assetBundleManifestPath { get; set; }
        public BuildTargetGroup targetGroup { get; set; }
        public BuildTarget target { get; set; }
        public BuildOptions options { get; set; }
        public string[] extraScriptingDefines { get; set; }
    }

    public static class BuildPipeline
    {
        public static object BuildPlayer(string[] levels, string locationPathName, BuildTarget target, BuildOptions options) { return null; }
        public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions options) { return null; }
    }

    [Flags]
    public enum BuildOptions
    {
        None = 0, Development = 1, AutoRunPlayer = 4, ShowBuiltPlayer = 8, BuildAdditionalStreamedScenes = 16,
        AcceptExternalModificationsToPlayer = 32, ConnectWithProfiler = 256, AllowDebugging = 512,
        SymlinkSources = 1024, UncompressedAssetBundle = 8192, ConnectToHost = 4096, CompressWithLz4 = 262144
    }

    public static class AssetImporter
    {
        public static UnityEditor.AssetImporterBase GetAtPath(string path) { return null; }
    }

    public class AssetImporterBase { public string assetPath { get; set; } public void SaveAndReimport() { } }

    public class TextureImporter : AssetImporterBase
    {
        public bool isReadable { get; set; }
        public bool mipmapEnabled { get; set; }
        public int maxTextureSize { get; set; }
        public TextureImporterType textureType { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
        public TextureImporterShape textureShape { get; set; }
        public TextureImporterAlphaSource alphaSource { get; set; }
        public bool sRGBTexture { get; set; }
        public bool alphaIsTransparency { get; set; }
        public bool crunchedCompression { get; set; }
        public int compressionQuality { get; set; }
        public UnityEngine.FilterMode filterMode { get; set; }
        public UnityEngine.TextureWrapMode wrapMode { get; set; }
        public int anisoLevel { get; set; }
        public bool streamingMipmaps { get; set; }
    }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object obj) { }
        public SerializedProperty FindProperty(string propertyPath) { return null; }
        public void Update() { }
        public bool ApplyModifiedProperties() { return false; }
    }

    public class SerializedProperty
    {
        public bool boolValue { get; set; }
        public int intValue { get; set; }
        public float floatValue { get; set; }
        public string stringValue { get; set; }
        public UnityEngine.Object objectReferenceValue { get; set; }
    }
}

namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup) { return default(UnityEngine.SceneManagement.Scene); }
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup, NewSceneMode mode) { return default(UnityEngine.SceneManagement.Scene); }
        public static UnityEngine.SceneManagement.Scene OpenScene(string scenePath) { return default(UnityEngine.SceneManagement.Scene); }
        public static UnityEngine.SceneManagement.Scene OpenScene(string scenePath, OpenSceneMode mode) { return default(UnityEngine.SceneManagement.Scene); }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene) { return false; }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene, string dstScenePath) { return false; }
        public static bool SaveOpenScenes() { return false; }
        public static bool MarkSceneDirty(UnityEngine.SceneManagement.Scene scene) { return false; }
        public static UnityEngine.SceneManagement.Scene GetActiveScene() { return default(UnityEngine.SceneManagement.Scene); }
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return false; }
    }

    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading }
}

namespace UnityEditor.Build
{
    public interface IOrderedCallback { int callbackOrder { get; } }
    public interface IPreprocessBuildWithReport : IOrderedCallback { void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report); }
    public interface IPostprocessBuildWithReport : IOrderedCallback { void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report); }
}

namespace UnityEditor.Build.Reporting
{
    public class BuildReport
    {
        public BuildSummary summary { get { return default(BuildSummary); } }
    }

    public struct BuildSummary
    {
        public UnityEditor.BuildTarget platform { get { return default(UnityEditor.BuildTarget); } }
        public string outputPath { get { return ""; } }
        public BuildResult result { get { return default(BuildResult); } }
        public ulong totalSize { get { return 0UL; } }
    }

    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
}

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Class)] public class TestFixtureAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class TestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class SetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class TearDownAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class OneTimeSetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class OneTimeTearDownAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)] public class TestCaseAttribute : Attribute
    {
        public TestCaseAttribute(params object[] arguments) { Arguments = arguments ?? new object[0]; }
        public object[] Arguments { get; private set; }
        public object ExpectedResult { get; set; }
        public string TestName { get; set; }
    }
    [AttributeUsage(AttributeTargets.Method)] public class IgnoreAttribute : Attribute { public IgnoreAttribute(string reason) { } }
    [AttributeUsage(AttributeTargets.All)] public class CategoryAttribute : Attribute { public CategoryAttribute(string name) { } }
    [AttributeUsage(AttributeTargets.Method)] public class TimeoutAttribute : Attribute { public TimeoutAttribute(int timeout) { } }
    [AttributeUsage(AttributeTargets.Method)] public class DescriptionAttribute : Attribute { public DescriptionAttribute(string description) { } }
    [AttributeUsage(AttributeTargets.Method)] public class RepeatAttribute : Attribute { public RepeatAttribute(int count) { } }
    [AttributeUsage(AttributeTargets.Method)] public class MaxTimeAttribute : Attribute { public MaxTimeAttribute(int ms) { } }
    [AttributeUsage(AttributeTargets.Parameter)] public class ValuesAttribute : Attribute { public ValuesAttribute(params object[] args) { } }
}
