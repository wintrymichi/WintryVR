using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Character
{
    /// <summary>
    /// Wintry has no human face: expressions are conveyed through two luminous lens-eyes (scale, tilt,
    /// brightness, eyelid coverage) and a brow ring. Transitions are eased and subtle.
    /// </summary>
    public class FacialExpressionController : MonoBehaviour
    {
        public Transform LeftEye, RightEye, LeftLid, RightLid, Brow;
        public Material EyeMaterial;
        public Color EyeColor = new Color(0.7f, 0.95f, 1f);

        private FacialExpression _current = FacialExpression.Neutral;
        private Vector2 _eyeScale = Vector2.one, _targetEyeScale = Vector2.one;
        private float _tilt, _targetTilt, _lid, _targetLid, _bright = 1f, _targetBright = 1f, _browY, _targetBrowY;
        private float _blinkTimer = 3f;
        private float _blink;
        private float _baseEye = 1f;
        private Vector3 _browBase;
        private bool _browInit;
        public float TransitionSpeed = 6f;

        public FacialExpression Current => _current;

        public void Set(FacialExpression e)
        {
            _current = e;
            switch (e)
            {
                case FacialExpression.Neutral: _targetEyeScale = Vector2.one; _targetTilt = 0f; _targetLid = 0.05f; _targetBright = 1f; _targetBrowY = 0f; break;
                case FacialExpression.Happy: _targetEyeScale = new Vector2(1.1f, 0.7f); _targetTilt = 4f; _targetLid = 0.2f; _targetBright = 1.3f; _targetBrowY = 0.004f; break;
                case FacialExpression.Curious: _targetEyeScale = new Vector2(1.05f, 1.2f); _targetTilt = -6f; _targetLid = 0f; _targetBright = 1.15f; _targetBrowY = 0.006f; break;
                case FacialExpression.Thinking: _targetEyeScale = new Vector2(0.95f, 0.8f); _targetTilt = 8f; _targetLid = 0.25f; _targetBright = 0.9f; _targetBrowY = 0.002f; break;
                case FacialExpression.Surprised: _targetEyeScale = new Vector2(1.25f, 1.35f); _targetTilt = 0f; _targetLid = 0f; _targetBright = 1.5f; _targetBrowY = 0.009f; break;
                case FacialExpression.Concerned: _targetEyeScale = new Vector2(0.9f, 0.85f); _targetTilt = -10f; _targetLid = 0.3f; _targetBright = 0.7f; _targetBrowY = -0.003f; break;
                case FacialExpression.Excited: _targetEyeScale = new Vector2(1.2f, 1.1f); _targetTilt = 3f; _targetLid = 0f; _targetBright = 1.6f; _targetBrowY = 0.007f; break;
            }
        }

        public void SetEyeColor(Color c) { EyeColor = c; }

        private void Update()
        {
            float k = 1f - Mathf.Exp(-TransitionSpeed * Time.deltaTime);
            _eyeScale = Vector2.Lerp(_eyeScale, _targetEyeScale, k);
            _tilt = Mathf.Lerp(_tilt, _targetTilt, k);
            _lid = Mathf.Lerp(_lid, _targetLid, k);
            _bright = Mathf.Lerp(_bright, _targetBright, k);
            _browY = Mathf.Lerp(_browY, _targetBrowY, k);

            // blink
            _blinkTimer -= Time.deltaTime;
            if (_blinkTimer <= 0f) { _blinkTimer = Random.Range(2.5f, 6f); _blink = 1f; }
            _blink = Mathf.MoveTowards(_blink, 0f, Time.deltaTime * 8f);
            float lid = Mathf.Clamp01(_lid + _blink);

            Apply(LeftEye, LeftLid, -1f, lid);
            Apply(RightEye, RightLid, 1f, lid);
            if (Brow != null)
            {
                if (!_browInit) { _browBase = Brow.localPosition; _browInit = true; }
                Brow.localPosition = _browBase + Vector3.up * _browY;
            }
            if (EyeMaterial != null) WintryMaterials.SetEmission(EyeMaterial, EyeColor * (1.6f * _bright));
        }

        private void Apply(Transform eye, Transform lidT, float side, float lid)
        {
            if (eye == null) return;
            eye.localScale = new Vector3(_baseEye * _eyeScale.x, _baseEye * _eyeScale.y * (1f - lid * 0.9f), _baseEye);
            eye.localRotation = Quaternion.Euler(0f, 0f, _tilt * side);
        }
    }
}
