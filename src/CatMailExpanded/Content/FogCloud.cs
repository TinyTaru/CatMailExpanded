using System.Collections.Generic;
using CatMailExpanded.Core;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// A soft, drifting cloud of camera-facing puffs rendered as one dynamic mesh.
    /// (Unity's ParticleSystem scripting API is stripped from this game build.)
    /// </summary>
    internal sealed class FogCloud
    {
        struct Puff
        {
            public Vector3 Pos, Vel;
            public float Age, Life, Size, Rot, RotSpeed;
            public int Frame;
        }

        const int MaxPuffs = 64;
        static Material _material;

        readonly GameObject _go;
        readonly Mesh _mesh;
        readonly List<Puff> _puffs = new();
        readonly Vector3[] _verts = new Vector3[MaxPuffs * 4];
        readonly Vector2[] _uvs = new Vector2[MaxPuffs * 4];
        readonly Color[] _cols = new Color[MaxPuffs * 4];
        readonly int[] _tris = new int[MaxPuffs * 6];
        float _spawnAccum;

        public Vector3 Origin;
        public Color Tint = Color.white;
        public float Rate;          // puffs per second
        public float Radius = 0.45f;
        public int Count => _puffs.Count;

        public FogCloud(string name)
        {
            _go = new GameObject(name);
            _mesh = new Mesh { name = name };
            _mesh.MarkDynamic();
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = _go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Material();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            for (int i = 0; i < MaxPuffs; i++)
            {
                int v = i * 4, t = i * 6;
                _tris[t] = v; _tris[t + 1] = v + 1; _tris[t + 2] = v + 2;
                _tris[t + 3] = v; _tris[t + 4] = v + 2; _tris[t + 5] = v + 3;
            }
        }

        static Material Material()
        {
            if (_material) return _material;
            Shader sh = Shader.Find("Sprites/Default");
            if (!sh)
                foreach (var s in Resources.FindObjectsOfTypeAll<Shader>())
                    if (s.name == "Sprites/Default") { sh = s; break; }
            _material = new Material(sh) { name = "CME_ScentFog", renderQueue = 3100 };
            _material.mainTexture = ModAssets.Texture("fx_cloud");
            _material.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return _material;
        }

        public static void ResetShared() => _material = null;

        public void Destroy()
        {
            if (_go) Object.Destroy(_go);
            if (_mesh) Object.Destroy(_mesh);
        }

        public void Update(float dt, Camera cam)
        {
            // spawn
            _spawnAccum += Rate * dt;
            while (_spawnAccum >= 1f && _puffs.Count < MaxPuffs)
            {
                _spawnAccum -= 1f;
                var off = Random.insideUnitSphere * Radius;
                off.y = Mathf.Abs(off.y) * 0.6f;
                _puffs.Add(new Puff
                {
                    Pos = Origin + off,
                    Vel = new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0.03f, 0.12f), Random.Range(-0.12f, 0.12f)),
                    Life = Random.Range(6f, 9.5f),
                    Size = Random.Range(1.7f, 3.0f),
                    Rot = Random.Range(0f, 360f),
                    RotSpeed = Random.Range(-12f, 12f),
                    Frame = Random.Range(0, 4),
                });
            }
            if (_spawnAccum > 1f) _spawnAccum = 1f;

            // simulate
            float t = Time.time;
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var p = _puffs[i];
                p.Age += dt;
                if (p.Age >= p.Life) { _puffs.RemoveAt(i); continue; }
                // gentle swirl so the cloud billows instead of drifting in straight lines
                var swirl = new Vector3(Mathf.Sin(t * 0.4f + p.Rot), 0, Mathf.Cos(t * 0.33f + p.Rot * 1.7f)) * 0.05f;
                p.Vel = Vector3.Lerp(p.Vel, swirl + Vector3.up * 0.02f, dt * 0.25f);
                p.Pos += p.Vel * dt;
                p.Rot += p.RotSpeed * dt;
                _puffs[i] = p;
            }

            // build mesh
            if (!cam) return;
            var right = cam.transform.right;
            var up = cam.transform.up;
            int n = _puffs.Count;
            for (int i = 0; i < MaxPuffs; i++)
            {
                int v = i * 4;
                if (i >= n)
                {
                    _verts[v] = _verts[v + 1] = _verts[v + 2] = _verts[v + 3] = Origin;
                    continue;
                }
                var p = _puffs[i];
                float k = p.Age / p.Life;
                float size = p.Size * Mathf.Lerp(0.5f, 1.55f, Mathf.Sqrt(k));
                float alpha = k < 0.15f ? k / 0.15f * 0.72f : Mathf.Lerp(0.72f, 0f, Mathf.Pow((k - 0.15f) / 0.85f, 1.6f));
                alpha = Mathf.Clamp01(alpha) * Tint.a;
                float rad = p.Rot * Mathf.Deg2Rad;
                float cs = Mathf.Cos(rad) * size * 0.5f, sn = Mathf.Sin(rad) * size * 0.5f;
                var r = right * cs + up * sn;
                var u = -right * sn + up * cs;
                _verts[v] = p.Pos - r - u;
                _verts[v + 1] = p.Pos - r + u;
                _verts[v + 2] = p.Pos + r + u;
                _verts[v + 3] = p.Pos + r - u;
                float fx = (p.Frame % 2) * 0.5f, fy = (p.Frame / 2) * 0.5f;
                _uvs[v] = new Vector2(fx, fy);
                _uvs[v + 1] = new Vector2(fx, fy + 0.5f);
                _uvs[v + 2] = new Vector2(fx + 0.5f, fy + 0.5f);
                _uvs[v + 3] = new Vector2(fx + 0.5f, fy);
                var c = new Color(Tint.r, Tint.g, Tint.b, alpha);
                _cols[v] = _cols[v + 1] = _cols[v + 2] = _cols[v + 3] = c;
            }
            _mesh.vertices = _verts;
            _mesh.uv = _uvs;
            _mesh.colors = _cols;
            if (_mesh.triangles.Length != _tris.Length) _mesh.triangles = _tris;
            _mesh.bounds = new Bounds(Origin, Vector3.one * 12f);
        }
    }
}
