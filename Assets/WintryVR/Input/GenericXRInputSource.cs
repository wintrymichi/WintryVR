using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using WintryVR.Core;

namespace WintryVR.Interaction
{
    /// <summary>
    /// Vendor-neutral controller input through UnityEngine.XR.InputDevices (works with the Oculus XR plugin
    /// and OpenXR without the Meta SDK): trigger = select, grip = push-to-talk, menu/secondary button = menu,
    /// primary button = dismiss, both grips = debug.
    /// </summary>
    public class GenericXRInputSource : IXRInputSource
    {
        private InputDevice _right, _left;
        private readonly Transform _trackingSpace;
        private float _lastScan;

        public GenericXRInputSource(Transform trackingSpace) { _trackingSpace = trackingSpace; }
        public string Name => "GenericXRControllers";
        public InputModality Modality => InputModality.Controllers;
        public int Priority => 5;
        public bool IsActive => _right.isValid || _left.isValid;

        public void Poll()
        {
            if (Time.time - _lastScan < 1f && (_right.isValid || _left.isValid)) return;
            _lastScan = Time.time;
            _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            _left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        public bool TryGetPointer(out Ray ray, out bool isLeft)
        {
            isLeft = false;
            var dev = _right.isValid ? _right : _left;
            isLeft = !_right.isValid;
            if (dev.isValid && dev.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) && dev.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                if (_trackingSpace != null) { pos = _trackingSpace.TransformPoint(pos); rot = _trackingSpace.rotation * rot; }
                ray = new Ray(pos, rot * Vector3.forward);
                return true;
            }
            ray = default; return false;
        }

        private static bool Button(InputDevice d, InputFeatureUsage<bool> usage) => d.isValid && d.TryGetFeatureValue(usage, out bool v) && v;
        private static bool Axis(InputDevice d, InputFeatureUsage<float> usage, float threshold) => d.isValid && d.TryGetFeatureValue(usage, out float v) && v > threshold;

        public bool Select(bool left) => Button(left ? _left : _right, CommonUsages.triggerButton) || Axis(left ? _left : _right, CommonUsages.trigger, 0.7f);
        public bool PushToTalk() => Button(_right, CommonUsages.gripButton) || Axis(_right, CommonUsages.grip, 0.7f) || Button(_left, CommonUsages.gripButton) || Axis(_left, CommonUsages.grip, 0.7f);
        public bool Menu() => Button(_right, CommonUsages.secondaryButton) || Button(_left, CommonUsages.secondaryButton) || Button(_left, CommonUsages.menuButton);
        public bool Secondary() => Button(_right, CommonUsages.primaryButton);
        public bool DebugCombo() => Button(_left, CommonUsages.primaryButton) && Button(_right, CommonUsages.primaryButton);
    }
}
