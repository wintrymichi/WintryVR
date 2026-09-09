// Additional faithful-signature UnityEngine stubs.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class TextMesh : Component
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public float characterSize { get; set; }
        public float lineSpacing { get; set; }
        public float tabSize { get; set; }
        public float offsetZ { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
        public Color color { get; set; }
        public bool richText { get; set; }
    }

    public enum TextAlignment { Left, Center, Right }

    public class LODGroup : Component
    {
        public float size { get; set; }
        public int lodCount { get { return 0; } }
        public Vector3 localReferencePoint { get; set; }
        public bool enabled { get; set; }
        public bool animateCrossFading { get; set; }
        public float crossFadeAnimationDuration { get; set; }
        public LODFadeMode fadeMode { get; set; }
        public LOD[] GetLODs() { return null; }
        public void SetLODs(LOD[] lods) { }
        public void RecalculateBounds() { }
        public void ForceLOD(int index) { }
    }

    public struct LOD
    {
        public LOD(float screenRelativeTransitionHeight, Renderer[] renderers) { }
        public float screenRelativeTransitionHeight { get { return 0f; } set { } }
        public float fadeTransitionWidth { get { return 0f; } set { } }
        public Renderer[] renderers { get { return null; } set { } }
    }

    public enum LODFadeMode { None, CrossFade, SpeedTree }

    public enum BatteryStatus { Unknown, Charging, Discharging, NotCharging, Full }

    public struct FrameTiming
    {
        public double cpuFrameTime { get { return 0d; } }
        public double cpuMainThreadFrameTime { get { return 0d; } }
        public double cpuRenderThreadFrameTime { get { return 0d; } }
        public double gpuFrameTime { get { return 0d; } }
        public ulong heightScale { get { return 0UL; } }
        public ulong widthScale { get { return 0UL; } }
    }

    public static class FrameTimingManager
    {
        public static void CaptureFrameTimings() { }
        public static uint GetLatestTimings(uint numFrames, FrameTiming[] timings) { return 0u; }
        public static float GetVSyncsPerSecond() { return 0f; }
        public static ulong GetGpuTimerFrequency() { return 0UL; }
    }
}

namespace UnityEngine.Networking
{
    public interface IMultipartFormSection
    {
        string sectionName { get; }
        byte[] sectionData { get; }
        string fileName { get; }
        string contentType { get; }
    }

    public class MultipartFormDataSection : IMultipartFormSection
    {
        public MultipartFormDataSection(string name, byte[] data, string contentType) { }
        public MultipartFormDataSection(string name, byte[] data) { }
        public MultipartFormDataSection(string name, string data) { }
        public MultipartFormDataSection(string name, string data, System.Text.Encoding encoding, string contentType) { }
        public MultipartFormDataSection(byte[] data) { }
        public string sectionName { get { return ""; } }
        public byte[] sectionData { get { return null; } }
        public string fileName { get { return null; } }
        public string contentType { get { return null; } }
    }

    public class MultipartFormFileSection : IMultipartFormSection
    {
        public MultipartFormFileSection(string name, byte[] data, string fileName, string contentType) { }
        public MultipartFormFileSection(string fileName, byte[] data) { }
        public MultipartFormFileSection(byte[] data) { }
        public string sectionName { get { return ""; } }
        public byte[] sectionData { get { return null; } }
        public string fileName { get { return null; } }
        public string contentType { get { return null; } }
    }
}
