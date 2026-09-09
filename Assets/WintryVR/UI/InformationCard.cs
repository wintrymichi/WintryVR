using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;
using WintryVR.Spatial;

namespace WintryVR.UI
{
    /// <summary>
    /// Floating information card anchored beside an object (or in a comfortable spot), with a thin leader
    /// line to the object, a title/subtitle, up to four facts and a "Tell me more" action.
    /// </summary>
    public class InformationCard : MonoBehaviour
    {
        public InformationCardData Data { get; private set; }
        private GlassPanel _panel;
        private LineRenderer _leader;
        private ISpatialService _spatial;

        public static InformationCard Show(InformationCardData data, ISpatialService spatial, Transform parent)
        {
            var go = new GameObject("InfoCard_" + data.Title);
            go.transform.SetParent(parent, false);
            var card = go.AddComponent<InformationCard>();
            card.Build(data, spatial);
            return card;
        }

        private void Build(InformationCardData data, ISpatialService spatial)
        {
            Data = data; _spatial = spatial;
            int lines = Mathf.Min(4, data.Lines.Count);
            float height = 0.075f + lines * 0.014f + 0.045f;
            _panel = GlassPanel.Create("Panel", 0.26f, height, spatial.Head);
            _panel.transform.SetParent(transform, false);
            _panel.SetTitle(data.Title ?? "");
            var body = new List<string>();
            if (!string.IsNullOrEmpty(data.Subtitle)) body.Add(data.Subtitle);
            for (int i = 0; i < lines; i++) body.Add("• " + data.Lines[i]);
            if (!string.IsNullOrEmpty(data.SourceUrl)) { try { body.Add(new System.Uri(data.SourceUrl).Host); } catch { } }
            _panel.SetBody(string.Join("\n", body));
            if (!string.IsNullOrEmpty(data.ActionQuery))
                _panel.AddButton(string.IsNullOrEmpty(data.ActionLabel) ? Localization.Get("card.more") : data.ActionLabel, () => WintryEvents.Publish(new UserMessageEvent { Text = data.ActionQuery }), 0.11f);
            _panel.OnClosed = () => Destroy(gameObject);

            Vector3 pos = data.HasWorldPosition ? WorldFirstLayout.BesideObject(spatial, data.WorldPosition, 0.12f) : WorldFirstLayout.SidePosition(spatial, WintrySettings.Current.MR.UIDistance, true, 0f);
            pos = WorldFirstLayout.ResolveOcclusion(spatial, pos, 0.14f);
            _panel.transform.position = pos;

            if (data.HasWorldPosition)
            {
                var lgo = new GameObject("Leader");
                lgo.transform.SetParent(transform, false);
                _leader = lgo.AddComponent<LineRenderer>();
                _leader.positionCount = 2; _leader.startWidth = 0.002f; _leader.endWidth = 0.004f;
                _leader.material = WintryMaterials.Glow(new Color(0.6f, 0.9f, 1f), new Color(0.6f, 0.9f, 1f), 1.2f, 0.5f);
                _leader.useWorldSpace = true;
                _leader.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void Update()
        {
            if (_leader != null && _panel != null)
            {
                _leader.SetPosition(0, _panel.transform.position - _panel.transform.up * (_panel.Height * 0.5f * _panel.transform.localScale.y));
                _leader.SetPosition(1, Data.WorldPosition);
            }
            // world-first: if the card drifts into the centre of the view (user turned), nudge it sideways
            if (_panel != null && _spatial != null && WorldFirstLayout.IsCoveringCentre(_spatial, _panel.transform.position, 0.14f))
            {
                Vector3 target = WorldFirstLayout.ResolveOcclusion(_spatial, _panel.transform.position, 0.14f);
                _panel.transform.position = Vector3.Lerp(_panel.transform.position, target, 1f - Mathf.Exp(-2f * Time.deltaTime));
            }
        }

        public void Close() { _panel?.Close(); }
    }

    /// <summary>Keeps a small number of cards alive; the world stays the protagonist.</summary>
    public class InformationCardManager : MonoBehaviour
    {
        public int MaxCards = 3;
        public ISpatialService Spatial;
        private readonly List<InformationCard> _cards = new List<InformationCard>();

        public InformationCard Show(InformationCardData data)
        {
            _cards.RemoveAll(c => c == null);
            // replace an existing card for the same object
            var same = _cards.Find(c => c.Data.ObjectId != null && c.Data.ObjectId == data.ObjectId);
            if (same != null) { same.Close(); _cards.Remove(same); }
            while (_cards.Count >= MaxCards) { _cards[0].Close(); _cards.RemoveAt(0); }
            var card = InformationCard.Show(data, Spatial, transform);
            _cards.Add(card);
            return card;
        }

        public void ClearAll()
        {
            foreach (var c in _cards) if (c != null) c.Close();
            _cards.Clear();
        }
    }
}
