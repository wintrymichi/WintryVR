// Faithful-signature stubs of UnityEngine graphics/physics/audio/input/networking/XR APIs.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public int anisoLevel { get; set; }
        public float mipMapBias { get; set; }
        public virtual GraphicsFormat graphicsFormat { get { return default(GraphicsFormat); } }
        public static void SetGlobalAnisotropicFilteringLimits(int forcedMin, int globalMax) { }
    }

    public sealed class Texture2D : Texture
    {
        private Color[] _px;
        public Texture2D(int width, int height) { Init(width, height); }
        public Texture2D(int width, int height, TextureFormat textureFormat, bool mipChain) { Init(width, height); format = textureFormat; }
        public Texture2D(int width, int height, TextureFormat textureFormat, bool mipChain, bool linear) { Init(width, height); format = textureFormat; }
        private void Init(int w, int h) { this.width = w; this.height = h; _px = new Color[Mathf.Max(0, w) * Mathf.Max(0, h)]; }
        private int Index(int x, int y) { return Mathf.Clamp(y, 0, height - 1) * width + Mathf.Clamp(x, 0, width - 1); }
        public TextureFormat format { get; private set; }
        public int mipmapCount { get { return 1; } }
        public bool isReadable { get { return true; } }
        public static Texture2D whiteTexture { get { return new Texture2D(1, 1); } }
        public static Texture2D blackTexture { get { return new Texture2D(1, 1); } }
        public static Texture2D grayTexture { get { return new Texture2D(1, 1); } }
        public static Texture2D normalTexture { get { return new Texture2D(1, 1); } }
        public static Texture2D redTexture { get { return new Texture2D(1, 1); } }
        public void SetPixel(int x, int y, Color color) { if (_px != null && _px.Length > 0) _px[Index(x, y)] = color; }
        public void SetPixels(Color[] colors) { if (colors != null) Array.Copy(colors, _px, Mathf.Min(colors.Length, _px.Length)); }
        public void SetPixels(int x, int y, int blockWidth, int blockHeight, Color[] colors)
        {
            if (colors == null) return;
            for (int j = 0; j < blockHeight; j++)
                for (int i = 0; i < blockWidth; i++)
                {
                    int src = j * blockWidth + i;
                    if (src < colors.Length) SetPixel(x + i, y + j, colors[src]);
                }
        }
        public void SetPixels32(Color32[] colors)
        {
            if (colors == null) return;
            for (int i = 0; i < Mathf.Min(colors.Length, _px.Length); i++) _px[i] = colors[i];
        }
        public Color GetPixel(int x, int y) { return _px == null || _px.Length == 0 ? default(Color) : _px[Index(x, y)]; }
        public Color GetPixelBilinear(float u, float v) { return GetPixel(Mathf.RoundToInt(u * (width - 1)), Mathf.RoundToInt(v * (height - 1))); }
        public Color[] GetPixels() { return (Color[])_px.Clone(); }
        public Color[] GetPixels(int x, int y, int blockWidth, int blockHeight)
        {
            var outp = new Color[blockWidth * blockHeight];
            for (int j = 0; j < blockHeight; j++)
                for (int i = 0; i < blockWidth; i++) outp[j * blockWidth + i] = GetPixel(x + i, y + j);
            return outp;
        }
        public Color32[] GetPixels32()
        {
            var outp = new Color32[_px.Length];
            for (int i = 0; i < _px.Length; i++) outp[i] = _px[i];
            return outp;
        }
        public void Apply() { }
        public void Apply(bool updateMipmaps) { }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
        public bool LoadImage(byte[] data) { return data != null && data.Length > 0; }
        public bool LoadImage(byte[] data, bool markNonReadable) { return LoadImage(data); }
        public void LoadRawTextureData(byte[] data) { }
        public byte[] GetRawTextureData() { return new byte[_px.Length * 4]; }
        public void Resize(int w, int h) { Init(w, h); }
        public bool Reinitialize(int w, int h) { Init(w, h); return true; }
        public void ReadPixels(Rect source, int destX, int destY) { }
        public void ReadPixels(Rect source, int destX, int destY, bool recalculateMipMaps) { }
        public void Compress(bool highQuality) { }
    }

    public sealed class Cubemap : Texture { public Cubemap(int width, TextureFormat format, bool mipChain) { } }

    public class RenderTexture : Texture
    {
        public RenderTexture(int width, int height, int depth) { }
        public RenderTexture(int width, int height, int depth, RenderTextureFormat format) { }
        public RenderTexture(RenderTextureDescriptor desc) { }
        public bool enableRandomWrite { get; set; }
        public int depth { get; set; }
        public RenderTextureFormat format { get; set; }
        public bool useMipMap { get; set; }
        public int antiAliasing { get; set; }
        public bool Create() { return false; }
        public void Release() { }
        public bool IsCreated() { return false; }
        public static RenderTexture active { get; set; }
        public static RenderTexture GetTemporary(int width, int height) { return null; }
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer) { return null; }
        public static RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format) { return null; }
        public static void ReleaseTemporary(RenderTexture temp) { }
    }

    public struct RenderTextureDescriptor
    {
        public RenderTextureDescriptor(int width, int height) { }
        public RenderTextureDescriptor(int width, int height, RenderTextureFormat colorFormat, int depthBufferBits) { }
        public int width { get { return 0; } set { } }
        public int height { get { return 0; } set { } }
        public int msaaSamples { get { return 0; } set { } }
        public RenderTextureFormat colorFormat { get { return default(RenderTextureFormat); } set { } }
        public int depthBufferBits { get { return 0; } set { } }
    }

    public enum TextureFormat
    {
        Alpha8, ARGB4444, RGB24, RGBA32, ARGB32, RGB565, R16, DXT1, DXT5,
        RGBA4444, BGRA32, RHalf, RGHalf, RGBAHalf, RFloat, RGFloat, RGBAFloat,
        BC7, ASTC_4x4, ASTC_6x6, ASTC_8x8, R8, RG16, RGB48, RGBA64
    }

    public enum RenderTextureFormat
    {
        ARGB32, Depth, ARGBHalf, Shadowmap, RGB565, ARGB4444, ARGB1555, Default,
        ARGB2101010, DefaultHDR, ARGB64, ARGBFloat, RGFloat, RGHalf, RFloat, RHalf,
        R8, ARGBInt, RGInt, RInt, BGRA32
    }

    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum GraphicsFormat { None, R8G8B8A8_UNorm, R8G8B8A8_SRGB }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }
        public Shader shader { get; set; }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public Vector2 mainTextureOffset { get; set; }
        public Vector2 mainTextureScale { get; set; }
        public int renderQueue { get; set; }
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        public string[] shaderKeywords { get; set; }
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
        public void SetInt(string name, int value) { }
        public void SetColor(string name, Color value) { }
        public void SetColor(int nameID, Color value) { }
        public void SetVector(string name, Vector4 value) { }
        public void SetVector(int nameID, Vector4 value) { }
        public void SetTexture(string name, Texture value) { }
        public void SetTexture(int nameID, Texture value) { }
        public void SetMatrix(string name, Matrix4x4 value) { }
        public void SetTextureOffset(string name, Vector2 value) { }
        public void SetTextureScale(string name, Vector2 value) { }
        public float GetFloat(string name) { return 0f; }
        public int GetInt(string name) { return 0; }
        public Color GetColor(string name) { return default(Color); }
        public Vector4 GetVector(string name) { return default(Vector4); }
        public Texture GetTexture(string name) { return null; }
        public bool HasProperty(string name) { return false; }
        public bool HasProperty(int nameID) { return false; }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
        public bool IsKeywordEnabled(string keyword) { return false; }
        public void CopyPropertiesFromMaterial(Material mat) { }
        public int FindPass(string passName) { return 0; }
        public void SetOverrideTag(string tag, string val) { }
    }

    public sealed class Shader : Object
    {
        public bool isSupported { get { return false; } }
        public int renderQueue { get { return 0; } }
        public static Shader Find(string name) { return null; }
        public static int PropertyToID(string name) { return 0; }
        public static void SetGlobalFloat(string name, float value) { }
        public static void SetGlobalColor(string name, Color value) { }
        public static void SetGlobalVector(string name, Vector4 value) { }
        public static void SetGlobalTexture(string name, Texture value) { }
        public static void EnableKeyword(string keyword) { }
        public static void DisableKeyword(string keyword) { }
    }

    public class Mesh : Object
    {
        public Mesh() { }
        public Vector3[] vertices { get; set; }
        public Vector3[] normals { get; set; }
        public Vector4[] tangents { get; set; }
        public Vector2[] uv { get; set; }
        public Vector2[] uv2 { get; set; }
        public Color[] colors { get; set; }
        public Color32[] colors32 { get; set; }
        public int[] triangles { get; set; }
        public int vertexCount { get { return vertices == null ? 0 : vertices.Length; } }
        public int subMeshCount { get; set; }
        public Bounds bounds { get; set; }
        public IndexFormat indexFormat { get; set; }
        public void Clear() { vertices = null; normals = null; uv = null; colors = null; triangles = null; }
        public void Clear(bool keepVertexLayout) { Clear(); }
        public void RecalculateNormals()
        {
            if (vertices == null || triangles == null) return;
            var n = new Vector3[vertices.Length];
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                if (a >= vertices.Length || b >= vertices.Length || c >= vertices.Length) continue;
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                n[a] += face; n[b] += face; n[c] += face;
            }
            for (int i = 0; i < n.Length; i++) n[i] = n[i].normalized;
            normals = n;
        }
        public void RecalculateBounds()
        {
            if (vertices == null || vertices.Length == 0) return;
            Vector3 mn = vertices[0], mx = vertices[0];
            foreach (var v in vertices) { mn = Vector3.Min(mn, v); mx = Vector3.Max(mx, v); }
            var b = new Bounds(); b.SetMinMax(mn, mx); bounds = b;
        }
        public void RecalculateTangents() { }
        public void Optimize() { }
        public void MarkDynamic() { }
        public void UploadMeshData(bool markNoLongerReadable) { }
        public void SetVertices(List<Vector3> inVertices) { vertices = inVertices == null ? null : inVertices.ToArray(); }
        public void SetVertices(Vector3[] inVertices) { vertices = inVertices; }
        public void SetNormals(List<Vector3> inNormals) { normals = inNormals == null ? null : inNormals.ToArray(); }
        public void SetNormals(Vector3[] inNormals) { normals = inNormals; }
        public void SetUVs(int channel, List<Vector2> uvs) { if (channel == 0) uv = uvs == null ? null : uvs.ToArray(); else uv2 = uvs == null ? null : uvs.ToArray(); }
        public void SetUVs(int channel, Vector2[] uvs) { if (channel == 0) uv = uvs; else uv2 = uvs; }
        public void SetColors(List<Color> inColors) { colors = inColors == null ? null : inColors.ToArray(); }
        public void SetColors(Color[] inColors) { colors = inColors; }
        public void SetTriangles(List<int> tris, int submesh) { triangles = tris == null ? null : tris.ToArray(); }
        public void SetTriangles(int[] tris, int submesh) { triangles = tris; }
        public void SetIndices(int[] indices, MeshTopology topology, int submesh) { triangles = indices; }
        public int[] GetTriangles(int submesh) { return triangles; }
        public void CombineMeshes(CombineInstance[] combine) { }
        public void CombineMeshes(CombineInstance[] combine, bool mergeSubMeshes) { }
    }

    public enum IndexFormat { UInt16, UInt32 }
    public enum MeshTopology { Triangles, Quads, Lines, LineStrip, Points }
    public struct CombineInstance { public Mesh mesh { get; set; } public Matrix4x4 transform { get; set; } public int subMeshIndex { get; set; } }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] materials { get; set; }
        public Material[] sharedMaterials { get; set; }
        public Bounds bounds { get { return default(Bounds); } }
        public bool isVisible { get { return false; } }
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
        public int sortingOrder { get; set; }
        public void SetPropertyBlock(MaterialPropertyBlock properties) { }
        public void GetPropertyBlock(MaterialPropertyBlock properties) { }
    }

    public class MaterialPropertyBlock
    {
        public void SetFloat(string name, float value) { }
        public void SetColor(string name, Color value) { }
        public void SetVector(string name, Vector4 value) { }
        public void SetTexture(string name, Texture value) { }
        public void Clear() { }
    }

    public class MeshRenderer : Renderer { }
    public class SkinnedMeshRenderer : Renderer
    {
        public Mesh sharedMesh { get; set; }
        public Transform rootBone { get; set; }
        public Transform[] bones { get; set; }
        public void SetBlendShapeWeight(int index, float value) { }
        public float GetBlendShapeWeight(int index) { return 0f; }
        public void BakeMesh(Mesh mesh) { }
    }

    public sealed class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public float widthMultiplier { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public Gradient colorGradient { get; set; }
        public AnimationCurve widthCurve { get; set; }
        public bool useWorldSpace { get; set; }
        public bool loop { get; set; }
        public int numCapVertices { get; set; }
        public int numCornerVertices { get; set; }
        public LineTextureMode textureMode { get; set; }
        public void SetPosition(int index, Vector3 position) { }
        public Vector3 GetPosition(int index) { return default(Vector3); }
        public void SetPositions(Vector3[] positions) { }
        public void SetWidth(float start, float end) { }
        public void BakeMesh(Mesh mesh) { }
    }

    public enum LineTextureMode { Stretch, Tile, DistributePerSegment, RepeatPerSegment }

    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public static Camera current { get { return null; } }
        public static Camera[] allCameras { get { return null; } }
        public static int allCamerasCount { get { return 0; } }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float aspect { get; set; }
        public int pixelWidth { get { return 0; } }
        public int pixelHeight { get { return 0; } }
        public Rect rect { get; set; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public int cullingMask { get; set; }
        public float depth { get; set; }
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public RenderTexture targetTexture { get; set; }
        public Matrix4x4 projectionMatrix { get; set; }
        public Matrix4x4 worldToCameraMatrix { get; set; }
        public Vector3 WorldToScreenPoint(Vector3 position) { return default(Vector3); }
        public Vector3 ScreenToWorldPoint(Vector3 position) { return default(Vector3); }
        public Vector3 WorldToViewportPoint(Vector3 position) { return default(Vector3); }
        public Vector3 ViewportToWorldPoint(Vector3 position) { return default(Vector3); }
        public Ray ScreenPointToRay(Vector3 pos) { return default(Ray); }
        public Ray ScreenPointToRay(Vector2 pos) { return default(Ray); }
        public Ray ViewportPointToRay(Vector3 pos) { return default(Ray); }
        public void Render() { }
        public void RenderToCubemap(Cubemap cubemap) { }
        public void ResetProjectionMatrix() { }
    }

    public enum CameraClearFlags { Skybox, Color, SolidColor, Depth, Nothing }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
        public float spotAngle { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
        public int cullingMask { get; set; }
    }

    public enum LightType { Spot, Directional, Point, Area, Rectangle, Disc }
    public enum LightShadows { None, Hard, Soft }

    public sealed class Sprite : Object
    {
        public Rect rect { get { return default(Rect); } }
        public Texture2D texture { get { return null; } }
        public Bounds bounds { get { return default(Bounds); } }
        public float pixelsPerUnit { get { return 0f; } }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) { return null; }
    }

    public class Font : Object
    {
        public Material material { get; set; }
        public int fontSize { get { return 0; } }
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) { return null; }
        public static string[] GetOSInstalledFontNames() { return null; }
        public bool HasCharacter(char c) { return false; }
    }

    public class Graphics
    {
        public static void Blit(Texture source, RenderTexture dest) { }
        public static void Blit(Texture source, RenderTexture dest, Material mat) { }
        public static void CopyTexture(Texture src, Texture dst) { }
        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer) { }
        public static void DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation) { }
    }

    public class Animator : Behaviour
    {
        public float speed { get; set; }
        public bool applyRootMotion { get; set; }
        public RuntimeAnimatorController runtimeAnimatorController { get; set; }
        public Avatar avatar { get; set; }
        public bool isHuman { get { return false; } }
        public int layerCount { get { return 0; } }
        public void Play(string stateName) { }
        public void Play(string stateName, int layer) { }
        public void Play(string stateName, int layer, float normalizedTime) { }
        public void CrossFade(string stateName, float normalizedTransitionDuration) { }
        public void CrossFade(string stateName, float normalizedTransitionDuration, int layer) { }
        public void SetFloat(string name, float value) { }
        public void SetFloat(string name, float value, float dampTime, float deltaTime) { }
        public void SetInteger(string name, int value) { }
        public void SetBool(string name, bool value) { }
        public void SetTrigger(string name) { }
        public void ResetTrigger(string name) { }
        public float GetFloat(string name) { return 0f; }
        public int GetInteger(string name) { return 0; }
        public bool GetBool(string name) { return false; }
        public void SetLayerWeight(int layerIndex, float weight) { }
        public float GetLayerWeight(int layerIndex) { return 0f; }
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex) { return default(AnimatorStateInfo); }
        public Transform GetBoneTransform(HumanBodyBones humanBoneId) { return null; }
        public void Rebind() { }
        public void Update(float deltaTime) { }
    }

    public struct AnimatorStateInfo
    {
        public float normalizedTime { get { return 0f; } }
        public float length { get { return 0f; } }
        public int shortNameHash { get { return 0; } }
        public bool IsName(string name) { return false; }
    }

    public class RuntimeAnimatorController : Object { }
    public class Avatar : Object { }
    public enum HumanBodyBones { Hips, Head, LeftHand, RightHand, Spine, Chest, Neck, LastBone }

    public class Collider : Component
    {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
        public Bounds bounds { get { return default(Bounds); } }
        public Material sharedMaterial { get; set; }
        public Vector3 ClosestPoint(Vector3 position) { return default(Vector3); }
        public Vector3 ClosestPointOnBounds(Vector3 position) { return default(Vector3); }
        public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
    }

    public class BoxCollider : Collider
    {
        public Vector3 center { get; set; }
        public Vector3 size { get; set; }
    }

    public class SphereCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
    }

    public class CapsuleCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
        public float height { get; set; }
    }

    public class MeshCollider : Collider
    {
        public Mesh sharedMesh { get; set; }
        public bool convex { get; set; }
    }

    public class Rigidbody : Component
    {
        public Vector3 velocity { get; set; }
        public Vector3 angularVelocity { get; set; }
        public float mass { get; set; }
        public float drag { get; set; }
        public bool useGravity { get; set; }
        public bool isKinematic { get; set; }
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public void AddForce(Vector3 force) { }
        public void AddForce(Vector3 force, ForceMode mode) { }
        public void MovePosition(Vector3 position) { }
        public void MoveRotation(Quaternion rot) { }
    }

    public enum ForceMode { Force, Acceleration, Impulse, VelocityChange }

    public class Physics
    {
        public const int DefaultRaycastLayers = -5;
        public const int AllLayers = -1;
        public static Vector3 gravity { get; set; }
        public static bool Raycast(Ray ray) { return false; }
        public static bool Raycast(Ray ray, float maxDistance) { return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask) { hitInfo = default(RaycastHit); return false; }
        public static RaycastHit[] RaycastAll(Ray ray) { return null; }
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance) { return null; }
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask) { return null; }
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results) { return 0; }
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance) { return 0; }
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask) { return 0; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { hitInfo = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { hitInfo = default(RaycastHit); return false; }
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { return null; }
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { return 0; }
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction) { return null; }
        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance) { hitInfo = default(RaycastHit); return false; }
        public static Collider[] OverlapSphere(Vector3 position, float radius) { return null; }
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask) { return null; }
        public static bool CheckSphere(Vector3 position, float radius) { return false; }
        public static bool Linecast(Vector3 start, Vector3 end) { return false; }
        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo) { hitInfo = default(RaycastHit); return false; }
    }

    public sealed class AudioClip : Object
    {
        private float[] _data = new float[0];
        public float length { get { return frequency > 0 ? samples / (float)frequency : 0f; } }
        public int samples { get; private set; }
        public int channels { get; private set; }
        public int frequency { get; private set; }
        public bool loadInBackground { get { return false; } }
        public AudioClipLoadType loadType { get { return AudioClipLoadType.DecompressOnLoad; } }
        public bool GetData(float[] data, int offsetSamples)
        {
            if (data == null) return false;
            for (int i = 0; i < data.Length; i++)
            {
                int src = offsetSamples * Mathf.Max(1, channels) + i;
                data[i] = src >= 0 && src < _data.Length ? _data[src] : 0f;
            }
            return true;
        }
        public bool SetData(float[] data, int offsetSamples)
        {
            if (data == null) return false;
            for (int i = 0; i < data.Length; i++)
            {
                int dst = offsetSamples * Mathf.Max(1, channels) + i;
                if (dst >= 0 && dst < _data.Length) _data[dst] = data[i];
            }
            return true;
        }
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream)
        {
            var c = new AudioClip
            {
                name = name,
                samples = lengthSamples,
                channels = Mathf.Max(1, channels),
                frequency = frequency
            };
            c._data = new float[Mathf.Max(0, lengthSamples) * Mathf.Max(1, channels)];
            return c;
        }
    }

    public enum AudioClipLoadType { DecompressOnLoad, CompressedInMemory, Streaming }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public bool loop { get; set; }
        public bool mute { get; set; }
        public bool playOnAwake { get; set; }
        public bool isPlaying { get { return false; } }
        public float time { get; set; }
        public int timeSamples { get; set; }
        public bool spatialize { get; set; }
        public bool spatializePostEffects { get; set; }
        public float spatialBlend { get; set; }
        public float minDistance { get; set; }
        public float maxDistance { get; set; }
        public float dopplerLevel { get; set; }
        public float spread { get; set; }
        public float panStereo { get; set; }
        public float reverbZoneMix { get; set; }
        public AudioRolloffMode rolloffMode { get; set; }
        public int priority { get; set; }
        public bool bypassEffects { get; set; }
        public void Play() { }
        public void Play(ulong delay) { }
        public void PlayDelayed(float delay) { }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
        public void Stop() { }
        public void Pause() { }
        public void UnPause() { }
        public void SetScheduledStartTime(double time) { }
        public void GetOutputData(float[] samples, int channel) { }
        public void GetSpectrumData(float[] samples, int channel, FFTWindow window) { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position) { }
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume) { }
    }

    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    public enum FFTWindow { Rectangular, Triangle, Hamming, Hanning, Blackman, BlackmanHarris }
    public class AudioListener : Behaviour
    {
        public static float volume { get; set; }
        public static bool pause { get; set; }
        public static void GetOutputData(float[] samples, int channel) { }
    }

    public static class AudioSettings
    {
        public static int outputSampleRate { get { return 0; } set { } }
        public static double dspTime { get { return 0d; } }
    }

    public static class Microphone
    {
        public static string[] devices { get { return null; } }
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency) { return null; }
        public static void End(string deviceName) { }
        public static bool IsRecording(string deviceName) { return false; }
        public static int GetPosition(string deviceName) { return 0; }
        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq) { minFreq = 0; maxFreq = 0; }
    }

    public sealed class WebCamTexture : Texture
    {
        public WebCamTexture() { }
        public WebCamTexture(string deviceName) { }
        public WebCamTexture(int requestedWidth, int requestedHeight) { }
        public WebCamTexture(string deviceName, int requestedWidth, int requestedHeight) { }
        public WebCamTexture(string deviceName, int requestedWidth, int requestedHeight, int requestedFPS) { }
        public string deviceName { get; set; }
        public float requestedFPS { get; set; }
        public int requestedWidth { get; set; }
        public int requestedHeight { get; set; }
        public bool isPlaying { get { return false; } }
        public bool didUpdateThisFrame { get { return false; } }
        public int videoRotationAngle { get { return 0; } }
        public bool videoVerticallyMirrored { get { return false; } }
        public static WebCamDevice[] devices { get { return null; } }
        public void Play() { }
        public void Pause() { }
        public void Stop() { }
        public Color32[] GetPixels32() { return null; }
        public Color32[] GetPixels32(Color32[] colors) { return null; }
        public Color[] GetPixels() { return null; }
        public Color GetPixel(int x, int y) { return default(Color); }
    }

    public struct WebCamDevice
    {
        public string name { get { return ""; } }
        public bool isFrontFacing { get { return false; } }
        public string[] availableResolutions { get { return null; } }
    }

    public static class Input
    {
        public static LocationService location { get { return null; } }
        public static Compass compass { get { return null; } }
        public static bool anyKey { get { return false; } }
        public static bool anyKeyDown { get { return false; } }
        public static string inputString { get { return ""; } }
        public static Vector3 mousePosition { get { return default(Vector3); } }
        public static bool mousePresent { get { return false; } }
        public static int touchCount { get { return 0; } }
        public static Touch[] touches { get { return null; } }
        public static bool touchSupported { get { return false; } }
        public static Vector3 acceleration { get { return default(Vector3); } }
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetKey(string name) { return false; }
        public static bool GetKeyDown(KeyCode key) { return false; }
        public static bool GetKeyDown(string name) { return false; }
        public static bool GetKeyUp(KeyCode key) { return false; }
        public static bool GetKeyUp(string name) { return false; }
        public static bool GetMouseButton(int button) { return false; }
        public static bool GetMouseButtonDown(int button) { return false; }
        public static bool GetMouseButtonUp(int button) { return false; }
        public static float GetAxis(string axisName) { return 0f; }
        public static float GetAxisRaw(string axisName) { return 0f; }
        public static bool GetButton(string buttonName) { return false; }
        public static bool GetButtonDown(string buttonName) { return false; }
        public static bool GetButtonUp(string buttonName) { return false; }
        public static Touch GetTouch(int index) { return default(Touch); }
    }

    public struct Touch
    {
        public int fingerId { get { return 0; } }
        public Vector2 position { get { return default(Vector2); } }
        public Vector2 deltaPosition { get { return default(Vector2); } }
        public TouchPhase phase { get { return default(TouchPhase); } }
        public int tapCount { get { return 0; } }
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public enum KeyCode
    {
        None, Backspace, Delete, Tab, Clear, Return, Pause, Escape, Space,
        Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
        KeypadPeriod, KeypadDivide, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter,
        UpArrow, DownArrow, RightArrow, LeftArrow, Insert, Home, End, PageUp, PageDown,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        Quote, Comma, Minus, Period, Slash, Semicolon, Equals, LeftBracket, Backslash, RightBracket,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Numlock, CapsLock, ScrollLock, RightShift, LeftShift, RightControl, LeftControl,
        RightAlt, LeftAlt, LeftCommand, LeftApple, LeftWindows, RightCommand, RightApple, RightWindows,
        Mouse0, Mouse1, Mouse2
    }

    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public string name { get; set; }
        public int fontSize { get; set; }
        public Font font { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
        public RectOffset padding { get; set; }
        public RectOffset margin { get; set; }
        public GUIStyleState normal { get; set; }
        public GUIStyleState hover { get; set; }
        public GUIStyleState active { get; set; }
        public Vector2 CalcSize(GUIContent content) { return default(Vector2); }
        public float CalcHeight(GUIContent content, float width) { return 0f; }
    }

    public class GUIStyleState { public Color textColor { get; set; } public Texture2D background { get; set; } }
    public class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom) { }
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }

    public class GUIContent
    {
        public GUIContent() { }
        public GUIContent(string text) { }
        public GUIContent(string text, string tooltip) { }
        public GUIContent(Texture image) { }
        public string text { get; set; }
        public string tooltip { get; set; }
        public Texture image { get; set; }
        public static GUIContent none { get { return null; } }
    }

    public class GUISkin : Object
    {
        public GUIStyle label { get; set; }
        public GUIStyle box { get; set; }
        public GUIStyle button { get; set; }
    }

    public class GUI
    {
        public static Color color { get; set; }
        public static Color backgroundColor { get; set; }
        public static Color contentColor { get; set; }
        public static GUISkin skin { get; set; }
        public static int depth { get; set; }
        public static bool enabled { get; set; }
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static void Box(Rect position, string text) { }
        public static void Box(Rect position, GUIContent content, GUIStyle style) { }
        public static bool Button(Rect position, string text) { return false; }
        public static bool Button(Rect position, string text, GUIStyle style) { return false; }
        public static void DrawTexture(Rect position, Texture image) { }
        public static string TextField(Rect position, string text) { return ""; }
        public static void BeginGroup(Rect position) { }
        public static void EndGroup() { }
    }

    public class GUILayout
    {
        public static void Label(string text, params GUILayoutOption[] options) { }
        public static void Label(Texture image, params GUILayoutOption[] options) { }
        public static void Label(GUIContent content, params GUILayoutOption[] options) { }
        public static void Label(string text, GUIStyle style, params GUILayoutOption[] options) { }
        public static bool Button(string text, params GUILayoutOption[] options) { return false; }
        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options) { return false; }
        public static void Box(string text, params GUILayoutOption[] options) { }
        public static void Space(float pixels) { }
        public static void FlexibleSpace() { }
        public static string TextField(string text, params GUILayoutOption[] options) { return ""; }
        public static string TextArea(string text, params GUILayoutOption[] options) { return ""; }
        public static bool Toggle(bool value, string text, params GUILayoutOption[] options) { return false; }
        public static float HorizontalSlider(float value, float leftValue, float rightValue, params GUILayoutOption[] options) { return 0f; }
        public static void BeginHorizontal(params GUILayoutOption[] options) { }
        public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] options) { }
        public static void EndVertical() { }
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) { return default(Vector2); }
        public static void EndScrollView() { }
        public static GUILayoutOption Width(float width) { return null; }
        public static GUILayoutOption Height(float height) { return null; }
        public static GUILayoutOption MinWidth(float minWidth) { return null; }
        public static GUILayoutOption MaxWidth(float maxWidth) { return null; }
        public static GUILayoutOption ExpandWidth(bool expand) { return null; }
        public static GUILayoutOption ExpandHeight(bool expand) { return null; }
    }

    public sealed class GUILayoutOption { }

    public class TextAsset : Object
    {
        public string text { get { return ""; } }
        public byte[] bytes { get { return null; } }
    }

    public sealed class ComputeShader : Object
    {
        public int FindKernel(string name) { return 0; }
        public void SetFloat(string name, float val) { }
        public void SetInt(string name, int val) { }
        public void SetTexture(int kernelIndex, string name, Texture texture) { }
        public void SetBuffer(int kernelIndex, string name, ComputeBuffer buffer) { }
        public void Dispatch(int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ) { }
    }

    public class ComputeBuffer : IDisposable
    {
        public ComputeBuffer(int count, int stride) { }
        public void SetData(Array data) { }
        public void GetData(Array data) { }
        public void Release() { }
        public void Dispose() { }
    }
}

