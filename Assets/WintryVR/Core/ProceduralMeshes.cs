using System.Collections.Generic;
using UnityEngine;

namespace WintryVR.Core
{
    /// <summary>Small procedural mesh library so the app needs no imported models for its core visuals.</summary>
    public static class ProceduralMeshes
    {
        private static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();

        public static Mesh Icosphere(float radius, int subdivisions)
        {
            string key = "ico_" + radius + "_" + subdivisions;
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            var tris = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            var midCache = new Dictionary<long, int>();
            for (int s = 0; s < subdivisions; s++)
            {
                var newTris = new List<int>(tris.Count * 4);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(verts, midCache, a, b), bc = Mid(verts, midCache, b, c), ca = Mid(verts, midCache, c, a);
                    newTris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = newTris;
            }
            var dir = new List<Vector3>(verts);
            var uv = new List<Vector2>(dir.Count);
            for (int i = 0; i < dir.Count; i++) uv.Add(SphereUv(dir[i]));
            SplitUvSeam(dir, uv, tris);

            var mesh = new Mesh { name = key };
            var vArr = new Vector3[dir.Count]; var nArr = new Vector3[dir.Count];
            for (int i = 0; i < dir.Count; i++) { nArr[i] = dir[i]; vArr[i] = dir[i] * radius; }
            mesh.SetVertices(vArr); mesh.SetNormals(nArr); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            _cache[key] = mesh;
            return mesh;
        }

        private static int Mid(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            long key = ((long)Mathf.Min(a, b) << 32) + Mathf.Max(a, b);
            if (cache.TryGetValue(key, out int idx)) return idx;
            var m = ((verts[a] + verts[b]) * 0.5f).normalized;
            verts.Add(m); idx = verts.Count - 1; cache[key] = idx; return idx;
        }

        private static Vector2 SphereUv(Vector3 d)
        {
            return new Vector2(0.5f + Mathf.Atan2(d.z, d.x) / (2f * Mathf.PI),
                               0.5f + Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) / Mathf.PI);
        }

        /// <summary>
        /// A latitude/longitude mapping wraps u from 1 back to 0 along one meridian. With vertices shared, every
        /// triangle crossing that meridian interpolates u the long way round and replays the whole texture
        /// backwards inside it — a smeared band straight down the head. The poles have the same problem in the
        /// other axis: a single vertex there carries one arbitrary u for every triangle that meets it.
        /// Both are fixed by duplicating the offending vertices: seam vertices get a copy at u + 1, and each
        /// pole triangle gets its own apex placed at the midpoint of the edge opposite it.
        /// </summary>
        private static void SplitUvSeam(List<Vector3> dir, List<Vector2> uv, List<int> tris)
        {
            var wrapped = new Dictionary<int, int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                float uMax = Mathf.Max(uv[tris[i]].x, Mathf.Max(uv[tris[i + 1]].x, uv[tris[i + 2]].x));
                for (int k = 0; k < 3; k++)
                {
                    int v = tris[i + k];
                    if (uv[v].x >= uMax - 0.5f) continue;      // on the near side of the seam already
                    if (!wrapped.TryGetValue(v, out int copy))
                    {
                        dir.Add(dir[v]);
                        uv.Add(new Vector2(uv[v].x + 1f, uv[v].y));
                        copy = dir.Count - 1;
                        wrapped[v] = copy;
                    }
                    tris[i + k] = copy;
                }
            }

