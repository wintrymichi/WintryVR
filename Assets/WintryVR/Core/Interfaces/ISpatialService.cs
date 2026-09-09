using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WintryVR.Core
{
    public enum RelativeDirection { Front, Back, Left, Right, Above, Below, FrontLeft, FrontRight, BackLeft, BackRight }

    public interface ISpatialService
    {
        Transform Head { get; }
        Vector3 HeadPosition { get; }
        Vector3 GazeDirection { get; }
        Ray GazeRay { get; }
        Vector3 FloorPosition { get; }
        bool TrackingValid { get; }

        float DistanceTo(Vector3 worldPoint);
        RelativeDirection DirectionTo(Vector3 worldPoint);
        string DescribeDirection(Vector3 worldPoint, string languageCode);
        /// <summary>Position for UI at a given distance in a comfortable spot (off centre unless requested).</summary>
        Vector3 ComfortablePosition(float distance, float lateralOffsetDegrees, float verticalOffsetDegrees);
        /// <summary>Ray from the current gaze intersected with the scene (or a default distance when nothing is hit).</summary>
        Vector3 GazeHitPoint(float defaultDistance);
        bool IsInFieldOfView(Vector3 worldPoint, float halfAngleDegrees);
    }

    public class SpatialAnchorHandle
    {
        public string Id;
        public Transform Transform;
        public bool Persisted;
        public bool Localized;
    }

    public interface IAnchorService
    {
        bool IsAvailable { get; }
        bool SupportsPersistence { get; }
        Task<SpatialAnchorHandle> CreateAnchorAsync(Pose pose, string label);
        Task<bool> PersistAsync(SpatialAnchorHandle handle);
        Task<bool> EraseAsync(SpatialAnchorHandle handle);
        Task<int> RestoreAsync();
        IReadOnlyList<SpatialAnchorHandle> Anchors { get; }
        void Destroy(SpatialAnchorHandle handle);
    }
}
