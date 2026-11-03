using System;
using System.Globalization;
using CatMailExpanded.Core;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The scenery around the post office: one ocean surface that reaches the horizon in every direction,
    /// a low-poly coastline whose seabed shelves gently up to sandy beaches, and a forest on the hills
    /// behind them. Replaces the game's rectangular water plane and flat seabed, whose hard edges left
    /// the empty sky sphere showing. Pure decoration: nothing here has colliders or is saved.
    /// </summary>
    internal static class Outdoors
    {
        static GameObject _root;

        // the ocean covers the whole sky sphere (centre (-42.3, 9.8), radius ~78)
        const float OceanX0 = -134f, OceanX1 = 44f, OceanZ0 = -78f, OceanZ1 = 98f;
        const float SeaLevel = -0.39f;

        public static bool Built => _root;

        public static void Reset() => _root = null;

        public static void Build()
        {
            if (_root) return;
            try { BuildInternal(); }
            catch (Exception e) { Plugin.Log.Error("Outdoors build failed: " + e); }
        }

        static void BuildInternal()
        {
            _root = World.Root("CME_Outdoors");
            var t = _root.transform;

            var terrain = new GameObject("CME_Terrain");
            terrain.transform.SetParent(t, false);
            terrain.AddComponent<MeshFilter>().sharedMesh = TerrainMesh();
            var tr = terrain.AddComponent<MeshRenderer>();
            tr.sharedMaterial = ModAssets.PropMaterial("tex_terrain");
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            tr.receiveShadows = true;

            BuildRoof(t);
            BuildOcean(t);
            HideOldSeabed();
            int n = PlaceForest(t);
            Plugin.Log.Msg($"Outdoors built ({n} plants)");
        }

        /// <summary>
        /// Builds the faceted terrain from the embedded height grid (terrain.cmeterr, written by
        /// Art/Blender/terrain.py). Every triangle gets its own vertices so it shades flat, and its
        /// ground type picks a colour from tex_terrain.
        /// </summary>
        static Mesh TerrainMesh()
        {
            var br = new System.IO.BinaryReader(new System.IO.MemoryStream(ModAssets.Bytes("terrain.cmeterr")));
            if (new string(br.ReadChars(4)) != "CMET") throw new System.IO.InvalidDataException("bad terrain");
            br.ReadUInt32();
            int nx = (int)br.ReadUInt32(), nz = (int)br.ReadUInt32();
            float x0 = br.ReadSingle(), z0 = br.ReadSingle(), step = br.ReadSingle();
            var h = new float[nx * nz];
            for (int k = 0; k < h.Length; k++) h[k] = br.ReadSingle();
            // the planetarium wing stands on what was a grassy slope: dig the ground out from under it
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    float x = x0 + i * step, z = z0 + j * step;
                    if (x > -2.6f && x < 4.6f && z > 20.4f && z < 27.8f) h[j * nx + i] = Mathf.Min(h[j * nx + i], -0.35f);
                }
            int cells = (nx - 1) * (nz - 1);
            var cellData = br.ReadBytes(cells * 3);
            int kept = 0;
            for (int c = 0; c < cells; c++) if (cellData[c * 3] != 0) kept++;

            var verts = new Vector3[kept * 6];
            var nrm = new Vector3[kept * 6];
            var uv = new Vector2[kept * 6];
            var idx = new int[kept * 6];
            int v = 0;
            Vector3 P(int i, int j) => new(x0 + i * step, h[j * nx + i], z0 + j * step);
            void Tri(Vector3 a, Vector3 b, Vector3 c, byte type)
            {
                var n = Vector3.Cross(b - a, c - a);
                if (n.y < 0) { (b, c) = (c, b); n = -n; }   // front faces look up (Unity winds clockwise)
                n.Normalize();
                var u = new Vector2(type / 255f, 0.5f);
                verts[v] = a; verts[v + 1] = b; verts[v + 2] = c;
                for (int q = 0; q < 3; q++) { nrm[v + q] = n; uv[v + q] = u; idx[v + q] = v + q; }
                v += 3;
            }
            for (int j = 0; j < nz - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    int c = (j * (nx - 1) + i) * 3;
                    if (cellData[c] == 0) continue;
                    if (((i + j) & 1) == 0)
                    {
                        Tri(P(i, j), P(i + 1, j), P(i + 1, j + 1), cellData[c + 1]);
                        Tri(P(i, j), P(i + 1, j + 1), P(i, j + 1), cellData[c + 2]);
                    }
                    else
                    {
                        Tri(P(i, j), P(i + 1, j), P(i, j + 1), cellData[c + 1]);
                        Tri(P(i + 1, j), P(i + 1, j + 1), P(i, j + 1), cellData[c + 2]);
                    }
                }
            var mesh = new Mesh { name = "CME_Terrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.normals = nrm;
            mesh.uv = uv;
            mesh.triangles = idx;
            mesh.RecalculateBounds();
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return mesh;
        }

        /// <summary>
        /// The game shades its rooms with grey "LightBlocker" boxes sitting on top of the walls; from outside
        /// they read as bare grey blocks. Keep them for their shadows only and show a shingled roof instead
        /// (Art/Blender/roof.py). The roof sits inside or above the boxes, which the rooms are always below,
        /// and casts no shadow of its own, so nothing changes indoors.
        /// </summary>
        static void BuildRoof(Transform parent)
        {
            var holder = World.Find("LightBlocker", "PostOffice_Environment");
            var group = holder ? holder.transform.parent : null;
            int hidden = 0;
            if (group)
                foreach (var r in group.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!World.PathOf(r.transform).Contains("/LightBlocker")) continue;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    hidden++;
                }
            foreach (var (mesh, tex) in new[] { ("roof_shingles", "tex_roof_shingles"), ("roof_trim", "tex_roof_trim") })
            {
                var go = World.Mesh(mesh, tex, parent, Vector3.zero, 0);
                go.name = "CME_" + mesh;
                var mr = go.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }
            // a wall-top beam above the storage corridor ceiling pokes out under the south roof's overhang;
            // it sits above the ceiling (invisible indoors), so just hide it
            var beam = World.Find("prop_Dock_Column (4)", "p_BigRoom");
            var beamRenderer = beam ? beam.GetComponentInChildren<MeshRenderer>() : null;
            if (beamRenderer && beamRenderer.bounds.min.y > 4.5f) beamRenderer.enabled = false;
            Plugin.Log.Msg($"Roof built ({hidden} light blockers now shadow-only)");
        }

        static void BuildOcean(Transform parent)
        {
            var water = World.Find("WaterOutside");
            if (!water)
            {
                Plugin.Log.Warning("WaterOutside not found; ocean skipped");
                return;
            }
            var src = water.GetComponent<MeshRenderer>();
            var wt = water.transform;

            // continue the original plane's UV mapping so ripples keep their size and line up
            Vector2 UvAt(Vector3 world)
            {
                var l = wt.InverseTransformPoint(world);
                return new Vector2(l.x + 0.5f, l.y + 0.5f);
            }

            const int N = 12; // a grid rather than one quad keeps per-vertex fog/depth interpolation tidy
            var verts = new Vector3[(N + 1) * (N + 1)];
            var uvs = new Vector2[verts.Length];
            var nrm = new Vector3[verts.Length];
            for (int j = 0; j <= N; j++)
                for (int i = 0; i <= N; i++)
                {
                    var p = new Vector3(Mathf.Lerp(OceanX0, OceanX1, i / (float)N), SeaLevel, Mathf.Lerp(OceanZ0, OceanZ1, j / (float)N));
                    int k = j * (N + 1) + i;
                    verts[k] = p;
                    uvs[k] = UvAt(p);
                    nrm[k] = Vector3.up;
                }
            var tris = new int[N * N * 6];
            int ti = 0;
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                {
                    int a = j * (N + 1) + i, b = a + 1, c = a + N + 1, d = c + 1;
                    tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                    tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
                }
            var mesh = new Mesh { name = "CME_Ocean" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = nrm;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;

            var go = new GameObject("CME_Ocean");
            go.transform.SetParent(parent, false);
            go.layer = water.layer;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = src.sharedMaterials;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = src.receiveShadows;
            src.enabled = false;
        }

        static void HideOldSeabed()
        {
            foreach (var name in new[] { "BottomOutside", "BottomInside" })
            {
                var go = World.Find(name, "PostOffice_Environment");
                var r = go ? go.GetComponent<MeshRenderer>() : null;
                if (r) r.enabled = false;
            }
        }

        static int PlaceForest(Transform parent)
        {
            var text = ModAssets.Text("forest_layout.txt");
            if (text == null) return 0;
            var holder = new GameObject("CME_Forest").transform;
            holder.SetParent(parent, false);
            int count = 0;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split(' ');
                if (p.Length < 6) continue;
                float F(int i) => float.Parse(p[i], CultureInfo.InvariantCulture);
                // nothing grows where the planetarium wing stands
                if (F(1) > -3.4f && F(1) < 5f && F(3) > 19.9f && F(3) < 28.6f) continue;
                try
                {
                    var go = ModAssets.Prop("syn_" + p[0], "tex_synty_adventure", holder,
                        new Vector3(F(1), F(2), F(3)), Quaternion.Euler(0, F(4), 0), false);
                    go.transform.localScale = Vector3.one * F(5);
                    count++;
                }
                catch (Exception e)
                {
                    Plugin.Log.Warning($"forest prop {p[0]} failed: {e.Message}");
                }
            }
            return count;
        }
    }
}
