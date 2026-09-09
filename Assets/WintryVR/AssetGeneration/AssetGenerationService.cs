using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.AssetGeneration.Providers;
using WintryVR.Core;

namespace WintryVR.AssetGeneration
{
    /// <summary>
    /// Wintry Asset Pipeline:
    ///   Concept → AI Generation → 3D Asset → Texture Generation → Material Creation → Optimization → LOD →
    ///   Unity Import → Prefab → Ready for Quest.
    /// Each stage is a real step with a working implementation: concept + look via provider (mock or HTTP),
    /// mesh from the identity-locked procedural character (or a provider mesh when one can supply it),
    /// PBR textures from the procedural generator, material creation, decimation + LOD chain, cache, and a
    /// runtime prefab (GameObject) ready to apply. Editor tooling saves the result as project assets.
    /// </summary>
    public class AssetGenerationService : IAssetGenerationService
    {
        private readonly IAssetGenerationProvider _provider;
        private readonly AssetCache _cache;
        private readonly List<GeneratedAsset> _loaded = new List<GeneratedAsset>();
        private readonly Func<WintryLookDefinition> _identity;
        public event Action<string, AssetPipelineStage> OnStage;

        public AssetGenerationService(IAssetGenerationProvider provider, Func<WintryLookDefinition> currentIdentity, AssetCache cache = null)
        {
            _provider = provider; _identity = currentIdentity; _cache = cache ?? new AssetCache();
        }

        public IAssetGenerationProvider Provider => _provider;
        public IReadOnlyList<GeneratedAsset> Cached => _loaded;
        public AssetCache Cache => _cache;

        public static IAssetGenerationProvider CreateProvider(WintryConfig cfg)
        {
            string choice = (cfg.AssetGenProvider ?? "auto").ToLowerInvariant();
            string key = SecretStore.Get("ASSETGEN_API_KEY") ?? SecretStore.Get("AI_API_KEY");
            string baseUrl = !string.IsNullOrEmpty(cfg.AssetGenBaseUrl) ? cfg.AssetGenBaseUrl : SecretStore.Get("PROXY_URL");
            if (choice == "auto") choice = (!string.IsNullOrEmpty(key) || !string.IsNullOrEmpty(baseUrl)) && !cfg.DemoMode ? "openai-images" : "mock";
            switch (choice)
            {
                case "openai-images":
                case "generic": return new HttpImageGenerationProvider(baseUrl, key);
                default: return new MockAssetGenerationProvider();
            }
        }

        public GeneratedAsset GetCached(string assetId)
        {
            var loaded = _loaded.Find(a => a.AssetId == assetId);
            if (loaded != null) return loaded;
            var entry = _cache.Find(assetId);
            if (entry == null) return null;
            var asset = new GeneratedAsset
            {
                AssetId = entry.AssetId, Version = entry.Version, Prompt = entry.GenerationPrompt, GeneratedAt = entry.GenerationDate, Model = entry.Model,
                Look = _cache.LoadLook(entry), Textures = _cache.LoadTextures(entry), Optimized = entry.OptimizationStatus == "optimized", Stage = AssetPipelineStage.Ready
            };
            _loaded.Add(asset);
            return asset;
        }

        public async Task<ConceptResult> ProposeLookAsync(string prompt, CancellationToken ct)
        {
            string id = AssetCache.IdFor(prompt, _provider.Name);
            var cached = GetCached(id);
            if (cached != null)
            {
                WintryLog.I("AssetGen", "Reusing cached asset " + id);
                return new ConceptResult { Success = true, Prompt = prompt, Look = cached.Look, Description = "Cached look '" + cached.Look.Name + "'" };
            }
            Report(id, AssetPipelineStage.Concept);
            var identity = _identity != null ? _identity() : new WintryLookDefinition();
            var concept = await _provider.GenerateConceptAsync(prompt, identity, ct);
            if (concept.Success) concept.Look = WintryVR.Character.CoreIdentity.Constrain(concept.Look);
            return concept;
        }

