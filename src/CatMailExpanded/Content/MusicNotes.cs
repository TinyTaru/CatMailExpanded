using System.Collections.Generic;
using CatMailExpanded.Core;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Painted music notes that pop out of a speaker in time with the music, drift upward with a gentle sway
    /// and wobble, then fade. Rendered as camera-facing quads in one dynamic mesh (the ParticleSystem
    /// scripting API is stripped from this game build).
    /// </summary>
    internal sealed class MusicNotes
    {
        struct Note
        {
            public Vector3 Pos, Vel;
            public float Age, Life, Size, Phase, Spin;
            public int Frame;
            public Color Col;
        }

        const int Max = 24;
        static Material _material;

        // pastel palette taken from the game's stickers and UI
        static readonly Color[] Palette =
        {
            new(0.58f, 0.44f, 0.95f), // lilac
            new(0.30f, 0.72f, 0.78f), // teal
            new(1.00f, 0.76f, 0.30f), // butter yellow
            new(0.97f, 0.50f, 0.62f), // pink
            new(0.50f, 0.76f, 0.42f), // sage
        };

        readonly GameObject _go;
        readonly Mesh _mesh;
        readonly List<Note> _notes = new();
        readonly Vector3[] _v = new Vector3[Max * 4];
        readonly Vector2[] _uv = new Vector2[Max * 4];
        readonly Color[] _c = new Color[Max * 4];
        readonly int[] _t = new int[Max * 6];
        readonly Transform _source;
        readonly Vector3 _localMouth;
        float _nextSpawn;

        public bool Emitting;

        public MusicNotes(Transform source, Vector3 localMouth)
        {
            _source = source;
            _localMouth = localMouth;
            _go = new GameObject("CME_MusicNotes");
            _mesh = new Mesh { name = "CME_MusicNotes" };
            _mesh.MarkDynamic();
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = _go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Material();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            for (int i = 0; i < Max; i++)
            {
                int v = i * 4, t = i * 6;
                _t[t] = v; _t[t + 1] = v + 1; _t[t + 2] = v + 2;
                _t[t + 3] = v; _t[t + 4] = v + 2; _t[t + 5] = v + 3;
            }
            _nextSpawn = Time.time + Random.Range(0f, 0.6f);
        }

        static Material Material()
        {
            if (_material) return _material;
            Shader sh = Shader.Find("Sprites/Default");
            if (!sh)
                foreach (var s in Resources.FindObjectsOfTypeAll<Shader>())
                    if (s.name == "Sprites/Default") { sh = s; break; }
            _material = new Material(sh) { name = "CME_MusicNotes", renderQueue = 3050 };
            _material.mainTexture = ModAssets.Texture("fx_notes");
            _material.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return _material;
        }

        public static void ResetShared() => _material = null;

        public void Destroy()
        {
            if (_go) Object.Destroy(_go);
            if (_mesh) Object.Destroy(_mesh);
        }

        public void Update(float dt, Camera cam, float beat)
        {
            if (!_source) return;
            float now = Time.time;
            if (Emitting && now >= _nextSpawn && _notes.Count < Max)
            {
                _nextSpawn = now + Random.Range(0.35f, 0.7f);
                var mouth = _source.TransformPoint(_localMouth);
                var fwd = _source.forward;
                var side = _source.right;
                float scale = _source.lossyScale.y;
                _notes.Add(new Note
                {
                    Pos = mouth + side * Random.Range(-0.08f, 0.08f) * scale,
                    Vel = fwd * Random.Range(0.18f, 0.32f) + Vector3.up * Random.Range(0.32f, 0.5f) + side * Random.Range(-0.1f, 0.1f),
                    Life = Random.Range(2.0f, 2.8f),
                    Size = Random.Range(0.24f, 0.33f),
                    Phase = Random.Range(0f, 6.28f),
                    Spin = Random.Range(-1f, 1f),
                    Frame = Random.Range(0, 4),
                    Col = Palette[Random.Range(0, Palette.Length)],
                });
            }

            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                var n = _notes[i];
                n.Age += dt;
                if (n.Age >= n.Life) { _notes.RemoveAt(i); continue; }
                // ease out of the speaker, then float up with a side-to-side sway
                n.Vel = Vector3.Lerp(n.Vel, Vector3.up * 0.22f, dt * 1.5f);
                n.Pos += n.Vel * dt;
                n.Pos += (cam ? cam.transform.right : Vector3.right) * Mathf.Sin(n.Age * 3.2f + n.Phase) * 0.18f * dt;
                _notes[i] = n;
            }

            if (!cam) return;
            var right = cam.transform.right;
            var up = cam.transform.up;
            int count = _notes.Count;
            for (int i = 0; i < Max; i++)
            {
                int v = i * 4;
                if (i >= count) { _v[v] = _v[v + 1] = _v[v + 2] = _v[v + 3] = _source.position; continue; }
                var n = _notes[i];
                float k = n.Age / n.Life;
                // pop in with a little overshoot, gently pulse on the beat, fade out at the end
                float pop = k < 0.12f ? Mathf.Sin(k / 0.12f * Mathf.PI * 0.75f) / 0.707f : 1f;
                float size = n.Size * Mathf.Min(pop, 1.15f) * (1f + beat * 0.08f);
                float alpha = k < 0.08f ? k / 0.08f : (k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f);
                float ang = Mathf.Sin(n.Age * 2.6f + n.Phase) * 0.35f + n.Spin * k * 0.6f;
                float cs = Mathf.Cos(ang) * size * 0.5f, sn = Mathf.Sin(ang) * size * 0.5f;
                var r = right * cs + up * sn;
                var u = -right * sn + up * cs;
                _v[v] = n.Pos - r - u;
                _v[v + 1] = n.Pos - r + u;
                _v[v + 2] = n.Pos + r + u;
                _v[v + 3] = n.Pos + r - u;
                float fx = (n.Frame % 2) * 0.5f, fy = (n.Frame / 2) == 0 ? 0.5f : 0f; // sheet rows: top = frames 0,1
                _uv[v] = new Vector2(fx, fy);
                _uv[v + 1] = new Vector2(fx, fy + 0.5f);
                _uv[v + 2] = new Vector2(fx + 0.5f, fy + 0.5f);
                _uv[v + 3] = new Vector2(fx + 0.5f, fy);
                var c = n.Col; c.a = Mathf.Clamp01(alpha);
                _c[v] = _c[v + 1] = _c[v + 2] = _c[v + 3] = c;
            }
            _mesh.vertices = _v;
            _mesh.uv = _uv;
            _mesh.colors = _c;
            if (_mesh.triangles.Length != _t.Length) _mesh.triangles = _t;
            _mesh.bounds = new Bounds(_source.position, Vector3.one * 8f);
        }
    }
}
