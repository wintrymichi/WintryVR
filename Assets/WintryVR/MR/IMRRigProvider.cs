using UnityEngine;
using WintryVR.Core;

namespace WintryVR.MR
{
    /// <summary>Result of building the XR camera rig.</summary>
    public class MRRigHandles
    {
        public Transform RigRoot;
        public Transform TrackingSpace;
        public Transform Head;               // centre eye
        public UnityEngine.Camera CenterCamera;
        public IPassthroughService Passthrough;
        public string ProviderName;
    }

    /// <summary>Vendor rig builder (Meta: OVRCameraRig + OVRManager + OVRPassthroughLayer).</summary>
    public interface IMRRigProvider
    {
        string Name { get; }
        bool IsAvailable { get; }
        MRRigHandles Build(Transform parent, DeviceCapabilities caps);
    }
}
