using UnityEngine;
using WintryVR.Core;

namespace WintryVR.UI
{
    /// <summary>
    /// Routes pointer input (hands, controllers, gaze fallback, editor mouse) to spatial UI: hover, click,
    /// and pinch-hold dragging of panels. Uses plain physics raycasts against button/panel trigger colliders.
    /// </summary>
    public class UIInteractionManager : MonoBehaviour
    {
        public IInputService Input;
        public float MaxDistance = 4f;
        private WintryButton _hovered;
        private GlassPanel _dragging;
        private float _dragDistance;
        private Vector3 _dragOffset;
        private bool _subscribed;

        public void Initialize(IInputService input)
        {
            Input = input;
            if (!_subscribed) { Input.OnGesture += OnGesture; _subscribed = true; }
        }

        private void OnGesture(GestureEvent g)
        {
            switch (g.Type)
            {
                case GestureType.PinchSelect:
                    {
                        var btn = RaycastButton(g.PointerRay, out _);
                        if (btn != null) btn.Click();
                        break;
                    }
                case GestureType.PinchHoldStart:
                    {
                        if (Physics.Raycast(g.PointerRay, out RaycastHit hit, MaxDistance, ~0, QueryTriggerInteraction.Collide))
                        {
                            var panel = hit.collider.GetComponent<GlassPanel>();
                            if (panel != null && panel.Movable)
                            {
                                _dragging = panel; _dragDistance = hit.distance;
                                _dragOffset = panel.transform.position - g.PointerRay.GetPoint(hit.distance);
                            }
                        }
                        break;
                    }
                case GestureType.PinchHoldEnd:
                    _dragging = null;
                    break;
            }
        }

        private void Update()
        {
            if (Input == null || !Input.PrimaryPointerValid) { SetHover(null); return; }
            var ray = Input.PrimaryPointerRay;
            if (_dragging != null)
            {
                _dragging.Move(ray.GetPoint(_dragDistance) + _dragOffset);
                return;
            }
            SetHover(RaycastButton(ray, out _));
        }

        private WintryButton RaycastButton(Ray ray, out RaycastHit hit)
        {
            var hits = Physics.RaycastAll(ray, MaxDistance, ~0, QueryTriggerInteraction.Collide);
            WintryButton best = null; float bestDist = float.MaxValue; hit = default;
            foreach (var h in hits)
            {
                var b = h.collider.GetComponent<WintryButton>();
                if (b != null && h.distance < bestDist) { best = b; bestDist = h.distance; hit = h; }
            }
            return best;
        }

        private void SetHover(WintryButton b)
        {
            if (_hovered == b) return;
            if (_hovered != null) _hovered.SetHover(false);
            _hovered = b;
            if (_hovered != null) _hovered.SetHover(true);
        }
    }
}
