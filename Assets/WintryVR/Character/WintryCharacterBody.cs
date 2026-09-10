using UnityEngine;
using WintryVR.AssetGeneration;
using WintryVR.Core;

namespace WintryVR.Character
{
    /// <summary>
    /// Wintry's full form, built procedurally: a soft teardrop body, a rounded head with a dark visor plate,
    /// two luminous lens-eyes with lids, a small mouth slit, a floating halo ring and two "hand" motes.
    /// The silhouette is the identity; looks only change materials, textures, colours and accents.
    /// Uses LODGroups (4 levels), 3 materials, no transparency on the body, emissive accents baked into
    /// the material — cheap on Quest.
    /// </summary>
    public class WintryCharacterBody : MonoBehaviour
    {
        public CharacterAnimator Animator { get; private set; }
        public FacialExpressionController Face { get; private set; }
        public LipSyncController LipSync { get; private set; }
        public Transform Head { get; private set; }
        public Transform Body { get; private set; }
        public float Height = 0.42f;

        private Material _bodyMat, _visorMat, _eyeMat, _ringMat, _mouthMat;
        private Transform _root;
        private WintryLookDefinition _look;
        private GeneratedTextureSet _textures;
        private readonly System.Collections.Generic.List<Renderer> _renderers = new System.Collections.Generic.List<Renderer>();

