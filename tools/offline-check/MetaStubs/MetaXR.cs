// Approximate stubs of the Meta XR SDK / MRUK surface, used ONLY to cross-check WintryVR's own types.
// Meta's real signatures vary by SDK version; nothing here should be treated as authoritative.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public static class OVRPlugin
{
    public enum SystemHeadset { None, Meta_Quest_3, Meta_Quest_3S, Oculus_Quest_2, Meta_Quest_Pro }
    public static SystemHeadset GetSystemHeadsetType() { return default(SystemHeadset); }
    public static bool GetHandTrackingEnabled() { return false; }
    public static bool GetEyeTrackingEnabled() { return false; }
    public static float systemDisplayFrequency { get { return 0f; } set { } }
    public static float[] systemDisplayFrequenciesAvailable { get { return null; } }
}

public class OVRManager : MonoBehaviour
{
    public enum TrackingOrigin { EyeLevel, FloorLevel, Stage, Stationary }
    public TrackingOrigin trackingOriginType { get; set; }
    public bool isInsightPassthroughEnabled { get; set; }
    public bool useRecommendedMSAALevel { get; set; }
    public static OVRManager instance { get { return null; } }
    public static bool IsInsightPassthroughSupported() { return false; }
    public static event Action InputFocusAcquired;
    public static event Action InputFocusLost;
}

public class OVRCameraRig : MonoBehaviour
{
    public Transform trackingSpace { get { return null; } }
    public Transform centerEyeAnchor { get { return null; } }
    public Transform leftEyeAnchor { get { return null; } }
    public Transform rightEyeAnchor { get { return null; } }
    public Transform leftHandAnchor { get { return null; } }
    public Transform rightHandAnchor { get { return null; } }
    public Transform leftControllerAnchor { get { return null; } }
    public Transform rightControllerAnchor { get { return null; } }
    public void EnsureGameObjectIntegrity() { }
}

public class OVROverlay : MonoBehaviour
{
    public enum OverlayType { None, Underlay, Overlay }
    public OverlayType currentOverlayType { get; set; }
}

public class OVRPassthroughLayer : MonoBehaviour
{
    public OVROverlay.OverlayType overlayType { get; set; }
    public float textureOpacity { get; set; }
    public bool edgeRenderingEnabled { get; set; }
    public Color edgeColor { get; set; }
    public bool hidden { get; set; }
    public void SetBrightnessContrastSaturation(float brightness, float contrast, float saturation) { }
    public void SetColorMapNone() { }
}

public class OVRHand : MonoBehaviour
{
    public enum Hand { None, HandLeft, HandRight }
    public enum HandFinger { Thumb, Index, Middle, Ring, Pinky }
    public enum TrackingConfidence { Low, High }
    public Hand HandType { get; set; }
    public bool IsTracked { get { return false; } }
    public bool IsDataHighConfidence { get { return false; } }
    public bool IsPointerPoseValid { get { return false; } }
    public Transform PointerPose { get { return null; } }
    public float HandScale { get { return 0f; } }
    public TrackingConfidence HandConfidence { get { return default(TrackingConfidence); } }
    public bool GetFingerIsPinching(HandFinger finger) { return false; }
    public float GetFingerPinchStrength(HandFinger finger) { return 0f; }
    public TrackingConfidence GetFingerConfidence(HandFinger finger) { return default(TrackingConfidence); }
}

public static class OVRInput
{
    [Flags]
    public enum Controller
    {
        None = 0, LTouch = 1, RTouch = 2, Touch = 3, LHand = 32, RHand = 64,
        Hands = 96, Active = 128, All = -1
    }

    [Flags]
    public enum Button
    {
        None = 0, One = 1, Two = 2, Three = 4, Four = 8, Start = 256, Back = 512,
        PrimaryIndexTrigger = 1024, PrimaryHandTrigger = 2048, PrimaryThumbstick = 4096,
        SecondaryIndexTrigger = 8192, SecondaryHandTrigger = 16384, Any = -1
    }

    public enum Axis1D { None, PrimaryIndexTrigger, PrimaryHandTrigger, SecondaryIndexTrigger, SecondaryHandTrigger }
    public enum Axis2D { None, PrimaryThumbstick, SecondaryThumbstick }
    public enum Touch { None, One, Two, PrimaryIndexTrigger, PrimaryThumbstick, Any }
    public enum Handedness { Unsupported, LeftHanded, RightHanded }