namespace UnityEngine.Rendering
{
    public enum GraphicsDeviceType { Null, OpenGLES2, OpenGLES3, Vulkan, Direct3D11, Direct3D12, Metal }
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum CompareFunction { Disabled, Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always }
    public enum BlendMode { Zero, One, DstColor, SrcColor, OneMinusDstColor, SrcAlpha, OneMinusSrcColor, DstAlpha, OneMinusDstAlpha, SrcAlphaSaturate, OneMinusSrcAlpha }
    public enum CullMode { Off, Front, Back }
    public class CommandBuffer : IDisposable
    {
        public string name { get; set; }
        public void Clear() { }
        public void Blit(Texture source, RenderTexture dest) { }
        public void Dispose() { }
    }
    public class RenderPipelineAsset : ScriptableObject { }

    public static class GraphicsSettings
    {
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
        public static RenderPipelineAsset currentRenderPipeline { get { return null; } }
    }
    public static class RenderPipelineManager
    {
        public static RenderPipelineAsset currentPipeline { get { return null; } }
    }
}

namespace UnityEngine.Profiling
{
    public static class Profiler
    {
        public static bool enabled { get; set; }
        public static void BeginSample(string name) { }
        public static void EndSample() { }
        public static long GetTotalAllocatedMemoryLong() { return 0L; }
        public static long GetTotalReservedMemoryLong() { return 0L; }
        public static long GetMonoUsedSizeLong() { return 0L; }
        public static long GetMonoHeapSizeLong() { return 0L; }
    }
}

