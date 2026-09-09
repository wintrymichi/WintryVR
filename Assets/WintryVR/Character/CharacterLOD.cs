using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Character
{
    /// <summary>
    /// Builds a LODGroup with four levels from a set of procedurally generated meshes at decreasing detail.
    /// For the character's lathe/icosphere parts the detail is the subdivision level; for arbitrary meshes
    /// <see cref="WintryVR.AssetGeneration.MeshOptimizer"/> decimates them.
    /// </summary>
    public static class CharacterLOD
    {
        public static readonly float[] ScreenHeights = { 0.35f, 0.15f, 0.06f, 0.01f };

        /// <summary>Creates LOD0..LOD3 renderers under parent using meshes[i] (index = LOD level).</summary>
        public static LODGroup Build(Transform parent, Mesh[] meshes, Material material, string name)
        {
            var group = parent.gameObject.GetComponent<LODGroup>();
            if (group == null) group = parent.gameObject.AddComponent<LODGroup>();
            var lods = new List<LOD>();
            for (int i = 0; i < meshes.Length && i < 4; i++)
            {
                var go = new GameObject(name + "_LOD" + i);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                lods.Add(new LOD(ScreenHeights[i], new Renderer[] { mr }));
            }
            group.SetLODs(lods.ToArray());
            group.RecalculateBounds();
            group.fadeMode = LODFadeMode.None;
            return group;
        }

        public static Mesh[] IcosphereLods(float radius) => new[] { ProceduralMeshes.Icosphere(radius, 3), ProceduralMeshes.Icosphere(radius, 2), ProceduralMeshes.Icosphere(radius, 1), ProceduralMeshes.Icosphere(radius, 0) };

        public static Mesh[] LatheLods(System.Func<float, float> profile, float height)
            => new[] { ProceduralMeshes.Lathe(profile, height, 28, 32), ProceduralMeshes.Lathe(profile, height, 16, 20), ProceduralMeshes.Lathe(profile, height, 8, 12), ProceduralMeshes.Lathe(profile, height, 4, 8) };

        public static int TriangleCount(Mesh[] lods, int level) => lods != null && level < lods.Length && lods[level] != null ? lods[level].triangles.Length / 3 : 0;
    }
}
