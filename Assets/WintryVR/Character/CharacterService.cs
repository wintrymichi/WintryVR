using System.Threading.Tasks;
using UnityEngine;
using WintryVR.AssetGeneration;
using WintryVR.Audio;
using WintryVR.Core;
using WintryVR.Settings;
using WintryVR.Spatial;

namespace WintryVR.Character
{
    /// <summary>
    /// Character Layer entry point. Owns the Core orb and the full character, the morph between them,
    /// Wintry's "personal space" (home position + natural following) and the world-first placement rules.
    /// </summary>
    public class CharacterService : MonoBehaviour, ICharacterService
    {
        public WintryCoreOrb Core { get; private set; }
        public WintryCharacterBody Body { get; private set; }
        public PresenceForm Form { get; private set; } = PresenceForm.Hidden;
        public Transform Root => transform;
        public Vector3 Position => transform.position;
        public WintryVariant Variant { get; private set; } = WintryVariant.Default;
        public bool IsVisible => Form != PresenceForm.Hidden;
        public WintryLookDefinition CurrentLook { get; private set; }

        public float CoreDistance = 0.9f;
        public float CharacterDistance = 1.15f;
        public float FollowLerp = 2.2f;
        public float ReleashDistance = 1.2f;   // how far the user may move before Wintry re-homes
        public float ReleashAngle = 55f;

        private ISpatialService _spatial;
        private SpatialAudioService _audio;
        private AssistantState _state = AssistantState.Idle;
        private Vector3 _home;
        private bool _hasHome;
        private Vector3 _targetPos;
        private bool _morphing;
        private bool _preferRight = true;
        private float _morph; // 0 = core, 1 = character

        /// <summary>
        /// Edge of the generated PBR maps. Wintry is looked at from roughly half a metre, so 256 showed its
        /// texels on the head; 512 does not. Six maps at 512 RGBA32 with mips cost about 8 MB, which is
        /// affordable on both Quest 3 and 3S, and they are generated once per look and cached.
        /// </summary>
        public const int TextureResolution = 512;

        public void Initialize(ISpatialService spatial, SpatialAudioService audio)
        {
            _spatial = spatial; _audio = audio;
            var settings = WintrySettings.Current;
            Variant = settings.CharacterVariant;
            CurrentLook = CharacterVariants.GetLook(Variant);
            var textures = ProceduralTextureGenerator.Generate(CurrentLook, TextureResolution);

            Core = new GameObject("WintryCore").AddComponent<WintryCoreOrb>();
            Core.transform.SetParent(transform, false);
            Core.Build(CurrentLook);

            Body = new GameObject("WintryCharacter").AddComponent<WintryCharacterBody>();
            Body.transform.SetParent(transform, false);
            Body.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            Body.Build(CurrentLook, textures);
            Body.Animator.Initialize(spatial.Head);
            Body.SetVisibility(0f);

            bool reduced = settings.Accessibility.ReducedMotion;
            Core.SetReducedMotion(reduced); Body.Animator.ReducedMotion = reduced;
            WintryEvents.Subscribe<SettingsChangedEvent>(_ =>
            {
                var s = WintrySettings.Current;
                Core.SetReducedMotion(s.Accessibility.ReducedMotion); Body.Animator.ReducedMotion = s.Accessibility.ReducedMotion;
                if (s.CharacterVariant != Variant) ApplyVariant(s.CharacterVariant);
            });

            if (_audio != null) _audio.Attach(transform);
            _targetPos = WorldFirstLayout.SidePosition(_spatial, CoreDistance, _preferRight);
            transform.position = _targetPos;
            ShowCore();
        }

        public void SetState(AssistantState state)
        {
            _state = state;
            Core?.SetState(state);
            Body?.Animator?.SetState(state);
            if (state == AssistantState.Listening) LookAtUser();
        }

        public void SetExpression(FacialExpression expression) { Body?.Face?.Set(expression); }

        public async Task TransformToCharacterAsync()
        {
            if (Form == PresenceForm.Character || _morphing) return;
            _morphing = true; Form = PresenceForm.Transforming;
            WintryEvents.Publish(new PresenceChangedEvent { Previous = PresenceForm.Core, Current = PresenceForm.Transforming });
            _audio?.PlayCue("transform", 0.35f);
            _targetPos = WorldFirstLayout.SidePosition(_spatial, CharacterDistance, _preferRight, -6f);
            float dur = WintrySettings.Current.Accessibility.ReducedMotion ? 0.35f : 0.8f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                _morph = k;
                Core.SetVisibility(1f - k);
                Core.transform.localScale = Vector3.one * (1f + k * 2.5f) * Mathf.Max(0.001f, 1f - k);
                Body.SetVisibility(k);
                await Task.Yield();
            }
            Core.SetVisibility(0f); Body.SetVisibility(1f);
            Form = PresenceForm.Character; _morphing = false;
            LookAtUser();
            SetExpression(FacialExpression.Happy);
            WintryEvents.Publish(new PresenceChangedEvent { Previous = PresenceForm.Transforming, Current = PresenceForm.Character });
        }