namespace UnityEngine.Android
{
    public static class Permission
    {
        public const string Camera = "android.permission.CAMERA";
        public const string Microphone = "android.permission.RECORD_AUDIO";
        public const string FineLocation = "android.permission.ACCESS_FINE_LOCATION";
        public const string CoarseLocation = "android.permission.ACCESS_COARSE_LOCATION";
        public const string ExternalStorageRead = "android.permission.READ_EXTERNAL_STORAGE";
        public const string ExternalStorageWrite = "android.permission.WRITE_EXTERNAL_STORAGE";
        public static bool HasUserAuthorizedPermission(string permission) { return false; }
        public static void RequestUserPermission(string permission) { }
        public static void RequestUserPermission(string permission, PermissionCallbacks callbacks) { }
        public static void RequestUserPermissions(string[] permissions, PermissionCallbacks callbacks) { }
    }

    public class PermissionCallbacks : AndroidJavaProxy
    {
        public PermissionCallbacks() : base("") { }
        public event Action<string> PermissionGranted;
        public event Action<string> PermissionDenied;
        public event Action<string> PermissionDeniedAndDontAskAgain;
    }
}

namespace UnityEngine
{
    public class AndroidJavaProxy
    {
        public AndroidJavaProxy(string javaInterface) { }
    }

