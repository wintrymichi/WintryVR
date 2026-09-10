// Faithful-signature stubs of the UnityEngine object model, for offline compile verification only.
#pragma warning disable 0067 // events here are declarations only; the stand-ins never raise them
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public int GetInstanceID() { return 0; }
        public override string ToString() { return ""; }
        public static void Destroy(Object obj) { }
        public static void Destroy(Object obj, float t) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DestroyImmediate(Object obj, bool allowDestroyingAssets) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static Object Instantiate(Object original) { return null; }
        public static Object Instantiate(Object original, Transform parent) { return null; }
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation) { return null; }
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent) { return null; }
        public static T Instantiate<T>(T original) where T : Object { return null; }
        public static T Instantiate<T>(T original, Transform parent) where T : Object { return null; }
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object { return null; }
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object { return null; }
        public static T FindObjectOfType<T>() where T : Object { return null; }
        public static T[] FindObjectsOfType<T>() where T : Object { return null; }
        public static T FindAnyObjectByType<T>() where T : Object { return null; }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sortMode) where T : Object { return null; }
        public static bool operator ==(Object x, Object y) { return ReferenceEquals(x, y); }
        public static bool operator !=(Object x, Object y) { return !ReferenceEquals(x, y); }
        public static implicit operator bool(Object exists) { return !ReferenceEquals(exists, null); }
        public override bool Equals(object other) { return ReferenceEquals(this, other); }
        public override int GetHashCode() { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this); }
    }

    public enum FindObjectsSortMode { None, InstanceID }
    public enum FindObjectsInactive { Exclude, Include }

    [Flags]
    public enum HideFlags
    {
        None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSaveInEditor = 4,
        NotEditable = 8, DontSaveInBuild = 16, DontUnloadUnusedAsset = 32,
        DontSave = 52, HideAndDontSave = 61
    }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform { get { return null; } }
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get { return false; } }
        public bool activeInHierarchy { get { return false; } }
        public bool isStatic { get; set; }
        public UnityEngine.SceneManagement.Scene scene { get { return default(UnityEngine.SceneManagement.Scene); } }
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component { return null; }
        public Component AddComponent(Type componentType) { return null; }
        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type type) { return null; }
        public Component GetComponent(string type) { return null; }
        public bool TryGetComponent<T>(out T component) { component = default(T); return false; }
        public T GetComponentInChildren<T>() { return default(T); }
        public T GetComponentInChildren<T>(bool includeInactive) { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponents<T>() { return null; }
        public Component[] GetComponents(Type type) { return null; }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return null; }
        public T[] GetComponentsInParent<T>() { return null; }
        public void SendMessage(string methodName) { }
        public void SendMessage(string methodName, object value) { }
        public void SendMessage(string methodName, SendMessageOptions options) { }
        public void BroadcastMessage(string methodName) { }
        public bool CompareTag(string tag) { return false; }
        public static GameObject Find(string name) { return null; }
        public static GameObject FindWithTag(string tag) { return null; }
        public static GameObject[] FindGameObjectsWithTag(string tag) { return null; }
        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
    }

    public enum SendMessageOptions { RequireReceiver, DontRequireReceiver }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum Space { World, Self }

    public class Component : Object
    {
        public Transform transform { get { return null; } }
        public GameObject gameObject { get { return null; } }
        public string tag { get { return null; } set { } }
        public T GetComponent<T>() { return default(T); }
        public Component GetComponent(Type type) { return null; }
        public bool TryGetComponent<T>(out T component) { component = default(T); return false; }
        public T GetComponentInChildren<T>() { return default(T); }
        public T GetComponentInChildren<T>(bool includeInactive) { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponents<T>() { return null; }
        public T[] GetComponentsInChildren<T>() { return null; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return null; }
        public T[] GetComponentsInParent<T>() { return null; }
        public void SendMessage(string methodName) { }
        public void SendMessage(string methodName, object value) { }
        public bool CompareTag(string tag) { return false; }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get { return false; } }
    }

    public class MonoBehaviour : Behaviour
    {
        public bool useGUILayout { get; set; }
        public Coroutine StartCoroutine(IEnumerator routine) { return null; }
        public Coroutine StartCoroutine(string methodName) { return null; }
        public Coroutine StartCoroutine(string methodName, object value) { return null; }
        public void StopCoroutine(Coroutine routine) { }
        public void StopCoroutine(IEnumerator routine) { }
        public void StopCoroutine(string methodName) { }
        public void StopAllCoroutines() { }
        public void Invoke(string methodName, float time) { }
        public void InvokeRepeating(string methodName, float time, float repeatRate) { }
        public void CancelInvoke() { }
        public void CancelInvoke(string methodName) { }
        public bool IsInvoking() { return false; }
        public bool IsInvoking(string methodName) { return false; }
        public static void print(object message) { }
    }

    public class ScriptableObject : Object
    {
        public static ScriptableObject CreateInstance(Type type) { return null; }
        public static T CreateInstance<T>() where T : ScriptableObject { return null; }
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale { get { return default(Vector3); } }
        public Vector3 forward { get; set; }
        public Vector3 right { get; set; }
        public Vector3 up { get; set; }
        public Transform parent { get; set; }
        public Transform root { get { return null; } }
        public int childCount { get { return 0; } }
        public int siblingIndex { get { return 0; } }
        public bool hasChanged { get; set; }
        public Matrix4x4 localToWorldMatrix { get { return default(Matrix4x4); } }
        public Matrix4x4 worldToLocalMatrix { get { return default(Matrix4x4); } }
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation) { }
        public void Translate(Vector3 translation) { }
        public void Translate(Vector3 translation, Space relativeTo) { }
        public void Translate(float x, float y, float z) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 eulers, Space relativeTo) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void Rotate(Vector3 axis, float angle, Space relativeTo) { }
        public void Rotate(float xAngle, float yAngle, float zAngle) { }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { }
        public void LookAt(Transform target) { }
        public void LookAt(Transform target, Vector3 worldUp) { }
        public void LookAt(Vector3 worldPosition) { }
        public void LookAt(Vector3 worldPosition, Vector3 worldUp) { }
        public Vector3 TransformPoint(Vector3 position) { return default(Vector3); }
        public Vector3 TransformDirection(Vector3 direction) { return default(Vector3); }
        public Vector3 TransformVector(Vector3 vector) { return default(Vector3); }
        public Vector3 InverseTransformPoint(Vector3 position) { return default(Vector3); }
        public Vector3 InverseTransformDirection(Vector3 direction) { return default(Vector3); }
        public Vector3 InverseTransformVector(Vector3 vector) { return default(Vector3); }
        public Transform Find(string n) { return null; }
        public Transform GetChild(int index) { return null; }
        public void SetSiblingIndex(int index) { }
        public int GetSiblingIndex() { return 0; }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void DetachChildren() { }
        public bool IsChildOf(Transform parent) { return false; }
        public IEnumerator GetEnumerator() { return null; }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Rect rect { get { return default(Rect); } }
    }

    public sealed class Coroutine : YieldInstruction { }
    public class YieldInstruction { }
    public sealed class WaitForSeconds : YieldInstruction { public WaitForSeconds(float seconds) { } }
    public sealed class WaitForSecondsRealtime : CustomYieldInstruction
    {
        public WaitForSecondsRealtime(float time) { }
        public override bool keepWaiting { get { return false; } }
    }
    public sealed class WaitForEndOfFrame : YieldInstruction { }
    public sealed class WaitForFixedUpdate : YieldInstruction { }
    public abstract class CustomYieldInstruction : IEnumerator
    {
        public abstract bool keepWaiting { get; }
        public object Current { get { return null; } }
        public bool MoveNext() { return false; }
        public void Reset() { }
    }
    public sealed class WaitUntil : CustomYieldInstruction
    {
        public WaitUntil(Func<bool> predicate) { }
        public override bool keepWaiting { get { return false; } }
    }
    public sealed class WaitWhile : CustomYieldInstruction
    {
        public WaitWhile(Func<bool> predicate) { }
        public override bool keepWaiting { get { return false; } }
    }

    public class Debug
    {
        public static void Log(object message) { Console.WriteLine("[Log] " + message); }
        public static void Log(object message, Object context) { }
        public static void LogWarning(object message) { Console.WriteLine("[Warn] " + message); }
        public static void LogWarning(object message, Object context) { }
        public static void LogError(object message) { Console.WriteLine("[Error] " + message); }
        public static void LogError(object message, Object context) { }
        public static void LogException(Exception exception) { Console.WriteLine("[Exception] " + exception); }
        public static void LogException(Exception exception, Object context) { }
        public static void LogFormat(string format, params object[] args) { }
        public static void LogWarningFormat(string format, params object[] args) { }
        public static void LogErrorFormat(string format, params object[] args) { }
        public static void DrawLine(Vector3 start, Vector3 end) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color) { }
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) { }
        public static void DrawRay(Vector3 start, Vector3 dir) { }
        public static void DrawRay(Vector3 start, Vector3 dir, Color color) { }
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) { }
        public static void Assert(bool condition) { }
        public static void Break() { }
        public static bool isDebugBuild { get { return false; } }
        public static bool developerConsoleVisible { get; set; }
    }

    public static class Time
    {
        public static float time { get { return 0f; } }
        public static float timeSinceLevelLoad { get { return 0f; } }
        public static float deltaTime { get { return 0.02f; } }
        public static float fixedDeltaTime { get { return 0f; } set { } }
        public static float unscaledTime { get { return 0f; } }
        public static float unscaledDeltaTime { get { return 0f; } }
        public static float realtimeSinceStartup { get { return (float)(DateTime.UtcNow - _start).TotalSeconds; } }
        private static readonly DateTime _start = DateTime.UtcNow;
        public static double realtimeSinceStartupAsDouble { get { return 0d; } }
        public static float timeScale { get { return 0f; } set { } }
        public static int frameCount { get { return 0; } }
        public static float smoothDeltaTime { get { return 0f; } }
        public static float maximumDeltaTime { get { return 0f; } set { } }
        public static int captureFramerate { get { return 0; } set { } }
    }

    public static class Application
    {
        public static bool isBatchMode { get { return false; } }
        public static string dataPath { get { return ""; } }
        public static string persistentDataPath { get { return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WintryVRTest"); } }
        public static string streamingAssetsPath { get { return ""; } }
        public static string temporaryCachePath { get { return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WintryVRTestCache"); } }
        public static string productName { get { return ""; } }
        public static string companyName { get { return ""; } }
        public static string version { get { return ""; } }
        public static string unityVersion { get { return ""; } }
        public static string identifier { get { return ""; } }
        public static bool isPlaying { get { return false; } }
        public static bool isEditor { get { return true; } }
        public static bool isFocused { get { return false; } }
        public static bool isMobilePlatform { get { return false; } }
        public static bool runInBackground { get { return false; } set { } }
        public static int targetFrameRate { get { return 0; } set { } }
        public static RuntimePlatform platform { get { return default(RuntimePlatform); } }
        public static SystemLanguage systemLanguage { get { return default(SystemLanguage); } }
        public static NetworkReachability internetReachability { get { return default(NetworkReachability); } }
        public static void Quit() { }
        public static void Quit(int exitCode) { }
        public static void OpenURL(string url) { }
        public static event Action<bool> focusChanged;
        public static event Action quitting;
        public static event Application.LogCallback logMessageReceived;
        public delegate void LogCallback(string condition, string stackTrace, LogType type);
    }

    public enum LogType { Error, Assert, Warning, Log, Exception }

    public enum RuntimePlatform
    {
        OSXEditor, OSXPlayer, WindowsPlayer, WindowsEditor, IPhonePlayer,
        Android, LinuxPlayer, LinuxEditor, WebGLPlayer, PS4, XboxOne, Switch
    }

    public enum SystemLanguage
    {
        Afrikaans, Arabic, Basque, Belarusian, Bulgarian, Catalan, Chinese, Czech, Danish,
        Dutch, English, Estonian, Faroese, Finnish, French, German, Greek, Hebrew, Hungarian,
        Icelandic, Indonesian, Italian, Japanese, Korean, Latvian, Lithuanian, Norwegian,
        Polish, Portuguese, Romanian, Russian, SerboCroatian, Slovak, Slovenian, Spanish,
        Swedish, Thai, Turkish, Ukrainian, Vietnamese, Unknown
    }

    public enum NetworkReachability { NotReachable, ReachableViaCarrierDataNetwork, ReachableViaLocalAreaNetwork }

    public static class Screen
    {
        public static int width { get { return 0; } }
        public static int height { get { return 0; } }
        public static float dpi { get { return 0f; } }
        public static bool fullScreen { get { return false; } set { } }
        public static ScreenOrientation orientation { get { return default(ScreenOrientation); } set { } }
        public static int sleepTimeout { get { return 0; } set { } }
        public static float brightness { get { return 0f; } set { } }
    }

    public enum ScreenOrientation { Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight, AutoRotation }

    public static class SleepTimeout { public const int NeverSleep = -1; public const int SystemSetting = -2; }

    public static class SystemInfo
    {
        public static string deviceModel { get { return ""; } }
        public static string deviceName { get { return ""; } }
        public static DeviceType deviceType { get { return default(DeviceType); } }
        public static string deviceUniqueIdentifier { get { return ""; } }
        public static string operatingSystem { get { return ""; } }
        public static string processorType { get { return ""; } }
        public static int processorCount { get { return 0; } }
        public static int processorFrequency { get { return 0; } }
        public static int systemMemorySize { get { return 0; } }
        public static int graphicsMemorySize { get { return 0; } }
        public static string graphicsDeviceName { get { return ""; } }
        public static string graphicsDeviceVendor { get { return ""; } }
        public static string graphicsDeviceVersion { get { return ""; } }
        public static int graphicsShaderLevel { get { return 0; } }
        public static bool supportsComputeShaders { get { return false; } }
        public static bool supportsInstancing { get { return false; } }
        public static int maxTextureSize { get { return 0; } }
        public static float batteryLevel { get { return 0f; } }
        public static BatteryStatus batteryStatus { get { return default(BatteryStatus); } }
        public static bool SupportsTextureFormat(TextureFormat format) { return false; }
        public static bool SupportsRenderTextureFormat(RenderTextureFormat format) { return false; }
    }

    public enum DeviceType { Unknown, Handheld, Console, Desktop }

    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, object> Store = new Dictionary<string, object>();
        public static void SetInt(string key, int value) { Store[key] = value; }
        public static int GetInt(string key) { return GetInt(key, 0); }
        public static int GetInt(string key, int defaultValue) { object v; return Store.TryGetValue(key, out v) && v is int ? (int)v : defaultValue; }
        public static void SetFloat(string key, float value) { Store[key] = value; }
        public static float GetFloat(string key) { return GetFloat(key, 0f); }
        public static float GetFloat(string key, float defaultValue) { object v; return Store.TryGetValue(key, out v) && v is float ? (float)v : defaultValue; }
        public static void SetString(string key, string value) { Store[key] = value; }
        public static string GetString(string key) { return GetString(key, ""); }
        public static string GetString(string key, string defaultValue) { object v; return Store.TryGetValue(key, out v) && v is string ? (string)v : defaultValue; }
        public static bool HasKey(string key) { return Store.ContainsKey(key); }
        public static void DeleteKey(string key) { Store.Remove(key); }
        public static void DeleteAll() { Store.Clear(); }
        public static void Save() { }
    }

    public static class JsonUtility
    {
        public static string ToJson(object obj) { return ""; }
        public static string ToJson(object obj, bool prettyPrint) { return ""; }
        public static T FromJson<T>(string json) { return default(T); }
        public static object FromJson(string json, Type type) { return null; }
        public static void FromJsonOverwrite(string json, object objectToOverwrite) { }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object { return null; }
        public static Object Load(string path) { return null; }
        public static Object Load(string path, Type systemTypeInstance) { return null; }
        public static T[] LoadAll<T>(string path) where T : Object { return null; }
        public static void UnloadAsset(Object assetToUnload) { }
        public static AsyncOperation UnloadUnusedAssets() { return null; }
        public static T[] FindObjectsOfTypeAll<T>() where T : Object { return null; }
        public static Object GetBuiltinResource(Type type, string path) { return null; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
    }

    public class AsyncOperation : YieldInstruction
    {
        public bool isDone { get { return false; } }
        public float progress { get { return 0f; } }
        public bool allowSceneActivation { get; set; }
        public int priority { get; set; }
        public event Action<AsyncOperation> completed;
    }

    public struct LayerMask
    {
        public int value { get; set; }
        public static int NameToLayer(string layerName) { return 0; }
        public static string LayerToName(int layer) { return ""; }
        public static int GetMask(params string[] layerNames) { return 0; }
        public static implicit operator int(LayerMask mask) { return 0; }
        public static implicit operator LayerMask(int intVal) { return default(LayerMask); }
    }

    public class SerializeField : Attribute { }
    public class SerializeReference : Attribute { }
    public class HideInInspector : Attribute { }
    public class NonSerializedAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : PropertyAttribute { public RangeAttribute(float min, float max) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : PropertyAttribute { public TooltipAttribute(string tooltip) { } }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : PropertyAttribute { public HeaderAttribute(string header) { } }
    [AttributeUsage(AttributeTargets.Field)] public class SpaceAttribute : PropertyAttribute { public SpaceAttribute() { } public SpaceAttribute(float height) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TextAreaAttribute : PropertyAttribute { public TextAreaAttribute() { } public TextAreaAttribute(int minLines, int maxLines) { } }
    [AttributeUsage(AttributeTargets.Field)] public class MultilineAttribute : PropertyAttribute { public MultilineAttribute() { } public MultilineAttribute(int lines) { } }
    public abstract class PropertyAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class RequireComponent : Attribute { public RequireComponent(Type requiredComponent) { } public RequireComponent(Type a, Type b) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class ExecuteAlways : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class ExecuteInEditMode : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class AddComponentMenu : Attribute { public AddComponentMenu(string menuName) { } public AddComponentMenu(string menuName, int order) { } }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute : Attribute { public string fileName { get; set; } public string menuName { get; set; } public int order { get; set; } }
    [AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)] public class ContextMenu : Attribute { public ContextMenu(string itemName) { } }
    [AttributeUsage(AttributeTargets.Class)] public class SelectionBaseAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    [AttributeUsage(AttributeTargets.Class)] public class IconAttribute : Attribute { public IconAttribute(string path) { } }
    [AttributeUsage(AttributeTargets.All)] public class SerializableAttributeStub : Attribute { }
}

namespace UnityEngine.SceneManagement
{
    using UnityEngine.Events;

    public struct Scene
    {
        public string name { get { return ""; } }
        public string path { get { return ""; } }
        public int buildIndex { get { return 0; } }
        public bool isLoaded { get { return false; } }
        public bool IsValid() { return false; }
        public GameObject[] GetRootGameObjects() { return null; }
    }

    public static class SceneManager
    {
        public static int sceneCount { get { return 0; } }
        public static int sceneCountInBuildSettings { get { return 0; } }
        public static Scene GetActiveScene() { return default(Scene); }
        public static Scene GetSceneAt(int index) { return default(Scene); }
        public static Scene GetSceneByName(string name) { return default(Scene); }
        public static void LoadScene(string sceneName) { }
        public static void LoadScene(int sceneBuildIndex) { }
        public static void LoadScene(string sceneName, LoadSceneMode mode) { }
        public static AsyncOperation LoadSceneAsync(string sceneName) { return null; }
        public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode) { return null; }
        public static AsyncOperation UnloadSceneAsync(string sceneName) { return null; }
        public static bool SetActiveScene(Scene scene) { return false; }
        public static void MoveGameObjectToScene(GameObject go, Scene scene) { }
        public static event UnityAction<Scene, LoadSceneMode> sceneLoaded;
        public static event UnityAction<Scene> sceneUnloaded;
    }

    public enum LoadSceneMode { Single, Additive }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);

    public abstract class UnityEventBase { }

    public class UnityEvent : UnityEventBase
    {
        public void AddListener(UnityAction call) { }
        public void RemoveListener(UnityAction call) { }
        public void RemoveAllListeners() { }
        public void Invoke() { }
    }

    public class UnityEvent<T0> : UnityEventBase
    {
        public void AddListener(UnityAction<T0> call) { }
        public void RemoveListener(UnityAction<T0> call) { }
        public void RemoveAllListeners() { }
        public void Invoke(T0 arg0) { }
    }
}
