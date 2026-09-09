// Additional faithful-signature Unity stubs (round 2).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public static class ImageConversion
    {
        public static byte[] EncodeToPNG(this Texture2D tex) { return null; }
        public static byte[] EncodeToJPG(this Texture2D tex) { return null; }
        public static byte[] EncodeToJPG(this Texture2D tex, int quality) { return null; }
        public static byte[] EncodeToEXR(this Texture2D tex) { return null; }
        public static byte[] EncodeToTGA(this Texture2D tex) { return null; }
        public static bool LoadImage(this Texture2D tex, byte[] data) { return false; }
        public static bool LoadImage(this Texture2D tex, byte[] data, bool markNonReadable) { return false; }
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string htmlString, out Color color) { color = default(Color); return false; }
        public static string ToHtmlStringRGB(Color color) { return ""; }
        public static string ToHtmlStringRGBA(Color color) { return ""; }
    }

    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }

    public static class RenderSettings
    {
        public static UnityEngine.Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static float ambientIntensity { get; set; }
        public static Material skybox { get; set; }
        public static bool fog { get; set; }
        public static Color fogColor { get; set; }
        public static FogMode fogMode { get; set; }
        public static float fogDensity { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
        public static Light sun { get; set; }
        public static float reflectionIntensity { get; set; }
        public static UnityEngine.Rendering.DefaultReflectionMode defaultReflectionMode { get; set; }
        public static Cubemap customReflection { get; set; }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static int antiAliasing { get; set; }
        public static ShadowQuality shadows { get; set; }
        public static ShadowResolution shadowResolution { get; set; }
        public static float shadowDistance { get; set; }
        public static int pixelLightCount { get; set; }
        public static float lodBias { get; set; }
        public static int masterTextureLimit { get; set; }
        public static SkinWeights skinWeights { get; set; }
        public static bool realtimeReflectionProbes { get; set; }
        public static bool softParticles { get; set; }
        public static AnisotropicFiltering anisotropicFiltering { get; set; }
        public static int GetQualityLevel() { return 0; }
        public static void SetQualityLevel(int index) { }
        public static void SetQualityLevel(int index, bool applyExpensiveChanges) { }
        public static string[] names { get { return null; } }
        public static UnityEngine.Rendering.RenderPipelineAsset renderPipeline { get; set; }
    }

    public enum ShadowQuality { Disable, HardOnly, All }
    public enum ShadowResolution { Low, Medium, High, VeryHigh }
    public enum SkinWeights { OneBone, TwoBones, FourBones, Unlimited }
    public enum AnisotropicFiltering { Disable, Enable, ForceEnable }

    public class LocationService
    {
        public bool isEnabledByUser { get { return false; } }
        public LocationServiceStatus status { get { return default(LocationServiceStatus); } }
        public LocationInfo lastData { get { return default(LocationInfo); } }
        public void Start() { }
        public void Start(float desiredAccuracyInMeters) { }
        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters) { }
        public void Stop() { }
    }

    public enum LocationServiceStatus { Stopped, Initializing, Running, Failed }

    public class Compass
    {
        public bool enabled { get; set; }
        public float magneticHeading { get { return 0f; } }
        public float trueHeading { get { return 0f; } }
        public float headingAccuracy { get { return 0f; } }
        public Vector3 rawVector { get { return default(Vector3); } }
        public double timestamp { get { return 0d; } }
    }

    public struct LocationInfo
    {
        public float latitude { get { return 0f; } }
        public float longitude { get { return 0f; } }
        public float altitude { get { return 0f; } }
        public float horizontalAccuracy { get { return 0f; } }
        public float verticalAccuracy { get { return 0f; } }
        public double timestamp { get { return 0d; } }
    }

    public enum MaterialGlobalIlluminationFlags
    {
        None = 0, RealtimeEmissive = 1, BakedEmissive = 2,
        EmissiveIsBlack = 4, AnyEmissive = 3
    }
}

namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public enum DefaultReflectionMode { Skybox, Custom }
    public enum RenderQueue
    {
        Background = 1000, Geometry = 2000, AlphaTest = 2450,
        GeometryLast = 2500, Transparent = 3000, Overlay = 4000
    }
}
