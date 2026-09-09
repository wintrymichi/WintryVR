using System;
using UnityEngine;

namespace WintryVR.Core
{
    public enum InputModality { None, Hands, Controllers, Gaze, Editor }

    public enum GestureType { PinchSelect, PinchHoldStart, PinchHoldEnd, Point, PalmMenu, PushToTalkStart, PushToTalkEnd, Dismiss, DebugToggle }

    public struct GestureEvent
    {
        public GestureType Type;
        public Ray PointerRay;
        public bool IsLeftHand;
        public Vector3 Position;
    }

    public interface IInputService
    {
        InputModality ActiveModality { get; }
        bool HandsTracked { get; }
        bool ControllersConnected { get; }
        Ray PrimaryPointerRay { get; }
        bool PrimaryPointerValid { get; }
        bool IsPinching(bool leftHand);
        event Action<GestureEvent> OnGesture;
    }

    public interface IPassthroughService
    {
        bool IsSupported { get; }
        bool IsEnabled { get; }
        void SetEnabled(bool enabled);
        void SetBrightness(float brightness); // -1..1
        void SetEdgeHighlight(bool enabled, Color color);
    }

    public interface ILocationService
    {
        bool IsEnabled { get; }
        bool HasFix { get; }
        double Latitude { get; }
        double Longitude { get; }
        void SetEnabled(bool enabled);
        string DescribeForContext();
    }
}