            for (int i = 0; i < tris.Count; i += 3)
            {
                for (int k = 0; k < 3; k++)
                {
                    int v = tris[i + k];
                    if (Mathf.Abs(dir[v].y) < 0.999f) continue; // not a pole vertex
                    int b = tris[i + (k + 1) % 3], c = tris[i + (k + 2) % 3];
                    dir.Add(dir[v]);
                    uv.Add(new Vector2((uv[b].x + uv[c].x) * 0.5f, uv[v].y));
                    tris[i + k] = dir.Count - 1;
                }
            }
        }

        /// <summary>Flat ring in the XY plane (facing +Z).</summary>
        public static Mesh Ring(float inner, float outer, int segments)
        {
            var verts = new Vector3[segments * 2]; var uv = new Vector2[segments * 2]; var norm = new Vector3[segments * 2];
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                verts[i * 2] = new Vector3(c * inner, s * inner, 0); verts[i * 2 + 1] = new Vector3(c * outer, s * outer, 0);
                uv[i * 2] = new Vector2(i / (float)segments, 0); uv[i * 2 + 1] = new Vector2(i / (float)segments, 1);
                norm[i * 2] = Vector3.back; norm[i * 2 + 1] = Vector3.back;
                int n = (i + 1) % segments;
                tris[i * 6] = i * 2; tris[i * 6 + 1] = n * 2; tris[i * 6 + 2] = i * 2 + 1;
                tris[i * 6 + 3] = i * 2 + 1; tris[i * 6 + 4] = n * 2; tris[i * 6 + 5] = n * 2 + 1;
            }
            var mesh = new Mesh { name = "ring" };
            mesh.vertices = verts; mesh.uv = uv; mesh.normals = norm; mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Torus around the Y axis.</summary>
        public static Mesh Torus(float radius, float tube, int segments, int sides)
        {
            var verts = new Vector3[(segments + 1) * (sides + 1)]; var norm = new Vector3[verts.Length]; var uv = new Vector2[verts.Length];
            var tris = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments * Mathf.PI * 2f;
                Vector3 centre = new Vector3(Mathf.Cos(u) * radius, 0, Mathf.Sin(u) * radius);
                for (int j = 0; j <= sides; j++)
                {
                    float v = j / (float)sides * Mathf.PI * 2f;
                    Vector3 n = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                    int idx = i * (sides + 1) + j;
                    verts[idx] = centre + n * tube; norm[idx] = n; uv[idx] = new Vector2(i / (float)segments, j / (float)sides);
                    if (i < segments && j < sides)
                    {
                        int a = idx, b = idx + sides + 1, c = idx + 1, d = idx + sides + 2;
                        tris.AddRange(new[] { a, c, b, b, c, d });
                    }
                }
            }
            var mesh = new Mesh { name = "torus" };
            mesh.vertices = verts; mesh.normals = norm; mesh.uv = uv; mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>Simple arrow pointing along +Z.</summary>
        public static Mesh Arrow(float width, float length)
        {
            var verts = new[]
            {
                new Vector3(-width * 0.25f, 0, 0), new Vector3(width * 0.25f, 0, 0), new Vector3(width * 0.25f, 0, length * 0.55f), new Vector3(-width * 0.25f, 0, length * 0.55f),
                new Vector3(-width, 0, length * 0.55f), new Vector3(width, 0, length * 0.55f), new Vector3(0, 0, length)
            };
            var tris = new[] { 0, 3, 2, 0, 2, 1, 4, 6, 5 };
            var mesh = new Mesh { name = "arrow" };
            mesh.vertices = verts; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Capsule-like body: a lathe of a profile curve around Y. profile(t) returns radius at height t∈[0,1].</summary>
        public static Mesh Lathe(System.Func<float, float> profile, float height, int rings, int segments)
        {
            var verts = new Vector3[(rings + 1) * (segments + 1)]; var norm = new Vector3[verts.Length]; var uv = new Vector2[verts.Length];
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float y = (t - 0.5f) * height;
                float rad = Mathf.Max(0.0001f, profile(t));
                float radNext = Mathf.Max(0.0001f, profile(Mathf.Clamp01(t + 0.01f)));
                float slope = (radNext - rad) / (0.01f * height);
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                    int idx = r * (segments + 1) + s;
                    verts[idx] = new Vector3(c * rad, y, sn * rad);
                    norm[idx] = new Vector3(c, -slope, sn).normalized;
                    uv[idx] = new Vector2(s / (float)segments, t);
                    if (r < rings && s < segments)
                    {
                        int a0 = idx, b0 = idx + segments + 1, c0 = idx + 1, d0 = idx + segments + 2;
                        tris.AddRange(new[] { a0, b0, c0, c0, b0, d0 });
                    }
                }
            }
            var mesh = new Mesh { name = "lathe" };
            mesh.vertices = verts; mesh.normals = norm; mesh.uv = uv; mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            return mesh;
        }

        public static Mesh Quad(float w, float h)
        {
            var mesh = new Mesh { name = "quad" };
            mesh.vertices = new[] { new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, -h / 2, 0), new Vector3(w / 2, h / 2, 0), new Vector3(-w / 2, h / 2, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Rounded rectangle (XY plane) used for glass panels.</summary>
        public static Mesh RoundedRect(float w, float h, float radius, int cornerSegments)
        {
            var verts = new List<Vector3> { Vector3.zero };
            var uv = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            radius = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
            Vector2[] centres = { new Vector2(w / 2 - radius, h / 2 - radius), new Vector2(-w / 2 + radius, h / 2 - radius), new Vector2(-w / 2 + radius, -h / 2 + radius), new Vector2(w / 2 - radius, -h / 2 + radius) };
            for (int c = 0; c < 4; c++)
                for (int i = 0; i <= cornerSegments; i++)
                {
                    float a = (c * 90f + i * 90f / cornerSegments) * Mathf.Deg2Rad;
                    var p = centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                    verts.Add(new Vector3(p.x, p.y, 0));
                    uv.Add(new Vector2(p.x / w + 0.5f, p.y / h + 0.5f));
                }
            var tris = new List<int>();
            int n = verts.Count - 1;
            for (int i = 1; i <= n; i++) { int next = i % n + 1; tris.AddRange(new[] { 0, next, i }); }
            var mesh = new Mesh { name = "roundedRect" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
            var norms = new Vector3[verts.Count]; for (int i = 0; i < norms.Length; i++) norms[i] = Vector3.back;
            mesh.SetNormals(norms);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
