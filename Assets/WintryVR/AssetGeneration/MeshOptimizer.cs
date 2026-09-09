using System.Collections.Generic;
using UnityEngine;

namespace WintryVR.AssetGeneration
{
    /// <summary>
    /// Mesh optimisation for Quest: vertex-clustering decimation (grid-based), weld, and LOD chain generation.
    /// Vertex clustering is fast and robust (no dependency), good enough for LOD1–LOD3 of stylised assets.
    /// </summary>
    public static class MeshOptimizer
    {
        public const int MaxTrianglesLOD0 = 12000;

        /// <summary>Generates 4 LODs: original (or capped), then ~50%, ~20%, ~7% via increasing cell size.</summary>
        public static Mesh[] GenerateLods(Mesh source)
        {
            var lods = new Mesh[4];
            lods[0] = source.triangles.Length / 3 > MaxTrianglesLOD0 ? Decimate(source, CellForTarget(source, MaxTrianglesLOD0)) : source;
            lods[1] = Decimate(source, CellForTarget(source, source.triangles.Length / 6));
            lods[2] = Decimate(source, CellForTarget(source, source.triangles.Length / 15));
            lods[3] = Decimate(source, CellForTarget(source, source.triangles.Length / 40));
            return lods;
        }

        private static float CellForTarget(Mesh m, int targetTris)
        {
            targetTris = Mathf.Max(24, targetTris);
            float size = m.bounds.size.magnitude;
            // heuristic: triangles scale ~ (size/cell)^2
            return size / Mathf.Sqrt(targetTris) * 1.4f;
        }

        public static Mesh Decimate(Mesh source, float cellSize)
        {
            var verts = source.vertices; var norms = source.normals; var uvs = source.uv; var tris = source.triangles;
            bool hasN = norms != null && norms.Length == verts.Length, hasUv = uvs != null && uvs.Length == verts.Length;
            var clusterIndex = new Dictionary<Vector3Int, int>();
            var map = new int[verts.Length];
            var newVerts = new List<Vector3>(); var newNorms = new List<Vector3>(); var newUvs = new List<Vector2>(); var counts = new List<int>();
            Vector3 min = source.bounds.min;
            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i];
                var key = new Vector3Int(Mathf.FloorToInt((v.x - min.x) / cellSize), Mathf.FloorToInt((v.y - min.y) / cellSize), Mathf.FloorToInt((v.z - min.z) / cellSize));
                if (!clusterIndex.TryGetValue(key, out int idx))
                {
                    idx = newVerts.Count; clusterIndex[key] = idx;
                    newVerts.Add(Vector3.zero); newNorms.Add(Vector3.zero); newUvs.Add(Vector2.zero); counts.Add(0);
                }
                newVerts[idx] += v; if (hasN) newNorms[idx] += norms[i]; if (hasUv) newUvs[idx] += uvs[i]; counts[idx]++;
                map[i] = idx;
            }
            for (int i = 0; i < newVerts.Count; i++)
            {
                newVerts[i] /= counts[i]; newUvs[i] /= counts[i];
                newNorms[i] = newNorms[i].sqrMagnitude > 0 ? newNorms[i].normalized : Vector3.up;
            }
            var newTris = new List<int>(tris.Length);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = map[tris[i]], b = map[tris[i + 1]], c = map[tris[i + 2]];
                if (a == b || b == c || a == c) continue; // collapsed
                newTris.Add(a); newTris.Add(b); newTris.Add(c);
            }
            var mesh = new Mesh { name = source.name + "_dec" };
            mesh.SetVertices(newVerts);
            if (hasN) mesh.SetNormals(newNorms);
            if (hasUv) mesh.SetUVs(0, newUvs);
            mesh.SetTriangles(newTris, 0);
            if (!hasN) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Quest budget report for a mesh set.</summary>
        public static string Report(Mesh[] lods)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < lods.Length; i++) if (lods[i] != null) sb.Append("LOD").Append(i).Append(": ").Append(lods[i].triangles.Length / 3).Append(" tris, ").Append(lods[i].vertexCount).Append(" verts; ");
            return sb.ToString();
        }
    }
}
