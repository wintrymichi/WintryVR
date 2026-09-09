using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.SceneUnderstanding
{
    /// <summary>Source of room geometry: MRUK on device, a synthetic room in demo/editor.</summary>
    public interface ISceneProvider
    {
        string Name { get; }
        bool IsAvailable { get; }
        bool HasRoom { get; }
        bool HasSceneMesh { get; }
        /// <summary>Fills the list with room/surface entities (no vision objects).</summary>
        void Populate(List<SceneEntity> entities);
        bool Raycast(Ray ray, float maxDistance, out RaycastHit hit);
        event Action OnSceneChanged;
        void RequestSceneCapture();
    }

    /// <summary>
    /// Scene graph: Room → surfaces (tables, walls, floor, ...) → objects (from vision). Merges the platform
    /// room model with what Wintry has observed, and describes it as text for the AI.
    /// </summary>
    public class SceneUnderstandingService : ISceneUnderstandingService
    {
        private readonly ISceneProvider _provider;
        private readonly List<SceneEntity> _entities = new List<SceneEntity>();
        private readonly Dictionary<string, SceneEntity> _objectsById = new Dictionary<string, SceneEntity>();
        private SceneEntity _room;
        private int _counter;

        public SceneUnderstandingService(ISceneProvider provider)
        {
            _provider = provider;
            if (_provider != null) _provider.OnSceneChanged += Refresh;
            Refresh();
        }

        public bool IsAvailable => _provider != null && _provider.IsAvailable;
        public bool HasRoom => _room != null && _entities.Count > 1;
        public SceneEntity Room => _room;
        public IReadOnlyList<SceneEntity> Entities => _entities;
        public string ProviderName => _provider != null ? _provider.Name : "none";

        public void Refresh()
        {
            var vision = new List<SceneEntity>();
            foreach (var e in _entities) if (e.Source == "vision") vision.Add(e);
            _entities.Clear();
            _room = new SceneEntity { Id = "room", Name = "Room", Kind = SceneEntityKind.Room, Source = ProviderName };
            _entities.Add(_room);
            if (_provider != null && _provider.HasRoom)
            {
                var list = new List<SceneEntity>();
                _provider.Populate(list);
                foreach (var e in list)
                {
                    if (string.IsNullOrEmpty(e.Id)) e.Id = "ent_" + (++_counter);
                    e.Parent = _room; _room.Children.Add(e); _entities.Add(e);
                }
                // room bounds
                if (list.Count > 0)
                {
                    var b = new Bounds(list[0].Position, Vector3.zero);
                    foreach (var e in list) b.Encapsulate(e.WorldBounds);
                    _room.Position = b.center; _room.Size = b.size;
                }
            }
            foreach (var v in vision) Attach(v);
            WintryLog.V("Scene", "Scene refreshed: " + _entities.Count + " entities via " + ProviderName);
        }

        public SceneEntity RegisterObject(ObservedObject obj)
        {
            if (obj == null) return null;
            if (_objectsById.TryGetValue(obj.Id, out var existing))
            {
                existing.Position = obj.WorldPosition; existing.Name = obj.DisplayName; existing.Confidence = obj.Confidence;
                return existing;
            }
            var e = new SceneEntity
            {
                Id = "obj_" + obj.Id, Name = obj.DisplayName, Kind = KindFromLabel(obj.Label), Position = obj.WorldPosition,
                Size = obj.Extents * 2f, Source = "vision", Confidence = obj.Confidence, ObservedObjectId = obj.Id
            };
            Attach(e);
            _objectsById[obj.Id] = e;
            return e;
        }

        private void Attach(SceneEntity e)
        {
            // find the supporting surface: nearest surface whose horizontal footprint contains the object and whose top is just below it
            SceneEntity best = _room; float bestScore = float.MaxValue;
            foreach (var s in _entities)
            {
                if (!s.IsSurface) continue;
                var b = s.WorldBounds;
                float top = b.max.y;
                bool inside = e.Position.x >= b.min.x - 0.1f && e.Position.x <= b.max.x + 0.1f && e.Position.z >= b.min.z - 0.1f && e.Position.z <= b.max.z + 0.1f;
                float dy = e.Position.y - top;
                if (inside && dy > -0.15f && dy < 0.5f && Mathf.Abs(dy) < bestScore) { bestScore = Mathf.Abs(dy); best = s; }
            }
            e.Parent = best;
            if (!best.Children.Contains(e)) best.Children.Add(e);
            if (!_entities.Contains(e)) _entities.Add(e);
        }

        public void RemoveObject(string observedObjectId)
        {
            if (_objectsById.TryGetValue(observedObjectId, out var e))
            {
                e.Parent?.Children.Remove(e); _entities.Remove(e); _objectsById.Remove(observedObjectId);
            }
        }

        public IEnumerable<SceneEntity> FindByKind(SceneEntityKind kind)
        {
            foreach (var e in _entities) if (e.Kind == kind) yield return e;
        }

        public IEnumerable<SceneEntity> FindByName(string nameFragment)
        {
            string f = (nameFragment ?? "").ToLowerInvariant();
            foreach (var e in _entities) if (e.Name != null && e.Name.ToLowerInvariant().Contains(f)) yield return e;
        }

        public bool Raycast(Ray ray, float maxDistance, out RaycastHit hit)
        {
            if (_provider != null && _provider.Raycast(ray, maxDistance, out hit)) return true;
            hit = default;
            return false;
        }

        public string Describe(Vector3 userPosition, Vector3 userForward, int maxEntities)
        {
            var sb = new StringBuilder();
            sb.Append("Room model: ").Append(HasRoom ? ProviderName : "none").AppendLine();
            int n = 0;
            foreach (var e in _entities)
            {
                if (e.Kind == SceneEntityKind.Room) continue;
                if (n++ >= maxEntities) break;
                Vector3 to = e.Position - userPosition;
                float dist = to.magnitude;
                float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(userForward, Vector3.up), Vector3.ProjectOnPlane(to, Vector3.up), Vector3.up);
                string side = Mathf.Abs(angle) < 25 ? "ahead" : angle < 0 ? (Mathf.Abs(angle) > 120 ? "behind-left" : "left") : (angle > 120 ? "behind-right" : "right");
                sb.Append("- ").Append(e.Name).Append(" [").Append(e.Kind).Append("]");
                if (e.Parent != null && e.Parent.Kind != SceneEntityKind.Room) sb.Append(" on ").Append(e.Parent.Name);
                sb.Append(", ").Append(dist.ToString("0.0")).Append(" m ").Append(side);
                if (e.Source == "vision") sb.Append(", seen ").Append(Mathf.RoundToInt(e.Confidence * 100)).Append('%');
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static SceneEntityKind KindFromLabel(string label)
        {
            string l = (label ?? "").ToLowerInvariant();
            if (l.Contains("table") || l.Contains("desk")) return SceneEntityKind.Table;
            if (l.Contains("chair") || l.Contains("stool")) return SceneEntityKind.Chair;
            if (l.Contains("couch") || l.Contains("sofa")) return SceneEntityKind.Couch;
            if (l.Contains("door")) return SceneEntityKind.Door;
            if (l.Contains("window")) return SceneEntityKind.Window;
            if (l.Contains("bed")) return SceneEntityKind.Bed;
            if (l.Contains("lamp")) return SceneEntityKind.Lamp;
            if (l.Contains("plant")) return SceneEntityKind.Plant;
            if (l.Contains("tv") || l.Contains("screen") || l.Contains("monitor")) return SceneEntityKind.Screen;
            if (l.Contains("shelf") || l.Contains("cabinet") || l.Contains("wardrobe") || l.Contains("storage")) return SceneEntityKind.Storage;
            return SceneEntityKind.Object;
        }
    }
}
