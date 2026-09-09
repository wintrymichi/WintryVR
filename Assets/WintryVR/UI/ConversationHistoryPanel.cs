using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;
using WintryVR.Spatial;

namespace WintryVR.UI
{
    /// <summary>Optional, minimal, closable floating transcript: YOU / WINTRY lines.</summary>
    public class ConversationHistoryPanel : MonoBehaviour
    {
        public int MaxLines = 8;
        public ISpatialService Spatial;
        private GlassPanel _panel;
        private readonly List<string> _lines = new List<string>();
        private bool _subscribed;

        public bool IsOpen => _panel != null;

        private void OnEnable()
        {
            if (_subscribed) return;
            _subscribed = true;
            WintryEvents.Subscribe<UserTurnEvent>(e => Add(Localization.Get("you") + "  " + e.Text));
            WintryEvents.Subscribe<AssistantReplyEvent>(e => Add(Localization.Get("wintry") + "  " + e.Turn.AssistantText));
        }

        private void Add(string line)
        {
            if (!WintrySettings.Current.Privacy.KeepHistory) return;
            _lines.Add(line);
            while (_lines.Count > MaxLines) _lines.RemoveAt(0);
            if (_panel != null) _panel.SetBody(string.Join("\n", _lines));
        }

        public void Toggle()
        {
            if (_panel != null) { _panel.Close(); _panel = null; return; }
            _panel = GlassPanel.Create("HistoryPanel", 0.3f, 0.2f, Spatial != null ? Spatial.Head : null);
            _panel.transform.SetParent(transform, false);
            _panel.SetTitle("Conversation");
            _panel.SetBody(_lines.Count > 0 ? string.Join("\n", _lines) : "…");
            _panel.OnClosed = () => _panel = null;
            _panel.transform.position = WorldFirstLayout.SidePosition(Spatial, WintrySettings.Current.MR.UIDistance, false, -4f);
        }

        public void Clear() { _lines.Clear(); if (_panel != null) _panel.SetBody("…"); }
    }
}