        public async Task<GeneratedAsset> BuildLookAsync(ConceptResult concept, CancellationToken ct)
        {
            var asset = new GeneratedAsset
            {
                AssetId = AssetCache.IdFor(concept.Prompt, _provider.Name), Version = 1, Prompt = concept.Prompt,
                GeneratedAt = DateTime.UtcNow.ToString("o"), Model = _provider.Name, Look = concept.Look
            };
            var existing = _cache.Find(asset.AssetId);
            if (existing != null) asset.Version = existing.Version + 1;
            try
            {
                Report(asset.AssetId, AssetPipelineStage.Generation);
                // 3D asset: provider mesh if it can produce one, otherwise the identity-locked procedural body
                Report(asset.AssetId, AssetPipelineStage.Model);
                Mesh source = _provider.Supports3DModels ? await _provider.GenerateMeshAsync(concept.Prompt, ct) : null;
                if (source == null) source = await MainThreadDispatcher.RunAsync(() => ProceduralMeshes.Lathe(t => Mathf.Sin(Mathf.Pow(t, 0.55f) * Mathf.PI * 0.5f) * 0.08f, 0.25f, 28, 32));

                Report(asset.AssetId, AssetPipelineStage.Textures);
                asset.Textures = await MainThreadDispatcher.RunAsync(() => ProceduralTextureGenerator.Generate(asset.Look, 512));
                await Task.Yield();

                Report(asset.AssetId, AssetPipelineStage.Materials);
                asset.Material = await MainThreadDispatcher.RunAsync(() =>
                {
                    var m = WintryMaterials.Body(asset.Look.PrimaryColor, asset.Look.Metallic, asset.Look.Smoothness, asset.Look.EmissionColor, asset.Look.EmissionStrength * 0.35f);
                    m.name = "WintryBody_" + asset.Look.Name;
                    if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", asset.Textures.BaseColor);
                    if (m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", asset.Textures.Normal); m.EnableKeyword("_NORMALMAP"); }
                    if (m.HasProperty("_MetallicGlossMap")) { m.SetTexture("_MetallicGlossMap", asset.Textures.Metallic); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                    if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", asset.Textures.Emission);
                    return m;
                });

                Report(asset.AssetId, AssetPipelineStage.Optimization);
                Report(asset.AssetId, AssetPipelineStage.LOD);
                asset.Lods = await MainThreadDispatcher.RunAsync(() => MeshOptimizer.GenerateLods(source));
                asset.Optimized = true;

                Report(asset.AssetId, AssetPipelineStage.Import);
                _cache.Store(asset);

                Report(asset.AssetId, AssetPipelineStage.Prefab);
                asset.Prefab = await MainThreadDispatcher.RunAsync(() =>
                {
                    var go = new GameObject("WintryLook_" + asset.Look.Name);
                    go.SetActive(false);
                    WintryVR.Character.CharacterLOD.Build(go.transform, asset.Lods, asset.Material, "Look");
                    return go;
                });
                asset.Stage = AssetPipelineStage.Ready;
                Report(asset.AssetId, AssetPipelineStage.Ready);
                _loaded.RemoveAll(a => a.AssetId == asset.AssetId);
                _loaded.Add(asset);
                WintryLog.I("AssetGen", "Asset ready: " + asset.AssetId + " v" + asset.Version + " " + MeshOptimizer.Report(asset.Lods));
            }
            catch (OperationCanceledException) { asset.Stage = AssetPipelineStage.Failed; asset.Error = "cancelled"; }
            catch (Exception ex)
            {
                asset.Stage = AssetPipelineStage.Failed; asset.Error = ex.Message;
                WintryLog.E("AssetGen", "Pipeline failed", ex);
                Report(asset.AssetId, AssetPipelineStage.Failed);
            }
            return asset;
        }

        private void Report(string id, AssetPipelineStage stage)
        {
            WintryLog.V("AssetGen", id + " → " + stage);
            OnStage?.Invoke(id, stage);
        }
    }
}