        public void Build(WintryLookDefinition look, GeneratedTextureSet textures)
        {
            _look = look; _textures = textures;
            _root = new GameObject("Rig").transform;
            _root.SetParent(transform, false);

            _bodyMat = WintryMaterials.Body(look.PrimaryColor, look.Metallic, look.Smoothness, look.EmissionColor, look.EmissionStrength * 0.35f);
            _visorMat = WintryMaterials.Body(look.SecondaryColor, 0.6f, 0.95f, look.EmissionColor, 0.15f);
            _eyeMat = WintryMaterials.Glow(look.EyeColor, look.EyeColor, 1.6f);
            _mouthMat = WintryMaterials.Glow(look.EmissionColor, look.EmissionColor, 1.2f);
            _ringMat = WintryMaterials.Glow(look.EmissionColor, look.EmissionColor, look.EmissionStrength, 0.85f);
            ApplyTextures(textures);

            // ---- body (teardrop lathe)
            float bodyH = Height * 0.6f;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(_root, false);
            bodyGo.transform.localPosition = new Vector3(0f, bodyH * 0.5f, 0f);
            float soft = look.Softness;
            System.Func<float, float> profile = t =>
            {
                // t=0 bottom (narrow tip), t=1 top (shoulders)
                float r = Mathf.Sin(Mathf.Pow(t, 0.55f) * Mathf.PI * 0.5f);
                r = Mathf.Pow(r, 1.25f - soft * 0.35f);
                float shoulder = 1f - Mathf.Clamp01((t - 0.85f) / 0.15f) * 0.35f;
                return Mathf.Max(0.004f, r * shoulder * Height * 0.19f);
            };
            CharacterLOD.Build(bodyGo.transform, CharacterLOD.LatheLods(profile, bodyH), _bodyMat, "Body");
            Body = bodyGo.transform;
            CollectRenderers(bodyGo);

            // ---- head
            float headR = Height * 0.17f;
            var headGo = new GameObject("Head");
            headGo.transform.SetParent(_root, false);
            headGo.transform.localPosition = new Vector3(0f, bodyH + headR * 0.85f, 0f);
            var headMesh = new GameObject("HeadMesh");
            headMesh.transform.SetParent(headGo.transform, false);
            headMesh.transform.localScale = new Vector3(1f, 0.92f + soft * 0.08f, 0.95f);
            CharacterLOD.Build(headMesh.transform, CharacterLOD.IcosphereLods(headR), _bodyMat, "Head");
            Head = headGo.transform;
            CollectRenderers(headMesh);

            // visor plate: a flattened sphere segment in front of the face
            var visor = new GameObject("Visor");
            visor.transform.SetParent(headGo.transform, false);
            visor.transform.localPosition = new Vector3(0f, -headR * 0.05f, headR * 0.52f);
            visor.transform.localScale = new Vector3(1.05f, 0.78f, 0.55f);
            CharacterLOD.Build(visor.transform, CharacterLOD.IcosphereLods(headR * 0.62f), _visorMat, "Visor");
            CollectRenderers(visor);

            // eyes
            float eyeR = headR * 0.16f;
            var leftEye = MakeEye("EyeL", headGo.transform, new Vector3(-headR * 0.22f, headR * 0.02f, headR * 0.86f), eyeR);
            var rightEye = MakeEye("EyeR", headGo.transform, new Vector3(headR * 0.22f, headR * 0.02f, headR * 0.86f), eyeR);
            // lids (thin visor-coloured caps that scale down with the eye when blinking – handled by scaling the eye itself)

            // brow ring: thin arc above the eyes
            var brow = new GameObject("Brow");
            brow.transform.SetParent(headGo.transform, false);
            brow.transform.localPosition = new Vector3(0f, headR * 0.34f, headR * 0.8f);
            brow.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Ring(headR * 0.42f, headR * 0.46f, 24);
            var browMr = brow.AddComponent<MeshRenderer>(); browMr.sharedMaterial = _mouthMat; browMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            brow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            brow.transform.localScale = new Vector3(1f, 0.35f, 1f);
            _renderers.Add(browMr);

            // mouth slit
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(headGo.transform, false);
            mouth.transform.localPosition = new Vector3(0f, -headR * 0.3f, headR * 0.9f);
            mouth.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Quad(1f, 1f);
            var mouthMr = mouth.AddComponent<MeshRenderer>(); mouthMr.sharedMaterial = _mouthMat; mouthMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mouth.transform.localScale = new Vector3(headR * 0.28f, headR * 0.03f, 1f);
            mouth.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            _renderers.Add(mouthMr);

            // halo ring
            var ring = new GameObject("Halo");
            ring.transform.SetParent(_root, false);
            ring.transform.localPosition = new Vector3(0f, bodyH + headR * 2.05f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Torus(headR * 0.85f, headR * 0.035f, 48, 8);
            var ringMr = ring.AddComponent<MeshRenderer>(); ringMr.sharedMaterial = _ringMat; ringMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.SetActive(look.ShowRing);
            _renderers.Add(ringMr);

            // scan band (vision state)
            var scan = new GameObject("ScanBand");
            scan.transform.SetParent(bodyGo.transform, false);
            scan.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Torus(Height * 0.2f, Height * 0.006f, 40, 6);
            var scanMr = scan.AddComponent<MeshRenderer>(); scanMr.sharedMaterial = _eyeMat; scanMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            scan.SetActive(false);
            _renderers.Add(scanMr);

            // search motes
            var motes = new GameObject("Motes");
            motes.transform.SetParent(_root, false);
            motes.transform.localPosition = new Vector3(0f, bodyH * 0.9f, 0f);
            for (int i = 0; i < 4; i++)
            {
                var m = new GameObject("Mote" + i);
                m.transform.SetParent(motes.transform, false);
                float a = i * Mathf.PI * 0.5f;
                m.transform.localPosition = new Vector3(Mathf.Cos(a), 0.02f * i, Mathf.Sin(a)) * Height * 0.3f;
                m.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Icosphere(Height * 0.012f, 1);
                var mr = m.AddComponent<MeshRenderer>(); mr.sharedMaterial = _eyeMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderers.Add(mr);
            }
            motes.SetActive(false);

            // hand motes: two small glowing spheres floating beside the body
            for (int i = -1; i <= 1; i += 2)
            {
                var hand = new GameObject(i < 0 ? "HandL" : "HandR");
                hand.transform.SetParent(bodyGo.transform, false);
                hand.transform.localPosition = new Vector3(i * Height * 0.24f, bodyH * 0.1f, Height * 0.03f);
                hand.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Icosphere(Height * 0.03f, 2);
                var mr = hand.AddComponent<MeshRenderer>(); mr.sharedMaterial = _bodyMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderers.Add(mr);
            }

            // ---- controllers
            Face = gameObject.AddComponent<FacialExpressionController>();
            Face.LeftEye = leftEye; Face.RightEye = rightEye; Face.Brow = brow.transform; Face.EyeMaterial = _eyeMat; Face.EyeColor = look.EyeColor;

            LipSync = gameObject.AddComponent<LipSyncController>();
            LipSync.Mouth = mouth.transform; LipSync.MouthMaterial = _mouthMat; LipSync.MouthColor = look.EmissionColor;

            Animator = gameObject.AddComponent<CharacterAnimator>();
            Animator.Body = bodyGo.transform; Animator.Head = headGo.transform; Animator.Ring = ring.transform; Animator.ScanBand = scan.transform; Animator.Motes = motes.transform;
            Animator.RingMaterial = _ringMat; Animator.BodyMaterial = _bodyMat; Animator.Face = Face; Animator.Look = look; Animator.LipSync = LipSync;
        }

        private Transform MakeEye(string name, Transform head, Vector3 pos, float r)
        {
            var eye = new GameObject(name);
            eye.transform.SetParent(head, false);
            eye.transform.localPosition = pos;
            eye.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Icosphere(r, 2);
            var mr = eye.AddComponent<MeshRenderer>(); mr.sharedMaterial = _eyeMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderers.Add(mr);
            return eye.transform;
        }

        private void CollectRenderers(GameObject go) { _renderers.AddRange(go.GetComponentsInChildren<Renderer>(true)); }

        private void ApplyTextures(GeneratedTextureSet t)
        {
            if (t == null) return;
            // Body and head share one material and one set of maps at 1:1.
            AssignMaps(_bodyMat, t, Vector2.one);
            // The visor is a small, tight surface: tiling the same maps denser keeps the pattern reading at its
            // own scale instead of stretching four texels across the whole face.
            AssignMaps(_visorMat, t, new Vector2(2.5f, 1.5f));
        }

        /// <summary>
        /// Binds a generated set to a material, covering both the URP Lit names and the built-in ones so the
        /// maps still land when <see cref="WintryMaterials.FindShader"/> has fallen back off URP.
        /// </summary>
        private static void AssignMaps(Material m, GeneratedTextureSet t, Vector2 tiling)
        {
            if (m == null || t == null) return;
            if (t.BaseColor != null)
            {
                if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", t.BaseColor); m.SetTextureScale("_BaseMap", tiling); }
                if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", t.BaseColor); m.SetTextureScale("_MainTex", tiling); }
            }
            if (t.Normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", t.Normal); m.SetTextureScale("_BumpMap", tiling); m.EnableKeyword("_NORMALMAP");
            }
            if (t.Metallic != null && m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", t.Metallic); m.SetTextureScale("_MetallicGlossMap", tiling); m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (t.Occlusion != null && m.HasProperty("_OcclusionMap"))
            {
                m.SetTexture("_OcclusionMap", t.Occlusion); m.SetTextureScale("_OcclusionMap", tiling); m.EnableKeyword("_OCCLUSIONMAP");
            }
            if (t.Emission != null && m.HasProperty("_EmissionMap"))
            {
                m.SetTexture("_EmissionMap", t.Emission); m.SetTextureScale("_EmissionMap", tiling);
            }
        }

        public void ApplyLook(WintryLookDefinition look, GeneratedTextureSet textures)
        {
            if (_textures != null && !ReferenceEquals(_textures, textures)) ProceduralTextureGenerator.Release(_textures);
            _look = look; _textures = textures;
            WintryMaterials.SetColor(_bodyMat, look.PrimaryColor);
            if (_bodyMat.HasProperty("_Metallic")) _bodyMat.SetFloat("_Metallic", look.Metallic);
            if (_bodyMat.HasProperty("_Smoothness")) _bodyMat.SetFloat("_Smoothness", look.Smoothness);
            WintryMaterials.SetEmission(_bodyMat, look.EmissionColor * look.EmissionStrength * 0.35f);
            WintryMaterials.SetColor(_visorMat, look.SecondaryColor);
            WintryMaterials.SetColor(_eyeMat, look.EyeColor); WintryMaterials.SetEmission(_eyeMat, look.EyeColor * 1.6f);
            WintryMaterials.SetColor(_mouthMat, look.EmissionColor); WintryMaterials.SetEmission(_mouthMat, look.EmissionColor * 1.2f);
            WintryMaterials.SetColor(_ringMat, look.EmissionColor); WintryMaterials.SetEmission(_ringMat, look.EmissionColor * look.EmissionStrength);
            if (Animator != null) Animator.Look = look;
            if (Face != null) Face.EyeColor = look.EyeColor;
            if (LipSync != null) LipSync.MouthColor = look.EmissionColor;
            var halo = _root.Find("Halo"); if (halo != null) halo.gameObject.SetActive(look.ShowRing);
            ApplyTextures(textures);
        }

        /// <summary>Scale-based visibility used during the Core↔Character morph.</summary>
        public void SetVisibility(float v)
        {
            v = Mathf.Clamp01(v);
            _root.localScale = Vector3.one * Mathf.Max(0.001f, v);
            bool on = v > 0.01f;
            foreach (var r in _renderers) if (r != null && r.enabled != on) r.enabled = on;
        }

        public int RendererCount => _renderers.Count;

        private void OnDestroy() { ProceduralTextureGenerator.Release(_textures); _textures = null; }
    }
}
