using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;

namespace WintryVR.UI
{
    /// <summary>
    /// Head-referenced (lazily following) HUD: privacy indicators (camera · microphone · cloud), the status
    /// chip ("Listening…", "Offline Mode") and subtitles. Small, at the edge of the view, never in the centre.
    /// </summary>
    public class HudOverlay : MonoBehaviour
    {
        public Transform Head;
        public float Distance = 0.9f;
        private Transform _root;
        private Material _camMat, _micMat, _cloudMat;
        private WintryText _status, _subtitle;
        private string _statusText = "";
        private float _subtitleUntil;
        private bool _online = true;
        private AssistantState _state = AssistantState.Idle;
        private readonly Color _off = new Color(0.4f, 0.45f, 0.5f, 0.35f);

        public void Initialize(Transform head)
        {
            Head = head;
            _root = new GameObject("HudRoot").transform;
            _root.SetParent(transform, false);

            _camMat = Dot(new Vector3(-0.03f, 0.13f, 0f), "Cam");
            _micMat = Dot(new Vector3(0f, 0.13f, 0f), "Mic");
            _cloudMat = Dot(new Vector3(0.03f, 0.13f, 0f), "Cloud");
            SetIndicators(false, false, false);

            _status = WintryText.Create("Status", _root, new Vector3(0f, 0.105f, 0f), 0.0068f,
                                        TextRole.Ui, TextAnchor.MiddleCenter, new Color(0.85f, 0.95f, 1f, 0.9f));
            _subtitle = WintryText.Create("Subtitle", _root, new Vector3(0f, -0.12f, 0f), 0.0104f,
                                          TextRole.Body, TextAnchor.MiddleCenter, Color.white);
            _subtitle.SetArea(new Vector2(0.46f, 0.09f));

            WintryEvents.Subscribe<PrivacyIndicatorEvent>(e => SetIndicators(e.CameraActive, e.MicrophoneActive, e.CloudActive));
            WintryEvents.Subscribe<StateChangedEvent>(e => { _state = e.Current; RefreshStatus(); });
            WintryEvents.Subscribe<ConnectivityChangedEvent>(e => { _online = e.Online; RefreshStatus(); });
            WintryEvents.Subscribe<AssistantReplyEvent>(e => ShowSubtitle(e.Turn.AssistantText, Mathf.Clamp(e.Turn.AssistantText.Length * 0.06f, 2.5f, 12f)));
            WintryEvents.Subscribe<TranscriptEvent>(e => { if (e.IsFinal) ShowSubtitle("“" + e.Text + "”", 2.5f); });
            WintryEvents.Subscribe<SettingsChangedEvent>(_ => RefreshStatus());
        }

        private Material Dot(Vector3 pos, string name)
        {
            var go = new GameObject("Indicator_" + name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Icosphere(0.004f, 1);
            var mr = go.AddComponent<MeshRenderer>();
            var m = WintryMaterials.Glow(_off, _off, 1f, 0.35f);
            mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return m;
        }

        private void SetIndicators(bool cam, bool mic, bool cloud)
        {
            bool show = WintrySettings.Current.Privacy.ShowIndicators;
            Paint(_camMat, cam && show, new Color(1f, 0.7f, 0.3f));
            Paint(_micMat, mic && show, new Color(0.4f, 1f, 0.6f));
            Paint(_cloudMat, cloud && show, new Color(0.5f, 0.8f, 1f));
        }

        private void Paint(Material m, bool on, Color c)
        {
            WintryMaterials.SetColor(m, on ? c : _off);
            WintryMaterials.SetEmission(m, on ? c * 2f : Color.black);
            WintryMaterials.SetAlpha(m, on ? 1f : 0.3f);
        }

        private void RefreshStatus()
        {
            string lang = WintrySettings.Current.EffectiveLanguage();
            if (!_online) _statusText = Localization.Get("status.offline", lang);
            else switch (_state)
                {
                    case AssistantState.Listening: _statusText = Localization.Get("status.listening", lang); break;
                    case AssistantState.Thinking: _statusText = Localization.Get("status.thinking", lang); break;
                    case AssistantState.Vision: _statusText = Localization.Get("status.looking", lang); break;
                    case AssistantState.Searching: _statusText = Localization.Get("status.searching", lang); break;
                    default: _statusText = ""; break;
                }
            if (_status != null) _status.SetText(_statusText);
        }

        public void ShowSubtitle(string text, float seconds)
        {
            if (!WintrySettings.Current.Accessibility.Subtitles || _subtitle == null) return;
            _subtitle.SetText(text);
            _subtitleUntil = Time.time + seconds;
        }

        private void LateUpdate()
        {
            if (Head == null || _root == null) return;
            // lazy follow: the HUD trails the head slightly so it does not feel glued to the eyes
            Vector3 target = Head.position + Head.forward * Distance;
            _root.position = Vector3.Lerp(_root.position, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
            _root.rotation = Quaternion.Slerp(_root.rotation, Quaternion.LookRotation(Head.forward, Vector3.up), 1f - Mathf.Exp(-10f * Time.deltaTime));
            _root.localScale = Vector3.one * WintrySettings.Current.Accessibility.TextSize;
            if (_subtitle != null && Time.time > _subtitleUntil && _subtitle.Text.Length > 0) _subtitle.SetText("");
        }
    }
}
