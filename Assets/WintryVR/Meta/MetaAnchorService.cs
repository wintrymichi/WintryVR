#if WINTRY_META_XR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.MetaPlatform
{
    /// <summary>
    /// Spatial anchors through OVRSpatialAnchor with on-device persistence (UUIDs kept in PlayerPrefs so the
    /// anchors can be loaded and re-localised in later sessions). Used for Wintry's home position and for
    /// pinned information cards.
    /// </summary>
    public class MetaAnchorService : IAnchorService
    {
        private readonly List<SpatialAnchorHandle> _anchors = new List<SpatialAnchorHandle>();
        private readonly Dictionary<string, OVRSpatialAnchor> _components = new Dictionary<string, OVRSpatialAnchor>();
        private const string PrefKey = "wintry.anchors.uuids";

        public bool IsAvailable => !Application.isEditor;
        public bool SupportsPersistence => true;
        public IReadOnlyList<SpatialAnchorHandle> Anchors => _anchors;

        public async Task<SpatialAnchorHandle> CreateAnchorAsync(Pose pose, string label)
        {
            var go = new GameObject("SpatialAnchor_" + label);
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            var anchor = go.AddComponent<OVRSpatialAnchor>();
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!anchor.Created && Time.realtimeSinceStartup < deadline) await Task.Yield();
            if (!anchor.Created) { WintryLog.W("Meta", "Anchor creation timed out"); UnityEngine.Object.Destroy(go); return null; }
            var handle = new SpatialAnchorHandle { Id = anchor.Uuid.ToString(), Transform = go.transform, Localized = anchor.Localized, Persisted = false };
            _anchors.Add(handle); _components[handle.Id] = anchor;
            return handle;
        }

        public async Task<bool> PersistAsync(SpatialAnchorHandle handle)
        {
            if (handle == null || !_components.TryGetValue(handle.Id, out var anchor)) return false;
            try
            {
                var result = await anchor.SaveAnchorAsync();
                handle.Persisted = result.Success;
                if (result.Success) RememberUuid(anchor.Uuid);
                return result.Success;
            }
            catch (Exception ex) { WintryLog.W("Meta", "Anchor save failed: " + ex.Message); return false; }
        }

        public async Task<bool> EraseAsync(SpatialAnchorHandle handle)
        {
            if (handle == null || !_components.TryGetValue(handle.Id, out var anchor)) return false;
            try
            {
                var result = await anchor.EraseAnchorAsync();
                if (result.Success) ForgetUuid(anchor.Uuid);
                Destroy(handle);
                return result.Success;
            }
            catch (Exception ex) { WintryLog.W("Meta", "Anchor erase failed: " + ex.Message); return false; }
        }

        public async Task<int> RestoreAsync()
        {
            var uuids = LoadUuids();
            if (uuids.Count == 0) return 0;
            int restored = 0;
            try
            {
                var unbound = new List<OVRSpatialAnchor.UnboundAnchor>();
                var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, unbound);
                if (!result.Success) { WintryLog.W("Meta", "LoadUnboundAnchors failed"); return 0; }
                foreach (var u in unbound)
                {
                    var go = new GameObject("SpatialAnchor_restored");
                    var anchor = go.AddComponent<OVRSpatialAnchor>();
                    bool localized = await u.LocalizeAsync();
                    if (!localized) { UnityEngine.Object.Destroy(go); continue; }
                    u.BindTo(anchor);
                    var handle = new SpatialAnchorHandle { Id = u.Uuid.ToString(), Transform = go.transform, Localized = true, Persisted = true };
                    _anchors.Add(handle); _components[handle.Id] = anchor; restored++;
                }
            }
            catch (Exception ex) { WintryLog.W("Meta", "Anchor restore failed: " + ex.Message); }
            WintryLog.I("Meta", "Restored " + restored + " anchor(s)");
            return restored;
        }

        public void Destroy(SpatialAnchorHandle handle)
        {
            if (handle == null) return;
            _anchors.Remove(handle); _components.Remove(handle.Id);
            if (handle.Transform != null) UnityEngine.Object.Destroy(handle.Transform.gameObject);
        }

        private static List<Guid> LoadUuids()
        {
            var list = new List<Guid>();
            foreach (var s in PlayerPrefs.GetString(PrefKey, "").Split(';')) if (Guid.TryParse(s, out var g)) list.Add(g);
            return list;
        }
        private static void RememberUuid(Guid g) { var l = LoadUuids(); if (!l.Contains(g)) l.Add(g); PlayerPrefs.SetString(PrefKey, string.Join(";", l)); PlayerPrefs.Save(); }
        private static void ForgetUuid(Guid g) { var l = LoadUuids(); l.Remove(g); PlayerPrefs.SetString(PrefKey, string.Join(";", l)); PlayerPrefs.Save(); }
    }
}
#endif
