using System;
using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;
using WintryVR.Spatial;

namespace WintryVR.UI
{
    public struct ClearSessionEvent { }
    public struct ClearHistoryEvent { }
    public struct ClearAllDataEvent { }

    /// <summary>
    /// Spatial settings menu with sections AI · Voice · Vision · MR · Privacy · Accessibility (+ Privacy
    /// Dashboard actions). Every row is a button that cycles or toggles the value; values persist immediately.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        public ISpatialService Spatial;
        private GlassPanel _panel;
        private string _section = "AI";
        private readonly List<WintryButton> _rows = new List<WintryButton>();
        private static readonly string[] Sections = { "AI", "VOICE", "VISION", "MR", "PRIVACY", "ACCESS." };

        public bool IsOpen => _panel != null;

        public void Toggle()
        {
            if (_panel != null) { Close(); return; }
            _panel = GlassPanel.Create("Settings", 0.42f, 0.30f, Spatial?.Head);
            _panel.transform.SetParent(transform, false);
            _panel.Resizable = false;
            _panel.SetTitle("Settings");
            _panel.OnClosed = () => _panel = null;
            _panel.transform.position = WorldFirstLayout.SidePosition(Spatial, WintrySettings.Current.MR.UIDistance, false, -2f);
            for (int i = 0; i < Sections.Length; i++)
            {
                string s = Sections[i];
                WintryButton.Create(s, _panel.transform, new Vector3(-0.17f + i * 0.066f, 0.105f, -0.003f), 0.062f, 0.024f, () => { _section = s; Rebuild(); }, Spatial?.Head, new Color(0.25f, 0.3f, 0.42f));
            }
            Rebuild();
        }

        public void Close() { if (_panel != null) { _panel.Close(); _panel = null; } }

