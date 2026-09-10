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
        private UIPointerCursor _cursor;

        // Physics.RaycastAll returns a fresh array on every call. At 72-90 Hz that is a steady stream of
        // garbage for the collector to sweep up mid-frame, which on a headset shows up as periodic hitching.
        // A reusable buffer keeps the hover test allocation-free.
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        public void Initialize(IInputService input, Transform head = null)
        {
            Input = input;
            if (!_subscribed) { Input.OnGesture += OnGesture; _subscribed = true; }
            if (_cursor == null) _cursor = UIPointerCursor.Create(transform, head);
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
                if (_cursor != null) _cursor.Point(ray, _dragDistance, true);
                return;
            }
            var btn = RaycastButton(ray, out RaycastHit hit);
            SetHover(btn);
            if (_cursor != null)
            {
                // show the cursor on any surface the pointer finds, brighter when it is a control
                if (btn != null) _cursor.Point(ray, hit.distance, true);
                else if (Physics.Raycast(ray, out RaycastHit surface, MaxDistance, ~0, QueryTriggerInteraction.Ignore))
                    _cursor.Point(ray, surface.distance, false);
                else _cursor.Hide();
            }
        }

        private WintryButton RaycastButton(Ray ray, out RaycastHit hit)
        {
            int count = Physics.RaycastNonAlloc(ray, _hits, MaxDistance, ~0, QueryTriggerInteraction.Collide);
            WintryButton best = null; float bestDist = float.MaxValue; hit = default;
            for (int i = 0; i < count; i++)
            {
                var b = _hits[i].collider.GetComponent<WintryButton>();
                if (b != null && _hits[i].distance < bestDist) { best = b; bestDist = _hits[i].distance; hit = _hits[i]; }
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
