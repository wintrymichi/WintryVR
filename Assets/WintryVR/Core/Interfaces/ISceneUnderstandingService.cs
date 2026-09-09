using System.Collections.Generic;
using UnityEngine;

namespace WintryVR.Core
{
    public enum SceneEntityKind { Room, Floor, Ceiling, Wall, Door, Window, Table, Couch, Chair, Bed, Storage, Screen, Lamp, Plant, Surface, Object, Other }

    /// <summary>A node in the scene graph (room, surface or object).</summary>
    public class SceneEntity
    {
        public string Id;
        public string Name;
        public SceneEntityKind Kind;
        public Vector3 Position;
        public Vector3 Size;
        public Quaternion Rotation = Quaternion.identity;
        public string Source;                   // "mruk", "vision", "demo"
        public float Confidence = 1f;
        public SceneEntity Parent;
        public List<SceneEntity> Children = new List<SceneEntity>();
        public string ObservedObjectId;         // link to context memory when created by vision

        public Bounds WorldBounds => new Bounds(Position, Size);
        public bool IsSurface => Kind == SceneEntityKind.Table || Kind == SceneEntityKind.Floor || Kind == SceneEntityKind.Couch || Kind == SceneEntityKind.Bed || Kind == SceneEntityKind.Storage || Kind == SceneEntityKind.Surface;
    }

    public interface ISceneUnderstandingService
    {
        bool IsAvailable { get; }
        bool HasRoom { get; }
        SceneEntity Room { get; }
        IReadOnlyList<SceneEntity> Entities { get; }
        void Refresh();
        /// <summary>Attach a vision-detected object to the closest supporting surface.</summary>
        SceneEntity RegisterObject(ObservedObject obj);
        void RemoveObject(string observedObjectId);
        IEnumerable<SceneEntity> FindByKind(SceneEntityKind kind);
        IEnumerable<SceneEntity> FindByName(string nameFragment);
        /// <summary>Raycast against the room geometry (walls, floor, furniture, scene mesh).</summary>
        bool Raycast(Ray ray, float maxDistance, out RaycastHit hit);
        /// <summary>Text description used as AI context.</summary>
        string Describe(Vector3 userPosition, Vector3 userForward, int maxEntities);
    }
}
