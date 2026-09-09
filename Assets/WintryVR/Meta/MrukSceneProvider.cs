#if WINTRY_META_XR && WINTRY_MRUK
using System;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using WintryVR.Core;
using WintryVR.SceneUnderstanding;

namespace WintryVR.MetaPlatform
{
    /// <summary>
    /// Room understanding through the Mixed Reality Utility Kit: walls, floor, ceiling, tables, couches, doors,
    /// windows, storage, screens, lamps, plants (+ the global scene mesh when present). Raycasts hit the room
    /// model so vision detections and gaze can be placed on real surfaces.
    /// </summary>
    public class MrukSceneProvider : ISceneProvider
    {
        private MRUK _mruk;
        public event Action OnSceneChanged;

        public MrukSceneProvider()
        {
            _mruk = MRUK.Instance;
            if (_mruk == null)
            {
                var go = new GameObject("MRUK");
                _mruk = go.AddComponent<MRUK>();
            }
            _mruk.SceneLoadedEvent.AddListener(() => { WintryLog.I("Meta", "MRUK scene loaded"); OnSceneChanged?.Invoke(); });
        }

        public string Name => "mruk";
        public bool IsAvailable => _mruk != null && !Application.isEditor;
        public bool HasRoom => _mruk != null && _mruk.GetCurrentRoom() != null;
        public bool HasSceneMesh { get { var r = _mruk != null ? _mruk.GetCurrentRoom() : null; return r != null && r.GlobalMeshAnchor != null; } }

        public async void RequestSceneCapture()
        {
            try
            {
                if (!await WintryVR.Settings.PermissionManager.EnsureSceneAsync()) { WintryLog.W("Meta", "Scene permission denied"); return; }
                var result = await _mruk.LoadSceneFromDevice(true);
                WintryLog.I("Meta", "LoadSceneFromDevice: " + result);
            }
            catch (Exception ex) { WintryLog.W("Meta", "Scene load failed: " + ex.Message); }
        }

        public void Populate(List<SceneEntity> entities)
        {
            var room = _mruk != null ? _mruk.GetCurrentRoom() : null;
            if (room == null) return;
            int i = 0;
            foreach (var anchor in room.Anchors)
            {
                if (anchor == null) continue;
                string label = anchor.Label.ToString();
                var e = new SceneEntity
                {
                    Id = "mruk_" + (i++), Name = Pretty(label), Kind = KindFromLabel(label), Source = "mruk",
                    Position = anchor.transform.position, Rotation = anchor.transform.rotation, Confidence = 1f
                };
                if (anchor.HasVolume && anchor.VolumeBounds.HasValue)
                {
                    var b = anchor.VolumeBounds.Value;
                    e.Size = anchor.transform.TransformVector(b.size); e.Size = new Vector3(Mathf.Abs(e.Size.x), Mathf.Abs(e.Size.y), Mathf.Abs(e.Size.z));
                    e.Position = anchor.transform.TransformPoint(b.center);
                }
                else if (anchor.HasPlane && anchor.PlaneRect.HasValue)
                {
                    var r = anchor.PlaneRect.Value;
                    Vector3 s = anchor.transform.TransformVector(new Vector3(r.width, r.height, 0.02f));
                    e.Size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                }
                else e.Size = new Vector3(0.3f, 0.3f, 0.3f);
                entities.Add(e);
            }
        }

        public bool Raycast(Ray ray, float maxDistance, out RaycastHit hit)
        {
            hit = default;
            var room = _mruk != null ? _mruk.GetCurrentRoom() : null;
            if (room == null) return false;
            return room.Raycast(ray, maxDistance, out hit, out MRUKAnchor _);
        }

        private static string Pretty(string label)
        {
            string l = label.Replace("_", " ").ToLowerInvariant();
            return char.ToUpperInvariant(l[0]) + l.Substring(1);
        }

        private static SceneEntityKind KindFromLabel(string label)
        {
            string l = label.ToUpperInvariant();
            if (l.Contains("FLOOR")) return SceneEntityKind.Floor;
            if (l.Contains("CEILING")) return SceneEntityKind.Ceiling;
            if (l.Contains("WALL")) return SceneEntityKind.Wall;
            if (l.Contains("DOOR")) return SceneEntityKind.Door;
            if (l.Contains("WINDOW")) return SceneEntityKind.Window;
            if (l.Contains("TABLE")) return SceneEntityKind.Table;
            if (l.Contains("COUCH")) return SceneEntityKind.Couch;
            if (l.Contains("BED")) return SceneEntityKind.Bed;
            if (l.Contains("STORAGE")) return SceneEntityKind.Storage;
            if (l.Contains("SCREEN")) return SceneEntityKind.Screen;
            if (l.Contains("LAMP")) return SceneEntityKind.Lamp;
            if (l.Contains("PLANT")) return SceneEntityKind.Plant;
            if (l.Contains("GLOBAL_MESH")) return SceneEntityKind.Surface;
            return SceneEntityKind.Other;
        }
    }
}
#endif