        public async Task CollapseToCoreAsync()
        {
            if (Form == PresenceForm.Core || _morphing) return;
            if (Form == PresenceForm.Hidden) { ShowCore(); return; }
            _morphing = true; Form = PresenceForm.Transforming;
            _audio?.PlayCue("collapse", 0.3f);
            _targetPos = WorldFirstLayout.SidePosition(_spatial, CoreDistance, _preferRight);
            float dur = WintrySettings.Current.Accessibility.ReducedMotion ? 0.3f : 0.6f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                _morph = 1f - k;
                Body.SetVisibility(1f - k);
                Core.SetVisibility(k);
                Core.transform.localScale = Vector3.one * k;
                await Task.Yield();
            }
            Body.SetVisibility(0f); Core.SetVisibility(1f); Core.transform.localScale = Vector3.one;
            Form = PresenceForm.Core; _morphing = false;
            WintryEvents.Publish(new PresenceChangedEvent { Previous = PresenceForm.Transforming, Current = PresenceForm.Core });
        }

        public void Hide()
        {
            var prev = Form;
            Core.SetVisibility(0f); Body.SetVisibility(0f); Form = PresenceForm.Hidden;
            WintryEvents.Publish(new PresenceChangedEvent { Previous = prev, Current = PresenceForm.Hidden });
        }

        public void ShowCore()
        {
            var prev = Form;
            Body.SetVisibility(0f); Core.SetVisibility(1f); Core.transform.localScale = Vector3.one; Form = PresenceForm.Core; _morph = 0f;
            if (!_hasHome) { _home = transform.position; _hasHome = true; }
            WintryEvents.Publish(new PresenceChangedEvent { Previous = prev, Current = PresenceForm.Core });
        }

        public void LookAt(Vector3 worldPoint) { Body?.Animator?.LookAt(worldPoint); }
        public void LookAtUser() { Body?.Animator?.LookAtUser(); }

        public void PointAt(Vector3 worldPoint)
        {
            LookAt(worldPoint);
            // lean slightly toward the target
            Vector3 dir = (worldPoint - transform.position); dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 0.5f);
        }

        /// <summary>Sets the current spot as Wintry's home position.</summary>
        public void SetHome() { _home = transform.position; _hasHome = true; }

        public void ReturnHome()
        {
            _targetPos = _hasHome ? _home : WorldFirstLayout.SidePosition(_spatial, CoreDistance, _preferRight);
        }

        public void ApplyVariant(WintryVariant variant)
        {
            Variant = variant;
            ApplyCustomLook(CharacterVariants.GetLook(variant));
            var s = WintrySettings.Current;
            if (s.CharacterVariant != variant) { s.CharacterVariant = variant; s.Save("character"); }
        }

        public void ApplyCustomLook(WintryLookDefinition look)
        {
            CurrentLook = CoreIdentity.Constrain(look);
            var textures = ProceduralTextureGenerator.Generate(CurrentLook, TextureResolution);
            Core.ApplyLook(CurrentLook);
            Body.ApplyLook(CurrentLook, textures);
            WintryLog.I("Character", "Applied look: " + CurrentLook.Name);
        }

        private void Update()
        {
            if (_spatial == null || Form == PresenceForm.Hidden) return;
            var settings = WintrySettings.Current.MR;
            float distance = Form == PresenceForm.Character ? CharacterDistance : CoreDistance;
            distance *= settings.UIDistance / 1.1f;

            // Personal space: keep a natural relation to the user without teleporting in front of the face.
            if (!_morphing)
            {
                Vector3 toWintry = transform.position - _spatial.HeadPosition;
                float dist = toWintry.magnitude;
                float angle = Vector3.Angle(Vector3.ProjectOnPlane(_spatial.GazeDirection, Vector3.up), Vector3.ProjectOnPlane(toWintry, Vector3.up));
                bool tooFar = dist > distance + ReleashDistance;
                bool outOfView = angle > ReleashAngle + 40f;
                bool covering = WorldFirstLayout.IsCoveringCentre(_spatial, transform.position, Form == PresenceForm.Character ? 0.25f : 0.08f);
                if (settings.AnchorBehavior == AnchorBehavior.FollowUser && (tooFar || outOfView))
                {
                    _preferRight = Vector3.Dot(toWintry, _spatial.Head != null ? _spatial.Head.right : Vector3.right) >= 0f;
                    _targetPos = WorldFirstLayout.SidePosition(_spatial, distance, _preferRight, Form == PresenceForm.Character ? -6f : -8f);
                }
                else if (covering)
                {
                    _targetPos = WorldFirstLayout.ResolveOcclusion(_spatial, transform.position, 0.25f);
                }
                else if (settings.AnchorBehavior == AnchorBehavior.ReturnHome && _hasHome && Vector3.Distance(transform.position, _home) > 0.05f && !tooFar)
                {
                    _targetPos = _home;
                }
            }
            // floor clamp: never below waist height, never above eye line by much
            _targetPos.y = Mathf.Clamp(_targetPos.y, _spatial.HeadPosition.y - 0.6f, _spatial.HeadPosition.y + 0.15f);
            transform.position = Vector3.Lerp(transform.position, _targetPos, 1f - Mathf.Exp(-FollowLerp * Time.deltaTime));

            // face the user (yaw only)
            Vector3 face = _spatial.HeadPosition - transform.position; face.y = 0;
            if (face.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face.normalized, Vector3.up), 1f - Mathf.Exp(-3f * Time.deltaTime));

            float size = settings.UISize;
            transform.localScale = Vector3.one * size;
        }
    }
}
