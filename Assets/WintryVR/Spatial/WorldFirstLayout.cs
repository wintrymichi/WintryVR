using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Spatial
{
    /// <summary>
    /// World-first UX rules: virtual content avoids the centre of the view and does not cover what the user is
    /// looking at. Provides placement helpers and a "am I in the way" test used by the character and cards.
    /// </summary>
    public static class WorldFirstLayout
    {
        public const float CentreExclusionDegrees = 14f;
        public const float SideAngleDegrees = 24f;
        public const float DownAngleDegrees = 8f;

        public static bool IsCoveringCentre(ISpatialService spatial, Vector3 position, float radius)
        {
            Vector3 to = position - spatial.HeadPosition;
            float angle = Vector3.Angle(spatial.GazeDirection, to);
            float angularRadius = Mathf.Atan2(radius, Mathf.Max(0.1f, to.magnitude)) * Mathf.Rad2Deg;
            return angle - angularRadius < CentreExclusionDegrees;
        }

        /// <summary>A position to the side of the view that keeps the centre clear.</summary>
        public static Vector3 SidePosition(ISpatialService spatial, float distance, bool right, float verticalDegrees = -DownAngleDegrees)
        {
            return spatial.ComfortablePosition(distance, right ? SideAngleDegrees : -SideAngleDegrees, verticalDegrees);
        }

        /// <summary>Position for a card next to an object, pushed sideways away from the centre of view and slightly up.</summary>
        public static Vector3 BesideObject(ISpatialService spatial, Vector3 objectPosition, float objectRadius)
        {
            Vector3 toObj = objectPosition - spatial.HeadPosition;
            Vector3 right = Vector3.Cross(Vector3.up, toObj.normalized);
            float side = Vector3.Dot(toObj, spatial.Head != null ? spatial.Head.right : Vector3.right) >= 0 ? 1f : -1f;
            Vector3 pos = objectPosition + right * side * (objectRadius + 0.18f) + Vector3.up * (objectRadius * 0.5f + 0.06f);
            // never further than the object itself by more than a little, keep it near the user
            return pos;
        }

        /// <summary>Nudges a position sideways until it stops covering the centre of the view.</summary>
        public static Vector3 ResolveOcclusion(ISpatialService spatial, Vector3 position, float radius)
        {
            if (!IsCoveringCentre(spatial, position, radius)) return position;
            Vector3 to = position - spatial.HeadPosition;
            float dist = Mathf.Max(0.4f, to.magnitude);
            Vector3 headRight = spatial.Head != null ? spatial.Head.right : Vector3.right;
            float side = Vector3.Dot(to, headRight) >= 0 ? 1f : -1f;
            return spatial.ComfortablePosition(dist, side * SideAngleDegrees, -DownAngleDegrees);
        }
    }
}
