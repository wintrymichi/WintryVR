using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Networking;
using WintryVR.Settings;

namespace WintryVR.OCR
{
    /// <summary>
    /// Object recognition through the multimodal AI provider. Asks for a structured list of detections with
    /// normalised bounding boxes and confidences; results are then localised in 3D by <see cref="ObjectLocalizer"/>.
    /// A dedicated on-device detector can be plugged in by registering another IVisionService.
    /// </summary>
    public class VisionService : IVisionService
    {
        private readonly IAIProvider _provider;
        public VisionService(IAIProvider provider) { _provider = provider; }
        public bool IsAvailable => _provider != null && _provider.IsConfigured && _provider.SupportsVision && WintrySettings.Current.Vision.ObjectRecognition;

        public async Task<VisionResult> AnalyzeAsync(CapturedFrame frame, string hint, string languageCode, CancellationToken ct)
        {
            var result = new VisionResult();
            if (frame == null || !frame.IsValid) { result.UserMessage = Localization.Get("err.noCamera", languageCode); return result; }
            if (!IsAvailable) { result.UserMessage = Localization.Get("err.generic", languageCode); return result; }

            var req = new AIRequest
            {
                SystemPrompt = "You are a precise computer-vision analyst for a mixed-reality assistant. Analyse the image and return JSON only: " +
                               "{\"scene\": short description, \"objects\": [{\"label\": generic class (english, lowercase), \"identity\": specific make/model/title if recognisable else empty, " +
                               "\"category\": one of electronics|furniture|food|plant|animal|book|document|sign|clothing|tool|vehicle|art|person|other, \"confidence\": 0..1, " +
                               "\"x\": left 0..1, \"y\": top 0..1, \"w\": width 0..1, \"h\": height 0..1, \"attributes\": [short strings]}]}. " +
                               "List at most 8 salient objects. Never invent identities; leave identity empty when unsure and lower confidence accordingly. Do not identify people by name.",
                MaxTokens = 700,
                Temperature = 0.1f,
                ExpectJson = true
            };
            var msg = new AIMessage { Role = AIRole.User };
            msg.Parts.Add(AIContentPart.FromText("[vision] " + (string.IsNullOrEmpty(hint) ? "Analyse what the user is looking at. The centre of the image is the user's focus." : hint)));
            msg.Parts.Add(AIContentPart.FromJpeg(frame.JpegBytes));
            req.Messages.Add(msg);

            float t0 = Time.realtimeSinceStartup;
            var res = await _provider.CompleteAsync(req, ct);
            result.LatencyMs = (Time.realtimeSinceStartup - t0) * 1000f;
            if (!res.Success) { result.UserMessage = Localization.Get("err.identify", languageCode); return result; }

            var obj = MiniJson.ExtractObject(res.Text);
            if (obj == null) { result.UserMessage = Localization.Get("err.identify", languageCode); return result; }
            result.SceneSummary = MiniJson.GetString(obj, "scene", "") ?? "";
            var arr = MiniJson.GetArray(obj, "objects");
            float threshold = WintrySettings.Current.Vision.ConfidenceThreshold * 0.5f; // keep low ones, the UI hedges
            if (arr != null)
            {
                foreach (var o in arr)
                {
                    var d = new Detection
                    {
                        Label = MiniJson.GetString(o, "label", "object"),
                        Identity = MiniJson.GetString(o, "identity", ""),
                        Category = MiniJson.GetString(o, "category", "other"),
                        Confidence = Mathf.Clamp01((float)MiniJson.GetNumber(o, "confidence", 0.5)),
                        Description = MiniJson.GetString(o, "description", "")
                    };
                    float x = (float)MiniJson.GetNumber(o, "x", -1), y = (float)MiniJson.GetNumber(o, "y", -1);
                    float w = (float)MiniJson.GetNumber(o, "w", 0), h = (float)MiniJson.GetNumber(o, "h", 0);
                    if (x >= 0 && y >= 0 && w > 0 && h > 0) d.NormalizedBounds = new Rect(Mathf.Clamp01(x), Mathf.Clamp01(y), Mathf.Clamp01(w), Mathf.Clamp01(h));
                    var attrs = MiniJson.GetArray(o, "attributes");
                    if (attrs != null) foreach (var a in attrs) if (a != null) d.Attributes.Add(a.ToString());
                    if (d.Confidence >= threshold) result.Detections.Add(d);
                }
            }
            result.Detections.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
            result.Success = true;
            return result;
        }
    }

    /// <summary>
    /// Turns a 2D detection into a 3D position: image point → world ray (from the capture pose) → raycast
    /// against room geometry; falls back to a plausible distance when nothing is hit.
    /// </summary>
    public static class ObjectLocalizer
    {
        public const float DefaultDistance = 1.2f;
        public const float MaxDistance = 6f;

        public static Vector3 Localize(CapturedFrame frame, Vector2 normalizedPoint, ISceneUnderstandingService scene, out bool hitGeometry)
        {
            hitGeometry = false;
            if (frame == null) return Vector3.zero;
            Ray ray = frame.ToWorldRay(normalizedPoint);
            if (scene != null && scene.Raycast(ray, MaxDistance, out RaycastHit hit))
            {
                hitGeometry = true;
                return hit.point + hit.normal * 0.02f;
            }
            if (Physics.Raycast(ray, out RaycastHit ph, MaxDistance))
            {
                hitGeometry = true;
                return ph.point + ph.normal * 0.02f;
            }
            return ray.GetPoint(DefaultDistance);
        }

        public static Vector3 Localize(CapturedFrame frame, Detection d, ISceneUnderstandingService scene, out bool hitGeometry)
        {
            Vector2 c = d.HasBounds ? d.NormalizedBounds.center : new Vector2(0.5f, 0.5f);
            return Localize(frame, c, scene, out hitGeometry);
        }
    }
}
