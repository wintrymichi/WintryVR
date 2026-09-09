using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Spatial
{
    /// <summary>
    /// Spatial Intelligence: the user's pose and gaze, distances and relative directions, comfortable UI
    /// placement and gaze-vs-scene intersections. Everything spatial in the app asks this service.
    /// </summary>
    public class SpatialService : ISpatialService
    {
        private readonly Transform _head;
        public ISceneUnderstandingService Scene { get; set; }
        public float DefaultFloorHeight = 0f;

        public SpatialService(Transform head, ISceneUnderstandingService scene)
        {
            _head = head; Scene = scene;
        }

        public Transform Head => _head;
        public Vector3 HeadPosition => _head != null ? _head.position : Vector3.zero;
        public Vector3 GazeDirection => _head != null ? _head.forward : Vector3.forward;
        public Ray GazeRay => new Ray(HeadPosition, GazeDirection);
        public bool TrackingValid => _head != null && (UnityEngine.XR.XRSettings.isDeviceActive || Application.isEditor);

        public Vector3 FloorPosition
        {
            get
            {
                var p = HeadPosition;
                float floor = DefaultFloorHeight;
                if (Scene != null && Scene.HasRoom)
                    foreach (var e in Scene.FindByKind(SceneEntityKind.Floor)) { floor = e.Position.y; break; }
                p.y = floor;
                return p;
            }
        }

        public float DistanceTo(Vector3 worldPoint) => Vector3.Distance(HeadPosition, worldPoint);

        public RelativeDirection DirectionTo(Vector3 worldPoint)
        {
            Vector3 to = worldPoint - HeadPosition;
            Vector3 flatFwd = Vector3.ProjectOnPlane(GazeDirection, Vector3.up).normalized;
            Vector3 flatTo = Vector3.ProjectOnPlane(to, Vector3.up);
            float elev = Mathf.Atan2(to.y, flatTo.magnitude) * Mathf.Rad2Deg;
            if (elev > 40f) return RelativeDirection.Above;
            if (elev < -50f) return RelativeDirection.Below;
            float angle = Vector3.SignedAngle(flatFwd, flatTo.normalized, Vector3.up);
            float a = Mathf.Abs(angle);
            if (a < 25f) return RelativeDirection.Front;
            if (a > 155f) return RelativeDirection.Back;
            if (a < 70f) return angle < 0 ? RelativeDirection.FrontLeft : RelativeDirection.FrontRight;
            if (a > 110f) return angle < 0 ? RelativeDirection.BackLeft : RelativeDirection.BackRight;
            return angle < 0 ? RelativeDirection.Left : RelativeDirection.Right;
        }

        public string DescribeDirection(Vector3 worldPoint, string languageCode)
        {
            var dir = DirectionTo(worldPoint);
            float dist = DistanceTo(worldPoint);
            string distStr = dist < 1f ? DistWord(languageCode, "close") : (Mathf.RoundToInt(dist * 2f) / 2f).ToString("0.#") + " m";
            return DirWord(languageCode, dir) + ", " + distStr;
        }

        private static string DistWord(string lang, string key)
        {
            switch (Localization.Normalize(lang))
            {
                case "it": return "molto vicino";
                case "de": return "ganz nah";
                case "fr": return "tout près";
                case "es": return "muy cerca";
                default: return "very close";
            }
        }

        public static string DirWord(string lang, RelativeDirection d)
        {
            string l = Localization.Normalize(lang);
            switch (d)
            {
                case RelativeDirection.Front: return l == "it" ? "davanti a te" : l == "de" ? "vor dir" : l == "fr" ? "devant toi" : l == "es" ? "delante de ti" : "in front of you";
                case RelativeDirection.Back: return l == "it" ? "dietro di te" : l == "de" ? "hinter dir" : l == "fr" ? "derrière toi" : l == "es" ? "detrás de ti" : "behind you";
                case RelativeDirection.Left: return l == "it" ? "alla tua sinistra" : l == "de" ? "links von dir" : l == "fr" ? "à ta gauche" : l == "es" ? "a tu izquierda" : "on your left";
                case RelativeDirection.Right: return l == "it" ? "alla tua destra" : l == "de" ? "rechts von dir" : l == "fr" ? "à ta droite" : l == "es" ? "a tu derecha" : "on your right";
                case RelativeDirection.FrontLeft: return l == "it" ? "davanti a sinistra" : l == "de" ? "vorne links" : l == "fr" ? "devant à gauche" : l == "es" ? "delante a la izquierda" : "ahead on your left";
                case RelativeDirection.FrontRight: return l == "it" ? "davanti a destra" : l == "de" ? "vorne rechts" : l == "fr" ? "devant à droite" : l == "es" ? "delante a la derecha" : "ahead on your right";
                case RelativeDirection.BackLeft: return l == "it" ? "dietro a sinistra" : l == "de" ? "hinten links" : l == "fr" ? "derrière à gauche" : l == "es" ? "detrás a la izquierda" : "behind you on the left";
                case RelativeDirection.BackRight: return l == "it" ? "dietro a destra" : l == "de" ? "hinten rechts" : l == "fr" ? "derrière à droite" : l == "es" ? "detrás a la derecha" : "behind you on the right";
                case RelativeDirection.Above: return l == "it" ? "in alto" : l == "de" ? "oben" : l == "fr" ? "en haut" : l == "es" ? "arriba" : "above you";
                default: return l == "it" ? "in basso" : l == "de" ? "unten" : l == "fr" ? "en bas" : l == "es" ? "abajo" : "below you";
            }
        }

        public Vector3 ComfortablePosition(float distance, float lateralOffsetDegrees, float verticalOffsetDegrees)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(GazeDirection, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            Quaternion rot = Quaternion.AngleAxis(lateralOffsetDegrees, Vector3.up) * Quaternion.AngleAxis(-verticalOffsetDegrees, Vector3.Cross(Vector3.up, fwd));
            Vector3 dir = rot * fwd;
            return HeadPosition + dir * distance;
        }

        public Vector3 GazeHitPoint(float defaultDistance)
        {
            var ray = GazeRay;
            if (Scene != null && Scene.Raycast(ray, 8f, out RaycastHit hit)) return hit.point;
            if (Physics.Raycast(ray, out RaycastHit ph, 8f)) return ph.point;
            return ray.GetPoint(defaultDistance);
        }

        public bool IsInFieldOfView(Vector3 worldPoint, float halfAngleDegrees)
        {
            return Vector3.Angle(GazeDirection, worldPoint - HeadPosition) <= halfAngleDegrees;
        }
    }
}
