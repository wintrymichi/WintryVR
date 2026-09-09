using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;
using WintryVR.Spatial;

namespace WintryVR.UI
{
    /// <summary>Shows translated text next to the original text in the world (live translation).</summary>
    public class TranslationOverlay : MonoBehaviour
    {
        public ISpatialService Spatial;
        public int MaxOverlays = 2;
        public float Lifetime = 20f;
        private readonly List<(GlassPanel panel, float expires)> _active = new List<(GlassPanel, float)>();
        private bool _subscribed;

        private void OnEnable()
        {
            if (_subscribed) return;
            _subscribed = true;
            WintryEvents.Subscribe<TranslationReadyEvent>(Show);
        }

        private void Show(TranslationReadyEvent e)
        {
            _active.RemoveAll(a => a.panel == null);
            while (_active.Count >= MaxOverlays) { _active[0].panel.Close(); _active.RemoveAt(0); }
            int lines = Mathf.Max(1, e.Translated.Split('\n').Length);
            var panel = GlassPanel.Create("Translation", 0.3f, 0.06f + Mathf.Min(8, lines) * 0.013f, Spatial?.Head, new Color(0.06f, 0.1f, 0.14f));
            panel.transform.SetParent(transform, false);
            panel.Resizable = false;
            panel.SetTitle(LanguageCodes.DisplayName(e.SourceLanguage) + " → " + LanguageCodes.DisplayName(e.TargetLanguage));
            panel.SetBody(e.Translated);
            Vector3 pos = e.WorldPosition != Vector3.zero ? e.WorldPosition + Vector3.up * 0.12f : WorldFirstLayout.SidePosition(Spatial, WintrySettings.Current.MR.UIDistance, true, 4f);
            panel.transform.position = WorldFirstLayout.ResolveOcclusion(Spatial, pos, 0.15f);
            _active.Add((panel, Time.time + Lifetime));
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                if (_active[i].panel == null) _active.RemoveAt(i);
                else if (Time.time > _active[i].expires) { _active[i].panel.Close(); _active.RemoveAt(i); }
        }

        public void ClearAll() { foreach (var a in _active) if (a.panel != null) a.panel.Close(); _active.Clear(); }
    }
}
