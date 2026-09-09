using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using WintryVR.AssetGeneration;
using WintryVR.AssetGeneration.Providers;
using WintryVR.Character;
using WintryVR.Core;

namespace WintryVR.EditorTools
{
    /// <summary>
    /// Editor front-end for the Wintry Asset Pipeline: pick a variant or describe a look, generate the PBR
    /// texture set, materials and LOD meshes, preview the concept, and save everything as project assets
    /// (Textures/Materials/Meshes/Prefab) under Assets/WintryVR/Generated/&lt;LookName&gt;.
    /// </summary>
    public class WintryAssetPipelineWindow : EditorWindow
    {
        private string _prompt = "frosted white with a soft blue glow";
        private WintryVariant _variant = WintryVariant.Default;
        private int _resolution = 512;
        private ConceptResult _concept;
        private GeneratedAsset _asset;
        private string _status = "";
        private Vector2 _scroll;

        [MenuItem("WintryVR/Wintry Asset Pipeline")]
        public static void Open() => GetWindow<WintryAssetPipelineWindow>("Wintry Asset Pipeline");

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Label("Concept → Generation → 3D → Textures → Materials → Optimization → LOD → Import → Prefab", EditorStyles.helpBox);
            _variant = (WintryVariant)EditorGUILayout.EnumPopup("Base variant", _variant);
            _prompt = EditorGUILayout.TextField("Look prompt", _prompt);
            _resolution = EditorGUILayout.IntPopup("Texture resolution", _resolution, new[] { "256", "512", "1024" }, new[] { 256, 512, 1024 });

            if (GUILayout.Button("1. Propose concept (mock provider, offline)"))
            {
                var provider = new MockAssetGenerationProvider();
                var identity = CharacterVariants.GetLook(_variant);
                _concept = provider.GenerateConceptAsync(_prompt, identity, CancellationToken.None).GetAwaiter().GetResult();
                _concept.Look = CoreIdentity.Constrain(_concept.Look);
                _status = _concept.Description;
            }
            if (_concept != null && _concept.ConceptImage != null)
            {
                GUILayout.Label(_concept.ConceptImage, GUILayout.Width(192), GUILayout.Height(192));
                EditorGUILayout.LabelField("Look", _concept.Look.Name + " · " + _concept.Look.TexturePreset);
            }
            using (new EditorGUI.DisabledScope(_concept == null))
            {
                if (GUILayout.Button("2. Build textures, material, LODs"))
                {
                    var look = _concept.Look;
                    var textures = ProceduralTextureGenerator.Generate(look, _resolution);
                    var mat = WintryMaterials.Body(look.PrimaryColor, look.Metallic, look.Smoothness, look.EmissionColor, look.EmissionStrength * 0.35f);
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", textures.BaseColor);
                    if (mat.HasProperty("_BumpMap")) { mat.SetTexture("_BumpMap", textures.Normal); mat.EnableKeyword("_NORMALMAP"); }
                    if (mat.HasProperty("_MetallicGlossMap")) { mat.SetTexture("_MetallicGlossMap", textures.Metallic); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                    if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", textures.Emission);
                    var source = ProceduralMeshes.Lathe(t => Mathf.Sin(Mathf.Pow(t, 0.55f) * Mathf.PI * 0.5f) * 0.08f, 0.25f, 28, 32);
                    _asset = new GeneratedAsset
                    {
                        AssetId = AssetCache.IdFor(_prompt, "editor"), Version = 1, Prompt = _prompt, GeneratedAt = System.DateTime.UtcNow.ToString("o"), Model = "editor-procedural",
                        Look = look, Textures = textures, Material = mat, Lods = MeshOptimizer.GenerateLods(source), Optimized = true, Stage = AssetPipelineStage.Ready
                    };
                    _status = "Built. " + MeshOptimizer.Report(_asset.Lods);
                }
            }
            using (new EditorGUI.DisabledScope(_asset == null))
            {
                if (GUILayout.Button("3. Save as project assets (Textures / Material / Meshes / Prefab)")) SaveAssets();
            }
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void SaveAssets()
        {
            string safe = _asset.Look.Name.Replace(' ', '_');
            string dir = "Assets/WintryVR/Generated/" + safe;
            Directory.CreateDirectory(dir);
            void SaveTex(Texture2D t, string name)
            {
                if (t == null) return;
                string path = dir + "/" + name + ".png";
                File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = name == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    imp.sRGBTexture = name == "BaseColor" || name == "Emission";
                    imp.mipmapEnabled = true; imp.textureCompression = TextureImporterCompression.Compressed; imp.maxTextureSize = 1024;
                    imp.SaveAndReimport();
                }
            }
            SaveTex(_asset.Textures.BaseColor, "BaseColor"); SaveTex(_asset.Textures.Normal, "Normal"); SaveTex(_asset.Textures.Roughness, "Roughness");
            SaveTex(_asset.Textures.Metallic, "MetallicSmoothness"); SaveTex(_asset.Textures.Emission, "Emission"); SaveTex(_asset.Textures.Detail, "Detail");
            AssetDatabase.Refresh();
            var mat = new Material(_asset.Material);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/BaseColor.png"));
            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/Normal.png"));
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/MetallicSmoothness.png"));
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/Emission.png"));
            AssetDatabase.CreateAsset(mat, dir + "/WintryBody_" + safe + ".mat");
            for (int i = 0; i < _asset.Lods.Length; i++) AssetDatabase.CreateAsset(Object.Instantiate(_asset.Lods[i]), dir + "/Body_LOD" + i + ".asset");
            AssetDatabase.SaveAssets();
            var meshes = new Mesh[_asset.Lods.Length];
            for (int i = 0; i < meshes.Length; i++) meshes[i] = AssetDatabase.LoadAssetAtPath<Mesh>(dir + "/Body_LOD" + i + ".asset");
            var go = new GameObject("WintryLook_" + safe);
            CharacterLOD.Build(go.transform, meshes, mat, "Body");
            PrefabUtility.SaveAsPrefabAsset(go, dir + "/WintryLook_" + safe + ".prefab");
            Object.DestroyImmediate(go);
            var lookJson = JsonUtility.ToJson(_asset.Look, true);
            File.WriteAllText(dir + "/look.json", lookJson);
            AssetDatabase.Refresh();
            _status = "Saved to " + dir;
            Debug.Log("[WintryVR] Asset pipeline output saved to " + dir);
        }
    }
}
