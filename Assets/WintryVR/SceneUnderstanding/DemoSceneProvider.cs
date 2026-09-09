using System;
using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.SceneUnderstanding
{
    /// <summary>
    /// A synthetic living room with invisible colliders (so gaze/vision raycasts hit real geometry). Used by
    /// Demo Mode and in the editor; on a Quest with Scene permission the MRUK provider takes over.
    /// </summary>
    public class DemoSceneProvider : ISceneProvider
    {
        private readonly List<SceneEntity> _entities = new List<SceneEntity>();
        private readonly GameObject _root;
        public event Action OnSceneChanged;

        public DemoSceneProvider(Transform parent)
        {
            _root = new GameObject("DemoRoomColliders");
            _root.transform.SetParent(parent, false);
            int layer = LayerMask.NameToLayer("Ignore Raycast"); // keep separate from UI hits; we raycast explicitly
            Add("Floor", SceneEntityKind.Floor, new Vector3(0, 0, 0), new Vector3(5f, 0.02f, 5f));
            Add("Ceiling", SceneEntityKind.Ceiling, new Vector3(0, 2.6f, 0), new Vector3(5f, 0.02f, 5f));
            Add("Wall front", SceneEntityKind.Wall, new Vector3(0, 1.3f, 2.5f), new Vector3(5f, 2.6f, 0.02f));
            Add("Wall back", SceneEntityKind.Wall, new Vector3(0, 1.3f, -2.5f), new Vector3(5f, 2.6f, 0.02f));
            Add("Wall left", SceneEntityKind.Wall, new Vector3(-2.5f, 1.3f, 0), new Vector3(0.02f, 2.6f, 5f));
            Add("Wall right", SceneEntityKind.Wall, new Vector3(2.5f, 1.3f, 0), new Vector3(0.02f, 2.6f, 5f));
            Add("Table", SceneEntityKind.Table, new Vector3(0, 0.72f, 1.2f), new Vector3(1.4f, 0.04f, 0.8f));
            Add("Chair", SceneEntityKind.Chair, new Vector3(-0.9f, 0.45f, 1.0f), new Vector3(0.5f, 0.9f, 0.5f));
            Add("Chair 2", SceneEntityKind.Chair, new Vector3(0.9f, 0.45f, 1.0f), new Vector3(0.5f, 0.9f, 0.5f));
            Add("Couch", SceneEntityKind.Couch, new Vector3(-1.6f, 0.4f, -1.4f), new Vector3(1.8f, 0.8f, 0.9f));
            Add("Door", SceneEntityKind.Door, new Vector3(1.8f, 1.05f, 2.49f), new Vector3(0.9f, 2.1f, 0.05f));
            Add("Window", SceneEntityKind.Window, new Vector3(-2.49f, 1.4f, 0.5f), new Vector3(0.05f, 1.2f, 1.4f));
            Add("TV", SceneEntityKind.Screen, new Vector3(0, 1.3f, 2.45f), new Vector3(1.4f, 0.8f, 0.06f));
            Add("Lamp", SceneEntityKind.Lamp, new Vector3(2.1f, 1.5f, -2.1f), new Vector3(0.3f, 1.6f, 0.3f));
            Add("Plant", SceneEntityKind.Plant, new Vector3(-2.1f, 0.5f, 2.1f), new Vector3(0.4f, 1.0f, 0.4f));
        }

        private void Add(string name, SceneEntityKind kind, Vector3 pos, Vector3 size)
        {
            var e = new SceneEntity { Id = "demo_" + name.Replace(" ", "_").ToLowerInvariant(), Name = name, Kind = kind, Position = pos, Size = size, Source = "demo" };
            _entities.Add(e);
            var go = new GameObject("DemoCollider_" + name);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = pos;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            go.AddComponent<DemoSceneCollider>().Entity = e;
        }

        public string Name => "demo-room";
        public bool IsAvailable => true;
        public bool HasRoom => true;
        public bool HasSceneMesh => false;
        public void Populate(List<SceneEntity> entities) { foreach (var e in _entities) entities.Add(e); }
        public void RequestSceneCapture() { OnSceneChanged?.Invoke(); }

        public bool Raycast(Ray ray, float maxDistance, out RaycastHit hit)
        {
            var hits = Physics.RaycastAll(ray, maxDistance);
            float best = float.MaxValue; hit = default; bool found = false;
            foreach (var h in hits)
            {
                if (h.collider.GetComponent<DemoSceneCollider>() == null) continue;
                if (h.distance < best) { best = h.distance; hit = h; found = true; }
            }
            return found;
        }
    }

    /// <summary>Tag component linking a demo collider to its scene entity.</summary>
    public class DemoSceneCollider : MonoBehaviour
    {
        public SceneEntity Entity;
    }
}
