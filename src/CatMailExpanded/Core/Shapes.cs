using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CatMailExpanded.Core
{
    /// <summary>Small procedural meshes (spheres, rings, rods, quads, discs) for runtime-built props.</summary>
    internal static class Shapes
    {
        static readonly Dictionary<string, Mesh> _cache = new();

        static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (_cache.TryGetValue(key, out var m) && m) return m;
            m = make();
            m.name = "CME_" + key;
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return _cache[key] = m;
        }

        static Mesh Build(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            var m = new Mesh();
            m.vertices = v.ToArray();
            m.normals = n.ToArray();
            m.uv = uv.ToArray();
            m.triangles = t.ToArray();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// Unit-radius UV sphere. u = longitude (0.5 at local +Z), v = latitude (1 at the north pole), matching the
        /// planet textures from Tools/make_planetarium_textures.py.
        /// </summary>
        public static Mesh Sphere(int seg = 40, int rings = 20) => Cached($"sphere{seg}_{rings}", () =>
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                float lat = Mathf.PI / 2 - Mathf.PI * j / rings;
                for (int i = 0; i <= seg; i++)
                {
                    float lon = -Mathf.PI + 2 * Mathf.PI * i / seg;
                    var d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                    v.Add(d); n.Add(d); uv.Add(new Vector2((float)i / seg, 1f - (float)j / rings));
                }
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1;
                    // front faces: cross(p1 - p0, p2 - p0) points outwards
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }
            return Build(v, n, uv, t);
        });

        /// <summary>Flat annulus in the XZ plane, both sides; u runs from inner (0) to outer (1) edge.</summary>
        public static Mesh Ring(float inner, float outer, int seg = 64) => Cached($"ring{inner}_{outer}", () =>
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                int b0 = v.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float a = 2 * Mathf.PI * i / seg;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var nm = side == 0 ? Vector3.up : Vector3.down;
                    v.Add(dir * inner); n.Add(nm); uv.Add(new Vector2(0, (float)i / seg));
                    v.Add(dir * outer); n.Add(nm); uv.Add(new Vector2(1, (float)i / seg));
                }
                for (int i = 0; i < seg; i++)
                {
                    int a = b0 + i * 2, b = a + 1, c = a + 2, d = a + 3;
                    if (side == 0) { t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d); }
                    else { t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c); }
                }
            }
            return Build(v, n, uv, t);
        });

        /// <summary>Torus in the XZ plane (major radius 1), tube radius given relative to it.</summary>
        public static Mesh Torus(float tube, int seg = 48, int sides = 8) => Cached($"torus{tube}_{seg}", () =>
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = 2 * Mathf.PI * i / seg;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int j = 0; j <= sides; j++)
                {
                    float b = 2 * Mathf.PI * j / sides;
                    var nm = dir * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    v.Add(dir + nm * tube); n.Add(nm); uv.Add(new Vector2((float)i / seg, (float)j / sides));
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = i * (sides + 1) + j, b = a + 1, c = a + sides + 1, d = c + 1;
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(b); t.Add(d); t.Add(c);
                }
            return Build(v, n, uv, t);
        });

        /// <summary>Unit cylinder along local Y from -0.5 to 0.5, radius 1, open ends capped.</summary>
        public static Mesh Rod(int seg = 12) => Cached($"rod{seg}", () =>
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = 2 * Mathf.PI * i / seg;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d + Vector3.down * 0.5f); n.Add(d); uv.Add(new Vector2((float)i / seg, 0));
                v.Add(d + Vector3.up * 0.5f); n.Add(d); uv.Add(new Vector2((float)i / seg, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                t.Add(a); t.Add(b); t.Add(c); t.Add(c); t.Add(b); t.Add(d);
            }
            foreach (var y in new[] { -0.5f, 0.5f })
            {
                int centre = v.Count;
                var nm = y > 0 ? Vector3.up : Vector3.down;
                v.Add(new Vector3(0, y, 0)); n.Add(nm); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= seg; i++)
                {
                    float a = 2 * Mathf.PI * i / seg;
                    v.Add(new Vector3(Mathf.Cos(a), y, Mathf.Sin(a))); n.Add(nm); uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                }
                for (int i = 0; i < seg; i++)
                {
                    if (y > 0) { t.Add(centre); t.Add(centre + i + 2); t.Add(centre + i + 1); }
                    else { t.Add(centre); t.Add(centre + i + 1); t.Add(centre + i + 2); }
                }
            }
            return Build(v, n, uv, t);
        });

        /// <summary>Flat disc facing up, UV mapped top-down (u along +x, v along +z).</summary>
        public static Mesh Disc(int seg = 64) => Cached($"disc{seg}", () =>
        {
            var v = new List<Vector3> { Vector3.zero }; var n = new List<Vector3> { Vector3.up };
            var uv = new List<Vector2> { new(0.5f, 0.5f) }; var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = 2 * Mathf.PI * i / seg;
                v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); n.Add(Vector3.up);
                uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i < seg; i++) { t.Add(0); t.Add(i + 2); t.Add(i + 1); }
            return Build(v, n, uv, t);
        });

        /// <summary>Unit quad in the local XY plane facing local -Z (towards a viewer looking along +Z).</summary>
        public static Mesh Quad() => Cached("quad", () =>
        {
            var v = new List<Vector3> { new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0), new(0.5f, 0.5f, 0), new(-0.5f, 0.5f, 0) };
            var n = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            var uv = new List<Vector2> { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
            return Build(v, n, uv, new List<int> { 0, 2, 1, 0, 3, 2 });
        });

        public static GameObject Make(string name, Mesh mesh, Material mat, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            return go;
        }

        /// <summary>A rod between two points in the parent's space.</summary>
        public static GameObject RodBetween(string name, Material mat, Transform parent, Vector3 a, Vector3 b, float radius)
        {
            var d = b - a;
            return Make(name, Rod(), mat, parent, (a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, d.normalized),
                new Vector3(radius, d.magnitude, radius));
        }

        public static void Clear() => _cache.Clear();
    }
}
