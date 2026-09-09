using System;
using System.Collections.Generic;
using UnityEngine;

namespace WintryVR.Core
{
    /// <summary>A single detection returned by the vision pipeline.</summary>
    [Serializable]
    public class Detection
    {
        public string Label;            // generic class: "camera", "phone"
        public string Identity;         // specific identity when known: "Canon EOS R6 Mark II"
        public string Category;         // "electronics", "furniture", "text", ...
        public float Confidence;        // 0..1
        public Rect NormalizedBounds;   // in image space, 0..1, origin top-left
        public string Description;
        public List<string> Attributes = new List<string>();
        public bool HasBounds => NormalizedBounds.width > 0 && NormalizedBounds.height > 0;
    }

    /// <summary>An object Wintry has seen and localised in space. Stored in context memory.</summary>
    [Serializable]
    public class ObservedObject
    {
        public string Id;
        public string Label;
        public string Identity;
        public string Category;
        public float Confidence;
        public string Description;
        public Vector3 WorldPosition;
        public Vector3 Extents = new Vector3(0.15f, 0.15f, 0.15f);
        public string AnchorId;              // optional spatial anchor
        public string SupportingSurface;     // "table", "floor", ...
        public DateTime ObservedAt;
        public List<string> Attributes = new List<string>();
        public Dictionary<string, string> Facts = new Dictionary<string, string>();
        public bool HasPosition;

        public string DisplayName => string.IsNullOrEmpty(Identity) ? Label : Identity;
    }

    /// <summary>Recognised text block from OCR.</summary>
    [Serializable]
    public class TextBlock
    {
        public string Text;
        public string LanguageCode;
        public float Confidence;
        public Rect NormalizedBounds;
        public Vector3 WorldPosition;
        public bool HasPosition;
    }

    public class OcrResult
    {
        public bool Success;
        public string FullText = "";
        public string LanguageCode = "";
        public List<TextBlock> Blocks = new List<TextBlock>();
        public string UserMessage = ""; // friendly message if !Success
    }

    public class TranslationResult
    {
        public bool Success;
        public string Original;
        public string Translated;
        public string SourceLanguage;
        public string TargetLanguage;
        public string UserMessage = "";
    }

    public class VisionResult
    {
        public bool Success;
        public string SceneSummary = "";
        public List<Detection> Detections = new List<Detection>();
        public string UserMessage = "";
        public float LatencyMs;
    }

    public class SearchResultItem
    {
        public string Title;
        public string Snippet;
        public string Url;
        public string Source;
        public string Price;
        public string Availability;
    }

    public class SearchResult
    {
        public bool Success;
        public string Query;
        public List<SearchResultItem> Items = new List<SearchResultItem>();
        public string Summary = "";
        public string UserMessage = "";
    }

    /// <summary>A captured frame from the headset cameras (or a demo/fallback source).</summary>
    public class CapturedFrame
    {
        public byte[] JpegBytes;
        public int Width;
        public int Height;
        public DateTime CapturedAt;
        public Pose CameraPose;      // world pose of the camera at capture time
        public float VerticalFovDegrees = 72f;
        public float HorizontalFovDegrees = 90f;
        public string Source;        // "passthrough-camera", "render", "demo"
        public bool IsValid => JpegBytes != null && JpegBytes.Length > 0;

        /// <summary>Converts a normalised image point into a world-space ray using the capture pose and FOV.</summary>
        public Ray ToWorldRay(Vector2 normalizedPoint)
        {
            float nx = (normalizedPoint.x - 0.5f) * 2f;
            float ny = (0.5f - normalizedPoint.y) * 2f;
            float tanH = Mathf.Tan(HorizontalFovDegrees * 0.5f * Mathf.Deg2Rad);
            float tanV = Mathf.Tan(VerticalFovDegrees * 0.5f * Mathf.Deg2Rad);
            Vector3 dirLocal = new Vector3(nx * tanH, ny * tanV, 1f).normalized;
            Vector3 dirWorld = CameraPose.rotation * dirLocal;
            return new Ray(CameraPose.position, dirWorld);
        }
    }

    /// <summary>One conversational exchange.</summary>
    [Serializable]
    public class AssistantTurn
    {
        public string UserText;
        public string AssistantText;
        public Intent Intent;
        public string LanguageCode;
        public float Confidence = 1f;
        public bool UsedVision;
        public bool UsedSearch;
        public bool Offline;
        public DateTime Time;
        public List<string> ReferencedObjectIds = new List<string>();
        public InformationCardData Card;
    }

    /// <summary>Data used to build a floating information card.</summary>
    [Serializable]
    public class InformationCardData
    {
        public string Id;
        public string Title;
        public string Subtitle;
        public List<string> Lines = new List<string>();
        public string ActionLabel = "Tell me more";
        public string ActionQuery;
        public Vector3 WorldPosition;
        public bool HasWorldPosition;
        public string ObjectId;
        public string SourceUrl;
    }

    /// <summary>Structured answer requested from the AI when identifying things.</summary>
    public class StructuredAnswer
    {
        public string Speech = "";          // what to say aloud
        public string ObjectName = "";      // identified object (if any)
        public string ObjectCategory = "";
        public float Confidence = -1f;      // -1 when N/A
        public List<string> CardLines = new List<string>();
        public string CardTitle = "";
        public string CardSubtitle = "";
        public bool NeedsSearch;
        public string SearchQuery = "";
        public string LanguageCode = "";
        public Vector2 FocusPoint = new Vector2(0.5f, 0.5f);
        public bool HasFocusPoint;
        public string TranslatedText = "";
        public string DetectedText = "";
        public string SourceLanguage = "";
    }
}