    public class AndroidJavaObject : IDisposable
    {
        public AndroidJavaObject(string className, params object[] args) { }
        public T Call<T>(string methodName, params object[] args) { return default(T); }
        public void Call(string methodName, params object[] args) { }
        public T CallStatic<T>(string methodName, params object[] args) { return default(T); }
        public void CallStatic(string methodName, params object[] args) { }
        public T Get<T>(string fieldName) { return default(T); }
        public void Set<T>(string fieldName, T val) { }
        public T GetStatic<T>(string fieldName) { return default(T); }
        public void Dispose() { }
    }

    public class AndroidJavaClass : AndroidJavaObject
    {
        public AndroidJavaClass(string className) : base(className) { }
    }
}

namespace UnityEngine.Networking
{
    public class UnityWebRequest : IDisposable
    {
        public UnityWebRequest() { }
        public UnityWebRequest(string url) { }
        public UnityWebRequest(string url, string method) { }
        public string url { get; set; }
        public string method { get; set; }
        public string error { get { return null; } }
        public long responseCode { get { return 0L; } }
        public bool isDone { get { return false; } }
        public float uploadProgress { get { return 0f; } }
        public float downloadProgress { get { return 0f; } }
        public int timeout { get; set; }
        public Result result { get { return default(Result); } }
        public DownloadHandler downloadHandler { get; set; }
        public UploadHandler uploadHandler { get; set; }
        public void SetRequestHeader(string name, string value) { }
        public string GetRequestHeader(string name) { return null; }
        public string GetResponseHeader(string name) { return null; }
        public Dictionary<string, string> GetResponseHeaders() { return null; }
        public UnityWebRequestAsyncOperation SendWebRequest() { return null; }
        public void Abort() { }
        public void Dispose() { }
        public static UnityWebRequest Get(string uri) { return null; }
        public static UnityWebRequest Post(string uri, string postData) { return null; }
        public static UnityWebRequest Post(string uri, string postData, string contentType) { return null; }
        public static UnityWebRequest Put(string uri, byte[] bodyData) { return null; }
        public static UnityWebRequest Delete(string uri) { return null; }
        public static UnityWebRequest Head(string uri) { return null; }
        public static UnityWebRequest Post(string uri, List<IMultipartFormSection> multipartFormSections) { return null; }
        public static UnityWebRequest Post(string uri, WWWForm formData) { return null; }
        public static byte[] GenerateBoundary() { return null; }
        public static byte[] SerializeFormSections(List<IMultipartFormSection> multipartFormSections, byte[] boundary) { return null; }
        public static string EscapeURL(string s) { return ""; }
        public static string UnEscapeURL(string s) { return ""; }
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }
    }

    public class UnityWebRequestAsyncOperation : AsyncOperation
    {
        public UnityWebRequest webRequest { get { return null; } }
    }

    public class DownloadHandler : IDisposable
    {
        public byte[] data { get { return null; } }
        public string text { get { return ""; } }
        public bool isDone { get { return false; } }
        public void Dispose() { }
    }

    public class DownloadHandlerBuffer : DownloadHandler { public DownloadHandlerBuffer() { } }

    public class DownloadHandlerAudioClip : DownloadHandler
    {
        public DownloadHandlerAudioClip(string url, AudioType audioType) { }
        public AudioClip audioClip { get { return null; } }
        public bool streamAudio { get; set; }
        public static AudioClip GetContent(UnityWebRequest www) { return null; }
    }

    public class DownloadHandlerTexture : DownloadHandler
    {
        public DownloadHandlerTexture() { }
        public DownloadHandlerTexture(bool readable) { }
        public Texture2D texture { get { return null; } }
        public static Texture2D GetContent(UnityWebRequest www) { return null; }
    }

    public class UploadHandler : IDisposable
    {
        public byte[] data { get { return null; } }
        public string contentType { get; set; }
        public void Dispose() { }
    }

    public class UploadHandlerRaw : UploadHandler { public UploadHandlerRaw(byte[] data) { } }

    public static class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string uri, AudioType audioType) { return null; }
    }

    public static class UnityWebRequestTexture
    {
        public static UnityWebRequest GetTexture(string uri) { return null; }
        public static UnityWebRequest GetTexture(string uri, bool nonReadable) { return null; }
    }
}

