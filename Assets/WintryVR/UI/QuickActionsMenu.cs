using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;
using WintryVR.Spatial;

namespace WintryVR.UI
{
    public struct QuickActionEvent { public Intent Intent; }
    public struct OpenSettingsEvent { }
    public struct ToggleHistoryEvent { }

    /// <summary>
    /// Palm / menu-button quick actions: Identify · Read · Translate · Search · Explain · Locate, plus
    /// History and Settings. Appears near the hand (or in front, off-centre) and auto-hides.
    /// </summary>
    public class QuickActionsMenu : MonoBehaviour
    {
        public ISpatialService Spatial;
        private GlassPanel _panel;
        private float _autoHideAt;

        public bool IsOpen => _panel != null;

        public void Toggle(Vector3? anchor = null)
        {
            if (_panel != null) { Close(); return; }
            string lang = WintrySettings.Current.EffectiveLanguage();
            _panel = GlassPanel.Create("QuickActions", 0.28f, 0.16f, Spatial != null ? Spatial.Head : null);
            _panel.transform.SetParent(transform, false);
            _panel.Resizable = false;
            _panel.SetTitle("Wintry");
            _panel.SetBody("");
            Row(0, new[] { ("Identify", Intent.IDENTIFY), ("Read", Intent.OCR), ("Translate", Intent.TRANSLATE) });
            Row(1, new[] { ("Search", Intent.SEARCH), ("Explain", Intent.EXPLAIN), ("Locate", Intent.LOCATE) });
            WintryButton.Create("History", _panel.transform, new Vector3(-0.07f, -0.06f, -0.003f), 0.1f, 0.026f, () => { WintryEvents.Publish(new ToggleHistoryEvent()); Close(); }, Spatial?.Head, new Color(0.3f, 0.35f, 0.45f));
            WintryButton.Create("Settings", _panel.transform, new Vector3(0.05f, -0.06f, -0.003f), 0.1f, 0.026f, () => { WintryEvents.Publish(new OpenSettingsEvent()); Close(); }, Spatial?.Head, new Color(0.3f, 0.35f, 0.45f));
            _panel.OnClosed = () => _panel = null;
            Vector3 pos = anchor ?? WorldFirstLayout.SidePosition(Spatial, 0.55f, false, -12f);
            if (anchor.HasValue) pos += Vector3.up * 0.12f;
            _panel.transform.position = pos;
            _autoHideAt = Time.time + 12f;
        }

        private void Row(int row, (string, Intent)[] items)
        {
            float y = 0.03f - row * 0.036f;
            for (int i = 0; i < items.Length; i++)
            {
                var intent = items[i].Item2;
                WintryButton.Create(items[i].Item1, _panel.transform, new Vector3(-0.085f + i * 0.085f, y, -0.003f), 0.078f, 0.028f, () => { WintryEvents.Publish(new QuickActionEvent { Intent = intent }); Close(); }, Spatial?.Head);
            }
        }

        public void Close() { if (_panel != null) { _panel.Close(); _panel = null; } }

        private void Update() { if (_panel != null && Time.time > _autoHideAt) Close(); }
    }
}