    public static bool IsControllerConnected(Controller controller) { return false; }
    public static Controller GetActiveController() { return default(Controller); }
    public static Controller GetConnectedControllers() { return default(Controller); }
    public static bool Get(Button virtualMask) { return false; }
    public static bool Get(Button virtualMask, Controller controller) { return false; }
    public static bool GetDown(Button virtualMask) { return false; }
    public static bool GetDown(Button virtualMask, Controller controller) { return false; }
    public static bool GetUp(Button virtualMask) { return false; }
    public static bool GetUp(Button virtualMask, Controller controller) { return false; }
    public static float Get(Axis1D virtualMask) { return 0f; }
    public static float Get(Axis1D virtualMask, Controller controller) { return 0f; }
    public static Vector2 Get(Axis2D virtualMask) { return default(Vector2); }
    public static Vector2 Get(Axis2D virtualMask, Controller controller) { return default(Vector2); }
    public static Vector3 GetLocalControllerPosition(Controller controllerType) { return default(Vector3); }
    public static Quaternion GetLocalControllerRotation(Controller controllerType) { return default(Quaternion); }
    public static void SetControllerVibration(float frequency, float amplitude, Controller controller) { }
    public static void Update() { }
}

public struct OVRResult
{
    public bool Success { get { return false; } }
    public int Status { get { return 0; } }
}

public struct OVRResult<T>
{
    public bool Success { get { return false; } }
    public T Value { get { return default(T); } }
}

public class OVRSpatialAnchor : MonoBehaviour
{
    public Guid Uuid { get { return default(Guid); } }
    public bool Created { get { return false; } }
    public bool Localized { get { return false; } }
    public bool PendingCreation { get { return false; } }
    public Task<OVRResult> SaveAnchorAsync() { return null; }
    public Task<OVRResult> EraseAnchorAsync() { return null; }

    public struct UnboundAnchor
    {
        public Guid Uuid { get { return default(Guid); } }
        public bool Localized { get { return false; } }
        public Task<bool> LocalizeAsync() { return null; }
        public Task<bool> LocalizeAsync(float timeout) { return null; }
        public void BindTo(OVRSpatialAnchor anchor) { }
        public bool TryGetPose(out Pose pose) { pose = default(Pose); return false; }
    }

    public static Task<OVRResult> LoadUnboundAnchorsAsync(IEnumerable<Guid> uuids, List<UnboundAnchor> results) { return null; }
    public static Task<OVRResult> SaveAnchorsAsync(IEnumerable<OVRSpatialAnchor> anchors) { return null; }
}

namespace Meta.XR.MRUtilityKit
{
    public class MRUK : MonoBehaviour
    {
        public static MRUK Instance { get { return null; } }
        public UnityEvent SceneLoadedEvent { get { return null; } }
        public UnityEvent<MRUKRoom> RoomCreatedEvent { get { return null; } }
        public UnityEvent<MRUKRoom> RoomUpdatedEvent { get { return null; } }
        public bool IsInitialized { get { return false; } }
        public List<MRUKRoom> Rooms { get { return null; } }
        public MRUKRoom GetCurrentRoom() { return null; }
        public Task<LoadDeviceResult> LoadSceneFromDevice(bool requestSceneCaptureIfNoDataFound) { return null; }
        public void LoadSceneFromJsonString(string json) { }
        public enum LoadDeviceResult { Success, NoScenePermission, NoRoomsFound, FailureSceneDataNotAvailable, FailureUnknown }
    }

    public class MRUKRoom : MonoBehaviour
    {
        public List<MRUKAnchor> Anchors { get { return null; } }
        public MRUKAnchor FloorAnchor { get { return null; } }
        public MRUKAnchor CeilingAnchor { get { return null; } }
        public MRUKAnchor GlobalMeshAnchor { get { return null; } }
        public List<MRUKAnchor> WallAnchors { get { return null; } }
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hit) { hit = default(RaycastHit); return false; }
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hit, out MRUKAnchor anchor) { hit = default(RaycastHit); anchor = null; return false; }
        public bool IsPositionInRoom(Vector3 position) { return false; }
        public Vector3 GenerateRandomPositionInRoom(float minDistanceToSurface, bool avoidVolumes) { return default(Vector3); }
        public bool TryGetClosestSurfacePosition(Vector3 testPosition, out Vector3 surfacePosition, out MRUKAnchor closestAnchor, out float distance)
        { surfacePosition = default(Vector3); closestAnchor = null; distance = 0f; return false; }
    }

    public class MRUKAnchor : MonoBehaviour
    {
        [Flags]
        public enum SceneLabels
        {
            FLOOR = 1, CEILING = 2, WALL_FACE = 4, TABLE = 8, COUCH = 16, DOOR_FRAME = 32,
            WINDOW_FRAME = 64, OTHER = 128, STORAGE = 256, BED = 512, SCREEN = 1024,
            LAMP = 2048, PLANT = 4096, WALL_ART = 8192, GLOBAL_MESH = 16384, INVISIBLE_WALL_FACE = 32768
        }

        public SceneLabels Label { get { return default(SceneLabels); } }
        public bool HasVolume { get { return false; } }
        public bool HasPlane { get { return false; } }
        public Bounds? VolumeBounds { get { return null; } }
        public Rect? PlaneRect { get { return null; } }
        public List<Vector2> PlaneBoundary2D { get { return null; } }
        public Mesh GlobalMesh { get { return null; } }
        public bool IsWall() { return false; }
        public bool IsFloor() { return false; }
    }
}