namespace UnityEngine
{
    public class WWWForm
    {
        public WWWForm() { }
        public void AddField(string fieldName, string value) { }
        public void AddBinaryData(string fieldName, byte[] contents) { }
        public void AddBinaryData(string fieldName, byte[] contents, string fileName, string mimeType) { }
        public byte[] data { get { return null; } }
        public Dictionary<string, string> headers { get { return null; } }
    }

    public enum AudioType { UNKNOWN, ACC, AIFF, MPEG, OGGVORBIS, WAV }
}

namespace UnityEngine.XR
{
    public static class XRSettings
    {
        public static bool enabled { get; set; }
        public static bool isDeviceActive { get { return false; } }
        public static string loadedDeviceName { get { return ""; } }
        public static string[] supportedDevices { get { return null; } }
        public static float eyeTextureResolutionScale { get; set; }
        public static int eyeTextureWidth { get { return 0; } }
        public static int eyeTextureHeight { get { return 0; } }
        public static float renderViewportScale { get; set; }
    }

    public static class XRDevice
    {
        public static bool isPresent { get { return false; } }
        public static float refreshRate { get { return 0f; } }
        public static string model { get { return ""; } }
    }

    public struct InputDevice
    {
        public string name { get { return ""; } }
        public string manufacturer { get { return ""; } }
        public bool isValid { get { return false; } }
        public InputDeviceCharacteristics characteristics { get { return default(InputDeviceCharacteristics); } }
        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, out bool value) { value = false; return false; }
        public bool TryGetFeatureValue(InputFeatureUsage<float> usage, out float value) { value = 0f; return false; }
        public bool TryGetFeatureValue(InputFeatureUsage<Vector2> usage, out Vector2 value) { value = default(Vector2); return false; }
        public bool TryGetFeatureValue(InputFeatureUsage<Vector3> usage, out Vector3 value) { value = default(Vector3); return false; }
        public bool TryGetFeatureValue(InputFeatureUsage<Quaternion> usage, out Quaternion value) { value = default(Quaternion); return false; }
        public bool IsValid() { return false; }
        public static bool operator ==(InputDevice a, InputDevice b) { return false; }
        public static bool operator !=(InputDevice a, InputDevice b) { return false; }
        public override bool Equals(object obj) { return false; }
        public override int GetHashCode() { return 0; }
    }

    public struct InputFeatureUsage<T>
    {
        public string name { get { return ""; } }
        public InputFeatureUsage(string name) { }
    }

    public static class CommonUsages
    {
        public static InputFeatureUsage<bool> primaryButton { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<bool> secondaryButton { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<bool> menuButton { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<bool> triggerButton { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<bool> gripButton { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<bool> primary2DAxisClick { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<bool> primary2DAxisTouch { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<bool> userPresence { get { return default(InputFeatureUsage<bool>); } }
        public static InputFeatureUsage<float> trigger { get { return default(InputFeatureUsage<float>); } }
        public static InputFeatureUsage<float> grip { get { return default(InputFeatureUsage<float>); } }
        public static InputFeatureUsage<float> batteryLevel { get { return default(InputFeatureUsage<float>); } }
        public static InputFeatureUsage<Vector2> primary2DAxis { get { return default(InputFeatureUsage<Vector2>); } }
        public static InputFeatureUsage<Vector2> secondary2DAxis { get { return default(InputFeatureUsage<Vector2>); } }
        public static InputFeatureUsage<Vector3> devicePosition { get { return default(InputFeatureUsage<Vector3>); } }
        public static InputFeatureUsage<Vector3> deviceVelocity { get { return default(InputFeatureUsage<Vector3>); } }
        public static InputFeatureUsage<Vector3> centerEyePosition { get { return default(InputFeatureUsage<Vector3>); } }
        public static InputFeatureUsage<Quaternion> deviceRotation { get { return default(InputFeatureUsage<Quaternion>); } }
        public static InputFeatureUsage<Quaternion> centerEyeRotation { get { return default(InputFeatureUsage<Quaternion>); } }
        public static InputFeatureUsage<bool> isTracked { get { return default(InputFeatureUsage<bool>); } }
    }

    [Flags]
    public enum InputDeviceCharacteristics
    {
        None = 0, HeadMounted = 1, Camera = 2, HeldInHand = 4, HandTracking = 8,
        EyeTracking = 16, TrackedDevice = 32, Controller = 64, TrackingReference = 128,
        Left = 1024, Right = 2048, Simulated6DOF = 4096
    }

    public static class InputDevices
    {
        public static InputDevice GetDeviceAtXRNode(XRNode node) { return default(InputDevice); }
        public static void GetDevices(List<InputDevice> inputDevices) { }
        public static void GetDevicesWithCharacteristics(InputDeviceCharacteristics desiredCharacteristics, List<InputDevice> inputDevices) { }
        public static event Action<InputDevice> deviceConnected;
        public static event Action<InputDevice> deviceDisconnected;
    }

    public enum XRNode { LeftEye, RightEye, CenterEye, Head, LeftHand, RightHand, GameController, TrackingReference, HardwareTracker }

    public enum TrackingOriginModeFlags { Unknown = 0, Device = 1, Floor = 2, TrackingReference = 4, Unbounded = 8 }
}

namespace UnityEngine.XR.Management
{
    public class XRManagerSettings : ScriptableObject
    {
        public bool isInitializationComplete { get { return false; } }
        public void InitializeLoaderSync() { }
        public void StartSubsystems() { }
        public void StopSubsystems() { }
        public void DeinitializeLoader() { }
    }

    public class XRGeneralSettings : ScriptableObject
    {
        public static XRGeneralSettings Instance { get { return null; } }
        public XRManagerSettings Manager { get { return null; } }
    }
}
