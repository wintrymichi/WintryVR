#if WINTRY_META_XR
using UnityEngine;
using WintryVR.Core;
using WintryVR.MR;

namespace WintryVR.MetaPlatform
{
    /// <summary>
    /// Builds the OVRCameraRig + OVRManager + OVRPassthroughLayer at runtime (no prefab dependency).
    /// The centre-eye camera clears to transparent black so the passthrough underlay shows through.
    /// </summary>
    public class MetaMRRigProvider : IMRRigProvider
    {
        public string Name => "MetaXR";
        public bool IsAvailable => true;

        public MRRigHandles Build(Transform parent, DeviceCapabilities caps)
        {
            var rigGo = new GameObject("OVRCameraRig");
            rigGo.transform.SetParent(parent, false);
            var rig = rigGo.AddComponent<OVRCameraRig>();
            var manager = rigGo.AddComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            manager.isInsightPassthroughEnabled = true;
            rig.EnsureGameObjectIntegrity();

            var head = rig.centerEyeAnchor;
            var cam = head.GetComponent<Camera>();
            if (cam == null) cam = head.gameObject.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 50f;
            if (head.GetComponent<AudioListener>() == null) head.gameObject.AddComponent<AudioListener>();

            var passthrough = rigGo.AddComponent<OVRPassthroughLayer>();
            passthrough.overlayType = OVROverlay.OverlayType.Underlay;
            passthrough.textureOpacity = 1f;
            passthrough.edgeRenderingEnabled = false;

            // hands: OVRHand on the hand anchors so the input source can read pinches and pointer poses
            var lh = rig.leftHandAnchor.gameObject.AddComponent<OVRHand>(); lh.HandType = OVRHand.Hand.HandLeft;
            var rh = rig.rightHandAnchor.gameObject.AddComponent<OVRHand>(); rh.HandType = OVRHand.Hand.HandRight;

            return new MRRigHandles
            {
                RigRoot = rigGo.transform, TrackingSpace = rig.trackingSpace, Head = head, CenterCamera = cam,
                Passthrough = new MetaPassthroughService(passthrough, manager), ProviderName = Name
            };
        }
    }

    public class MetaPassthroughService : IPassthroughService
    {
        private readonly OVRPassthroughLayer _layer;
        private readonly OVRManager _manager;
        public MetaPassthroughService(OVRPassthroughLayer layer, OVRManager manager) { _layer = layer; _manager = manager; }
        public bool IsSupported => OVRManager.IsInsightPassthroughSupported();
        public bool IsEnabled => _layer != null && _layer.enabled && _manager.isInsightPassthroughEnabled;
        public void SetEnabled(bool enabled) { if (_layer != null) _layer.enabled = enabled; _manager.isInsightPassthroughEnabled = enabled; }
        public void SetBrightness(float brightness) { if (_layer != null) _layer.SetBrightnessContrastSaturation(Mathf.Clamp(brightness, -1f, 1f), 0f, 0f); }
        public void SetEdgeHighlight(bool enabled, Color color) { if (_layer == null) return; _layer.edgeRenderingEnabled = enabled; _layer.edgeColor = color; }
    }
}
#endif
