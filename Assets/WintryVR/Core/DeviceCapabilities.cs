using System;
using System.Text;
using UnityEngine;

namespace WintryVR.Core
{
    /// <summary>
    /// What this headset/session can actually do. Filled by the bootstrap from the platform layer
    /// (WintryVR.Meta when the Meta XR SDK is present) plus generic Unity probes. Nothing is assumed:
    /// every consumer checks a flag and uses a fallback when it is false.
    /// </summary>
    [Serializable]
    public class DeviceCapabilities
    {
        public string HeadsetName = "Unknown";
        public bool IsQuest3;
        public bool IsQuest3S;
        public bool IsEditor;
        public bool XRRuntimeActive;

        public bool Passthrough;
        public bool HandTracking;
        public bool Controllers;
        public bool SpatialAnchors;
        public bool AnchorPersistence;
        public bool SceneUnderstanding;   // MRUK room model (planes/volumes)
        public bool SceneMesh;            // global mesh
        public bool EyeTracking;          // Quest 3/3S do not have eye tracking; kept for future hardware
        public bool Microphone;
        public bool PassthroughCameraAccess;  // Passthrough Camera API (HorizonOS v74+ with permission)
        public bool SpatialAudio;
        public bool Location;
        public bool Network;

        public string Notes = "";

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.Append(HeadsetName);
            if (IsEditor) sb.Append(" (Editor)");
            sb.Append(" | PT:").Append(Passthrough ? "Y" : "N");
            sb.Append(" Hands:").Append(HandTracking ? "Y" : "N");
            sb.Append(" Ctrl:").Append(Controllers ? "Y" : "N");
            sb.Append(" Anchors:").Append(SpatialAnchors ? (AnchorPersistence ? "Y+" : "Y") : "N");
            sb.Append(" Scene:").Append(SceneUnderstanding ? "Y" : "N");
            sb.Append(" Mesh:").Append(SceneMesh ? "Y" : "N");
            sb.Append(" Mic:").Append(Microphone ? "Y" : "N");
            sb.Append(" Cam:").Append(PassthroughCameraAccess ? "Y" : "N");
            sb.Append(" Net:").Append(Network ? "Y" : "N");
            return sb.ToString();
        }

        /// <summary>Generic probe that works without any vendor SDK. Platform layers refine it.</summary>
        public static DeviceCapabilities ProbeGeneric()
        {
            var c = new DeviceCapabilities();
            c.IsEditor = Application.isEditor;
            c.HeadsetName = Application.isEditor ? "Editor" : SystemInfo.deviceModel;
            c.Microphone = Microphone.devices != null && Microphone.devices.Length > 0;
            c.Network = Application.internetReachability != NetworkReachability.NotReachable;
            c.SpatialAudio = true; // Unity AudioSource spatialisation always available
            c.XRRuntimeActive = UnityEngine.XR.XRSettings.isDeviceActive;
            string model = (SystemInfo.deviceModel ?? "").ToLowerInvariant();
            if (model.Contains("quest 3s") || model.Contains("panther")) { c.IsQuest3S = true; c.HeadsetName = "Meta Quest 3S"; }
            else if (model.Contains("quest 3") || model.Contains("eureka")) { c.IsQuest3 = true; c.HeadsetName = "Meta Quest 3"; }
            c.Location = UnityEngine.Input.location != null;
            return c;
        }
    }

    /// <summary>Optional platform probe registered by a vendor assembly.</summary>
    public interface ICapabilityProbe
    {
        void Refine(DeviceCapabilities capabilities);
    }
}
