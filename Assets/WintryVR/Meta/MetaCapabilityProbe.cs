#if WINTRY_META_XR
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.MetaPlatform
{
    /// <summary>Refines capability detection with OVRPlugin/OVRManager facts (Quest 3 vs 3S, passthrough, hands).</summary>
    public class MetaCapabilityProbe : ICapabilityProbe
    {
        public void Refine(DeviceCapabilities c)
        {
            try
            {
                string headset = OVRPlugin.GetSystemHeadsetType().ToString();
                c.IsQuest3S = headset.Contains("Quest_3S");
                c.IsQuest3 = !c.IsQuest3S && headset.Contains("Quest_3");
                if (c.IsQuest3S) c.HeadsetName = "Meta Quest 3S";
                else if (c.IsQuest3) c.HeadsetName = "Meta Quest 3";
                else if (headset != "None") c.HeadsetName = headset.Replace('_', ' ');
                c.Passthrough = OVRManager.IsInsightPassthroughSupported();
                c.HandTracking = OVRPlugin.GetHandTrackingEnabled();
                c.Controllers = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) || OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) || Application.isEditor;
                c.SpatialAnchors = !Application.isEditor;
                c.AnchorPersistence = !Application.isEditor;
                c.EyeTracking = false; // Quest 3 / 3S have no eye tracking
                c.PassthroughCameraAccess = !Application.isEditor && WebCamTexture.devices != null && WebCamTexture.devices.Length > 0;
                c.Notes = "Meta XR probe: " + headset;
            }
            catch (System.Exception ex) { WintryLog.W("Meta", "Capability probe failed: " + ex.Message); }
        }
    }
}
#endif