        private void Rebuild()
        {
            foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
            _rows.Clear();
            var s = WintrySettings.Current;
            var rows = new List<(string, Action)>();
            switch (_section)
            {
                case "AI":
                    rows.Add(("Provider: " + s.AI.Provider, () => { s.AI.Provider = Cycle(s.AI.Provider, "auto", "anthropic", "openai-compatible", "local", "mock"); s.Save("ai"); }));
                    rows.Add(("Model: " + (string.IsNullOrEmpty(s.AI.Model) ? "default" : s.AI.Model), () => { }));
                    rows.Add(("Response length: " + s.AI.ResponseLength, () => { s.AI.ResponseLength = (ResponseLength)(((int)s.AI.ResponseLength + 1) % 3); s.Save("ai"); }));
                    rows.Add(("Creativity: " + s.AI.Creativity.ToString("0.0"), () => { s.AI.Creativity = s.AI.Creativity >= 0.9f ? 0f : s.AI.Creativity + 0.3f; s.Save("ai"); }));
                    rows.Add(("Context memory: " + On(s.AI.ContextMemory), () => { s.AI.ContextMemory = !s.AI.ContextMemory; s.Save("ai"); }));
                    rows.Add(("Cloud processing: " + On(s.AI.CloudProcessing), () => { s.AI.CloudProcessing = !s.AI.CloudProcessing; s.Save("ai"); }));
                    break;
                case "VOICE":
                    rows.Add(("Voice: " + s.Voice.Voice, () => { s.Voice.Voice = Cycle(s.Voice.Voice, "default", "alloy", "nova", "echo", "low"); s.Save("voice"); }));
                    rows.Add(("Language: " + s.Voice.Language, () => { s.Voice.Language = (WintryLanguage)(((int)s.Voice.Language + 1) % 6); Localization.CurrentLanguage = s.EffectiveLanguage(); s.Save("voice"); }));
                    rows.Add(("Speed: " + s.Voice.Speed.ToString("0.0") + "x", () => { s.Voice.Speed = s.Voice.Speed >= 1.5f ? 0.8f : s.Voice.Speed + 0.2f; s.Save("voice"); }));
                    rows.Add(("Wake word: " + On(s.Voice.WakeWord), () => { s.Voice.WakeWord = !s.Voice.WakeWord; s.Save("voice"); }));
                    rows.Add(("Continuous conversation: " + On(s.Voice.ContinuousConversation), () => { s.Voice.ContinuousConversation = !s.Voice.ContinuousConversation; s.Save("voice"); }));
                    break;
                case "VISION":
                    rows.Add(("Object recognition: " + On(s.Vision.ObjectRecognition), () => { s.Vision.ObjectRecognition = !s.Vision.ObjectRecognition; s.Save("vision"); }));
                    rows.Add(("OCR: " + On(s.Vision.OCR), () => { s.Vision.OCR = !s.Vision.OCR; s.Save("vision"); }));
                    rows.Add(("Scene understanding: " + On(s.Vision.SceneUnderstanding), () => { s.Vision.SceneUnderstanding = !s.Vision.SceneUnderstanding; s.Save("vision"); }));
                    rows.Add(("Smart Highlight: " + On(s.Vision.SmartHighlight), () => { s.Vision.SmartHighlight = !s.Vision.SmartHighlight; s.Save("vision"); }));
                    break;
                case "MR":
                    rows.Add(("UI distance: " + s.MR.UIDistance.ToString("0.0") + " m", () => { s.MR.UIDistance = s.MR.UIDistance >= 2f ? 0.7f : s.MR.UIDistance + 0.3f; s.Save("mr"); }));
                    rows.Add(("UI size: " + s.MR.UISize.ToString("0.0"), () => { s.MR.UISize = s.MR.UISize >= 1.5f ? 0.7f : s.MR.UISize + 0.2f; s.Save("mr"); }));
                    rows.Add(("Anchor behaviour: " + s.MR.AnchorBehavior, () => { s.MR.AnchorBehavior = (AnchorBehavior)(((int)s.MR.AnchorBehavior + 1) % 3); s.Save("mr"); }));
                    rows.Add(("Passthrough brightness: " + s.MR.PassthroughBrightness.ToString("0.0"), () => { s.MR.PassthroughBrightness = s.MR.PassthroughBrightness >= 0.6f ? -0.6f : s.MR.PassthroughBrightness + 0.3f; s.Save("mr"); }));
                    rows.Add(("Character look: " + s.CharacterVariant, () => { s.CharacterVariant = (WintryVariant)(((int)s.CharacterVariant + 1) % 8); s.Save("character"); }));
                    break;
                case "PRIVACY":
                    rows.Add(("Camera: " + On(s.Privacy.CameraAllowed), () => { s.Privacy.CameraAllowed = !s.Privacy.CameraAllowed; s.Save("privacy"); }));
                    rows.Add(("Microphone: " + On(s.Privacy.MicrophoneAllowed), () => { s.Privacy.MicrophoneAllowed = !s.Privacy.MicrophoneAllowed; s.Save("privacy"); }));
                    rows.Add(("Cloud processing: " + On(s.Privacy.CloudProcessing), () => { s.Privacy.CloudProcessing = !s.Privacy.CloudProcessing; s.Save("privacy"); }));
                    rows.Add(("Location: " + On(s.Privacy.LocationAllowed), () => { s.Privacy.LocationAllowed = !s.Privacy.LocationAllowed; s.Save("privacy"); }));
                    rows.Add(("Keep history: " + On(s.Privacy.KeepHistory), () => { s.Privacy.KeepHistory = !s.Privacy.KeepHistory; s.Save("privacy"); }));
                    rows.Add(("Clear session", () => WintryEvents.Publish(new ClearSessionEvent())));
                    rows.Add(("Clear history", () => WintryEvents.Publish(new ClearHistoryEvent())));
                    rows.Add(("Clear all data", () => WintryEvents.Publish(new ClearAllDataEvent())));
                    break;
                default:
                    rows.Add(("Subtitles: " + On(s.Accessibility.Subtitles), () => { s.Accessibility.Subtitles = !s.Accessibility.Subtitles; s.Save("accessibility"); }));
                    rows.Add(("Text size: " + s.Accessibility.TextSize.ToString("0.0"), () => { s.Accessibility.TextSize = s.Accessibility.TextSize >= 1.7f ? 0.8f : s.Accessibility.TextSize + 0.3f; s.Save("accessibility"); }));
                    rows.Add(("High contrast: " + On(s.Accessibility.HighContrast), () => { s.Accessibility.HighContrast = !s.Accessibility.HighContrast; s.Save("accessibility"); }));
                    rows.Add(("Reduced motion: " + On(s.Accessibility.ReducedMotion), () => { s.Accessibility.ReducedMotion = !s.Accessibility.ReducedMotion; s.Save("accessibility"); }));
                    rows.Add(("Voice volume: " + Mathf.RoundToInt(s.Accessibility.VoiceVolume * 100) + "%", () => { s.Accessibility.VoiceVolume = s.Accessibility.VoiceVolume >= 0.95f ? 0.3f : s.Accessibility.VoiceVolume + 0.2f; s.Save("accessibility"); }));
                    break;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                var action = rows[i].Item2;
                var b = WintryButton.Create(rows[i].Item1, _panel.transform, new Vector3(0f, 0.07f - i * 0.026f, -0.003f), 0.36f, 0.022f, () => { action(); Rebuild(); }, Spatial?.Head, new Color(0.18f, 0.24f, 0.34f));
                _rows.Add(b);
            }
        }

        private static string On(bool b) => b ? "on" : "off";
        private static string Cycle(string current, params string[] values)
        {
            int idx = Array.IndexOf(values, current);
            return values[(idx + 1) % values.Length];
        }
    }
}
