using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WintryVR.Core
{
    public enum AssetPipelineStage { Concept, Generation, Model, Textures, Materials, Optimization, LOD, Import, Prefab, Ready, Failed }

    public class GeneratedTextureSet
    {
        public Texture2D BaseColor;
        public Texture2D Normal;
        public Texture2D Roughness;
        public Texture2D Metallic;
        public Texture2D Emission;
        public Texture2D Detail;
        public int Resolution;
    }

    public class ConceptResult
    {
        public bool Success;
        public string Prompt;
        public Texture2D ConceptImage;      // may be null with mock provider
        public WintryLookDefinition Look;   // parameters derived from the concept
        public string Description;
        public string Error = "";
    }

    /// <summary>External generation backend (image/3D). Mock by default; the Quest does not generate heavy 3D locally.</summary>
    public interface IAssetGenerationProvider
    {
        string Name { get; }
        bool IsConfigured { get; }
        bool RequiresNetwork { get; }
        bool SupportsConceptImages { get; }
        bool Supports3DModels { get; }
        Task<ConceptResult> GenerateConceptAsync(string prompt, WintryLookDefinition coreIdentity, CancellationToken ct);
        Task<Mesh> GenerateMeshAsync(string prompt, CancellationToken ct);
    }

    public class GeneratedAsset
    {
        public string AssetId;
        public int Version;
        public string Prompt;
        public string GeneratedAt;
        public string Model;
        public AssetPipelineStage Stage;
        public WintryLookDefinition Look;
        public GeneratedTextureSet Textures;
        public Material Material;
        public Mesh[] Lods;
        public GameObject Prefab;
        public bool Optimized;
        public string Error = "";
    }

    public interface IAssetGenerationService
    {
        IAssetGenerationProvider Provider { get; }
        Task<ConceptResult> ProposeLookAsync(string prompt, CancellationToken ct);
        Task<GeneratedAsset> BuildLookAsync(ConceptResult concept, CancellationToken ct);
        IReadOnlyList<GeneratedAsset> Cached { get; }
        GeneratedAsset GetCached(string assetId);
    }
}
