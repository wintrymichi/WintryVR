using UnityEngine;
using UnityEngine.XR;
using WintryVR.Core;

namespace WintryVR.MR
{
    /// <summary>
    /// Vendor-neutral rig: a camera driven by XR head pose (UnityEngine.XR.InputDevices) with a transparent
    /// clear colour ready for passthrough composition, or a neutral grey background in the editor. Passthrough
    /// itself needs a vendor layer (Meta provider) – this rig reports it as unsupported so features fall back.
    /// </summary>
    public class GenericMRRigProvider : IMRRigProvider
    {
        public string Name => "GenericXR";
        public bool IsAvailable => true;

        public MRRigHandles Build(Transform parent, DeviceCapabilities caps)
        {
            var root = new GameObject("XRRig").transform;
            root.SetParent(parent, false);
            var tracking = new GameObject("TrackingSpace").transform;
            tracking.SetParent(root, false);
            var head = new GameObject("CenterEye").transform;
            head.SetParent(tracking, false);
            head.localPosition = new Vector3(0f, caps.IsEditor ? 1.6f : 0f, 0f);

            var cam = head.gameObject.AddComponent<UnityEngine.Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = caps.IsEditor ? new Color(0.18f, 0.19f, 0.22f, 1f) : new Color(0f, 0f, 0f, 0f);
            head.gameObject.AddComponent<AudioListener>();
            if (!caps.IsEditor) head.gameObject.AddComponent<GenericHeadTracker>();

            return new MRRigHandles { RigRoot = root, TrackingSpace = tracking, Head = head, CenterCamera = cam, Passthrough = new NoPassthroughService(caps.IsEditor), ProviderName = Name };
        }
    }

    /// <summary>Applies the XR centre-eye pose to the camera (no vendor SDK needed).</summary>
    public class GenericHeadTracker : MonoBehaviour
    {
        private InputDevice _hmd;
        private void Update()
        {
            if (!_hmd.isValid) _hmd = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!_hmd.isValid) return;
            if (_hmd.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 pos)) transform.localPosition = pos;
            if (_hmd.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rot)) transform.localRotation = rot;
        }
    }

    /// <summary>Passthrough stand-in when no vendor layer is available.</summary>
    public class NoPassthroughService : IPassthroughService
    {
        private readonly bool _editor;
        public NoPassthroughService(bool editor) { _editor = editor; }
        public bool IsSupported => false;
        public bool IsEnabled => _editor; // the editor grey background plays the role of the room
        public void SetEnabled(bool enabled) { }
        public void SetBrightness(float brightness) { }
        public void SetEdgeHighlight(bool enabled, Color color) { }
    }
}
