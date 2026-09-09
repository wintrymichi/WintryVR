using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Spatial
{
    /// <summary>
    /// Fallback anchor service: anchors are plain transforms in tracking space (stable for the session, not
    /// persisted across sessions). The Meta implementation replaces it when spatial anchors are supported.
    /// </summary>
    public class LocalAnchorService : IAnchorService
    {
        private readonly List<SpatialAnchorHandle> _anchors = new List<SpatialAnchorHandle>();
        private readonly Transform _root;
        private int _counter;

        public LocalAnchorService(Transform root) { _root = root; }
        public bool IsAvailable => true;
        public bool SupportsPersistence => false;
        public IReadOnlyList<SpatialAnchorHandle> Anchors => _anchors;

        public Task<SpatialAnchorHandle> CreateAnchorAsync(Pose pose, string label)
        {
            var go = new GameObject("LocalAnchor_" + label);
            go.transform.SetParent(_root, true);
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            var h = new SpatialAnchorHandle { Id = "local_" + (++_counter), Transform = go.transform, Localized = true, Persisted = false };
            _anchors.Add(h);
            return Task.FromResult(h);
        }

        public Task<bool> PersistAsync(SpatialAnchorHandle handle) => Task.FromResult(false);
        public Task<bool> EraseAsync(SpatialAnchorHandle handle) { Destroy(handle); return Task.FromResult(true); }
        public Task<int> RestoreAsync() => Task.FromResult(0);

        public void Destroy(SpatialAnchorHandle handle)
        {
            if (handle == null) return;
            _anchors.Remove(handle);
            if (handle.Transform != null) Object.Destroy(handle.Transform.gameObject);
        }
    }
}
