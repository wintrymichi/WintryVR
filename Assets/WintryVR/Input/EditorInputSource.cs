using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Interaction
{
    /// <summary>
    /// Desktop/editor input so the whole product can be developed without a headset:
    ///   mouse move (right button held) = look, mouse left click = select, hold = drag,
    ///   Space = push-to-talk, M = menu, Esc = dismiss, F1 = debug panel, WASD = move.
    /// </summary>
    public class EditorInputSource : IXRInputSource
    {
        private readonly Transform _head;
        private readonly Transform _rig;
        private float _yaw, _pitch;

        public EditorInputSource(Transform head, Transform rig) { _head = head; _rig = rig; }
        public string Name => "Editor";
        public InputModality Modality => InputModality.Editor;
        public int Priority => 0;
        public bool IsActive => Application.isEditor || !UnityEngine.XR.XRSettings.isDeviceActive;

        public void Poll()
        {
            if (_head == null) return;
            if (UnityEngine.Input.GetMouseButton(1))
            {
                _yaw += UnityEngine.Input.GetAxis("Mouse X") * 2.5f;
                _pitch = Mathf.Clamp(_pitch - UnityEngine.Input.GetAxis("Mouse Y") * 2.5f, -80f, 80f);
                _head.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
            if (_rig != null)
            {
                Vector3 move = Vector3.zero;
                if (UnityEngine.Input.GetKey(KeyCode.W)) move += _head.forward;
                if (UnityEngine.Input.GetKey(KeyCode.S)) move -= _head.forward;
                if (UnityEngine.Input.GetKey(KeyCode.A)) move -= _head.right;
                if (UnityEngine.Input.GetKey(KeyCode.D)) move += _head.right;
                move.y = 0;
                if (move.sqrMagnitude > 0) _rig.position += move.normalized * Time.deltaTime * 1.2f;
            }
        }

        public bool TryGetPointer(out Ray ray, out bool isLeft)
        {
            isLeft = false;
            var cam = UnityEngine.Camera.main;
            if (cam != null) { ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition); return true; }
            ray = new Ray(_head.position, _head.forward);
            return _head != null;
        }

        public bool Select(bool left) => !left && UnityEngine.Input.GetMouseButton(0);
        public bool PushToTalk() => UnityEngine.Input.GetKey(KeyCode.Space);
        public bool Menu() => UnityEngine.Input.GetKey(KeyCode.M);
        public bool Secondary() => UnityEngine.Input.GetKey(KeyCode.Escape);
        public bool DebugCombo() => UnityEngine.Input.GetKey(KeyCode.F1);
    }
}
