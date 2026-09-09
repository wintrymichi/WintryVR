using System;
using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Interaction
{
    /// <summary>
    /// Unified input: picks the best active source each frame (hands > controllers > gaze/editor) and turns raw
    /// state into gestures: PinchSelect, PinchHoldStart/End (move UI), Point, PalmMenu, PushToTalkStart/End,
    /// Dismiss, DebugToggle. Hand tracking is never required: every gesture has a controller equivalent.
    /// </summary>
    public class InputService : MonoBehaviour, IInputService
    {
        public event Action<GestureEvent> OnGesture;
        public InputModality ActiveModality { get; private set; } = InputModality.None;
        public bool HandsTracked { get; private set; }
        public bool ControllersConnected { get; private set; }
        public Ray PrimaryPointerRay { get; private set; }
        public bool PrimaryPointerValid { get; private set; }
        public float HoldThresholdSeconds = 0.35f;

        private readonly List<IXRInputSource> _sources = new List<IXRInputSource>();
        private IXRInputSource _active;
        private bool _selectDownR, _selectDownL, _holdingR, _menuDown, _pttDown, _secDown, _dbgDown;
        private float _selectStartR;
        private Transform _head;

        public void Initialize(Transform head, IEnumerable<IXRInputSource> sources)
        {
            _head = head;
            foreach (var s in sources) AddSource(s);
        }

        public void AddSource(IXRInputSource source)
        {
            _sources.Add(source);
            _sources.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            WintryLog.I("Input", "Input source added: " + source.Name + " (" + source.Modality + ")");
        }

        public bool IsPinching(bool leftHand) => _active != null && _active.Select(leftHand);

        private void Update()
        {
            HandsTracked = false; ControllersConnected = false;
            IXRInputSource best = null;
            foreach (var s in _sources)
            {
                s.Poll();
                if (!s.IsActive) continue;
                if (s.Modality == InputModality.Hands) HandsTracked = true;
                if (s.Modality == InputModality.Controllers) ControllersConnected = true;
                if (best == null) best = s;
            }
            if (best != _active) { _active = best; ActiveModality = best != null ? best.Modality : InputModality.None; ResetEdges(); }
            if (_active == null)
            {
                // gaze fallback pointer
                PrimaryPointerValid = _head != null;
                if (_head != null) PrimaryPointerRay = new Ray(_head.position, _head.forward);
                return;
            }

            if (_active.TryGetPointer(out Ray ray, out bool left)) { PrimaryPointerRay = ray; PrimaryPointerValid = true; }
            else if (_head != null) { PrimaryPointerRay = new Ray(_head.position, _head.forward); PrimaryPointerValid = true; }
            else PrimaryPointerValid = false;

            // ---- select / hold (right or dominant)
            bool sel = _active.Select(false);
            if (sel && !_selectDownR) { _selectDownR = true; _selectStartR = Time.time; _holdingR = false; }
            else if (sel && _selectDownR && !_holdingR && Time.time - _selectStartR > HoldThresholdSeconds)
            {
                _holdingR = true; Emit(GestureType.PinchHoldStart, false);
            }
            else if (!sel && _selectDownR)
            {
                _selectDownR = false;
                if (_holdingR) { _holdingR = false; Emit(GestureType.PinchHoldEnd, false); }
                else Emit(GestureType.PinchSelect, false);
            }

            // ---- push-to-talk
            bool ptt = _active.PushToTalk();
            if (ptt && !_pttDown) { _pttDown = true; Emit(GestureType.PushToTalkStart, true); }
            else if (!ptt && _pttDown) { _pttDown = false; Emit(GestureType.PushToTalkEnd, true); }

            // ---- menu / secondary / debug (edge-triggered)
            bool menu = _active.Menu();
            if (menu && !_menuDown) { _menuDown = true; Emit(GestureType.PalmMenu, true); } else if (!menu) _menuDown = false;
            bool sec = _active.Secondary();
            if (sec && !_secDown) { _secDown = true; Emit(GestureType.Dismiss, false); } else if (!sec) _secDown = false;
            bool dbg = _active.DebugCombo();
            if (dbg && !_dbgDown) { _dbgDown = true; Emit(GestureType.DebugToggle, false); } else if (!dbg) _dbgDown = false;
        }

        private void ResetEdges() { _selectDownR = _selectDownL = _holdingR = _menuDown = _pttDown = _secDown = _dbgDown = false; }

        private void Emit(GestureType type, bool left)
        {
            var e = new GestureEvent { Type = type, PointerRay = PrimaryPointerRay, IsLeftHand = left, Position = PrimaryPointerRay.origin };
            try { OnGesture?.Invoke(e); }
            catch (Exception ex) { WintryLog.E("Input", "Gesture handler failed", ex); }
        }
    }
}
