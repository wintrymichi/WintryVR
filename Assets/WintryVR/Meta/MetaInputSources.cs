#if WINTRY_META_XR
using UnityEngine;
using WintryVR.Core;
using WintryVR.Interaction;

namespace WintryVR.MetaPlatform
{
    /// <summary>Hand tracking through OVRHand: index pinch = select, left pinch-hold = push-to-talk, palm toward face = menu.</summary>
    public class MetaHandInputSource : IXRInputSource
    {
        private OVRHand _left, _right;
        private Transform _head;
        private float _lastFind;

        public string Name => "MetaHands";
        public InputModality Modality => InputModality.Hands;
        public int Priority => 20;
        public bool IsActive => (_left != null && _left.IsTracked) || (_right != null && _right.IsTracked);

        public void Poll()
        {
            if ((_left == null || _right == null) && Time.time - _lastFind > 1f)
            {
                _lastFind = Time.time;
                foreach (var h in Object.FindObjectsByType<OVRHand>(FindObjectsSortMode.None))
                {
                    var side = h.GetHand();
                    if (side == OVRPlugin.Hand.HandLeft) _left = h; else if (side == OVRPlugin.Hand.HandRight) _right = h;
                }
                var cam = UnityEngine.Camera.main; if (cam != null) _head = cam.transform;
            }
        }

        public bool TryGetPointer(out Ray ray, out bool isLeft)
        {
            var hand = _right != null && _right.IsTracked && _right.IsPointerPoseValid ? _right : (_left != null && _left.IsTracked && _left.IsPointerPoseValid ? _left : null);
            isLeft = hand == _left;
            if (hand == null) { ray = default; return false; }
            ray = new Ray(hand.PointerPose.position, hand.PointerPose.forward);
            return true;
        }

        private static bool Pinch(OVRHand h) => h != null && h.IsTracked && h.GetFingerIsPinching(OVRHand.HandFinger.Index);
        public bool Select(bool left) => Pinch(left ? _left : _right);
        public bool PushToTalk() => Pinch(_left) && !Pinch(_right);
        public bool Menu()
        {
            // palm facing the head (either hand) with a middle-finger pinch: an explicit, low-false-positive gesture
            foreach (var h in new[] { _left, _right })
            {
                if (h == null || !h.IsTracked || _head == null) continue;
                Vector3 palmNormal = h.transform.up * (h == _left ? 1f : -1f);
                bool facing = Vector3.Dot(palmNormal, (_head.position - h.transform.position).normalized) > 0.55f;
                if (facing && h.GetFingerIsPinching(OVRHand.HandFinger.Middle)) return true;
            }
            return false;
        }
        public bool Secondary() => _right != null && _right.IsTracked && _right.GetFingerIsPinching(OVRHand.HandFinger.Ring);
        public bool DebugCombo() => Pinch(_left) && Pinch(_right) && _left.GetFingerIsPinching(OVRHand.HandFinger.Middle);
    }

    /// <summary>Touch controllers through OVRInput: trigger = select, grip = push-to-talk, B/Y = menu, A = dismiss, both thumbsticks = debug.</summary>
    public class MetaControllerInputSource : IXRInputSource
    {
        private OVRCameraRig _rig;
        private float _lastFind;
        public string Name => "MetaControllers";
        public InputModality Modality => InputModality.Controllers;
        public int Priority => 10;
        public bool IsActive => OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) || OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);

        public void Poll()
        {
            if (_rig == null && Time.time - _lastFind > 1f) { _lastFind = Time.time; _rig = Object.FindFirstObjectByType<OVRCameraRig>(); }
        }

        public bool TryGetPointer(out Ray ray, out bool isLeft)
        {
            bool right = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
            isLeft = !right;
            if (_rig == null) { ray = default; return false; }
            var anchor = right ? _rig.rightControllerAnchor : _rig.leftControllerAnchor;
            ray = new Ray(anchor.position, anchor.forward);
            return true;
        }

        public bool Select(bool left) => OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
        public bool PushToTalk() => OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) || OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        public bool Menu() => OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch) || OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch) || OVRInput.Get(OVRInput.Button.Start, OVRInput.Controller.LTouch);
        public bool Secondary() => OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
        public bool DebugCombo() => OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch) && OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
    }
}
#endif
