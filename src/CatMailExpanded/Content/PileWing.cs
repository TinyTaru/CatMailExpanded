using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The pile corridor, carried on past its old end wall to make room for two more wings: the Catazon returns
    /// room on the right (a copy of the break room's shell, one room further along, entered through a copy of the
    /// break room's arch) and the mirror room on the left (MirrorRoom, behind a door further down). The new
    /// stretch is built from slices of the game's own corridor meshes, baked lighting and all, so it reads as the
    /// same hallway. The old end frame (columns, beam, "infinite" panel) and end wall move to the new end, where
    /// the bottomless pile now sits (PileExtension).
    /// </summary>
    internal static class PileWing
    {
        public const float CX0 = 4.43f, CX1 = 7.69f;       // corridor walls
        public const float OldEnd = 27.02f, End = 38.40f;   // corridor end wall, before and after
        public const float EndShift = End - OldEnd;
        public const float FloorY = 0.04f, CeilY = 4.11f;
        // The old end wall is 0.2 m thick (inner face at 26.84). The corridor is cut just before it; the new stretch
        // [26.80, 34.64) is a copy of [18.96, 26.80) (so the break room's doorway lands on the returns room),
        // [34.64, 38.18) a copy of plain wall from [17.88, 21.42), and the end wall itself moves to [38.18, 38.43].
        const float Cut0 = 26.80f, Seam = 34.64f, EndWallStart = End - 0.22f;
        const float Shift1 = 7.84f, Shift2 = 16.76f;
        // the returns room: the break room's shell moved one room length north
        public const float RoomShift = 7.84f;
        public const float RX0 = 7.69f, RX1 = 12.31f, RZ0 = 25.82f, RZ1 = 33.66f;
        public const float ROpenZ0 = 30.97f, ROpenZ1 = 32.57f, ROpenTop = 2.73f;   // inside the copied arch
        // the mirror room's doorway in the corridor's west wall (MirrorRoom keeps the room itself)
        public const float MOpenZ0 = 34.20f, MOpenZ1 = 35.85f, MOpenTop = 2.76f;
        const int WallLayer = 9;

        static GameObject _root;
        static readonly List<string> _log = new();
        static readonly List<GameObject> _returnsDoor = new();
        static readonly List<Collider> _returnsDoorColliders = new();
        static int _pending;

        public static bool Built => _root;
        public static Transform Root => _root ? _root.transform : null;

        public static void Reset()
        {
            _root = null;
            _log.Clear();
            _returnsDoor.Clear();
            _returnsDoorColliders.Clear();
            _pending = 0;
        }

        public static bool InCorridorExtension(Vector3 p) => p.x > CX0 && p.x < CX1 && p.z > OldEnd && p.z < End && p.y > -0.5f && p.y < CeilY;
        public static bool InGiftRoom(Vector3 p) => p.x > RX0 && p.x < RX1 && p.z > RZ0 && p.z < RZ1 && p.y > -0.5f && p.y < CeilY;

        public static void Build()
        {
            if (_root) return;
            try { BuildInternal(); }
            catch (Exception e) { Plugin.Log.Error("Pile wing build failed: " + e); }
        }

        static void BuildInternal()
        {
            _root = World.Root("CME_PileWing");
            var t = _root.transform;
            var corridor = World.Find("p_PileCorridor", "PostOffice_Environment");
            var breakRoom = World.Find("p_BreakRoom", "PostOffice_Environment");
            if (!corridor || !breakRoom) { Plugin.Log.Warning("Pile wing: corridor or break room not found"); return; }

            MoveEndFrame(corridor);
            SliceCorridor(corridor, t);
            SliceBreakRoom(breakRoom, t);
            CloneLamps(corridor, t);
            BuildColliders(t);
            BuildLightBlockers(t);
            // siding, cornices and a striped skirt on the new wing's outside walls (Art/Blender/mirror.py)
            var ext = World.Mesh("wing_exterior", "tex_wing_exterior", t, Vector3.zero, 0);
            ext.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Plugin.Log.Msg("Pile wing built" + (_log.Count > 0 ? ": " + string.Join("; ", _log) : ""));
        }

        // ------------------------------------------------------------------ the old end frame

        static readonly string[] EndFrameParts =
        {
            "prop_Dock_Column (2)", "prop_Dock_Column (3)", "prop_Dock_Column (4)", "prop_Infinite_Panel",
            "Props/prop_Dock_Column (1)", "Props/prop_Ceiling_Beam (4)", "Colliders/Collider_01",
        };

        static void MoveEndFrame(GameObject corridor)
        {
            foreach (var part in EndFrameParts)
            {
                var tr = corridor.transform.Find(part);
                if (!tr) { _log.Add("no end part " + part); continue; }
                if (tr.position.z > OldEnd + 1f) continue;   // already moved (re-entering the scene)
                tr.position += new Vector3(0, 0, EndShift);
            }
            var blocker = World.Find("LightBlocker (7)", "PostOffice_Environment");
            if (blocker && blocker.transform.position.z < OldEnd + 3f) blocker.transform.position += new Vector3(0, 0, EndShift);
        }

        // ------------------------------------------------------------------ mesh slicing

        sealed class Slice
        {
            public Vector3 Min, Max, Offset;
            public string Tag;
            public List<(Vector3 min, Vector3 max)> Holes = new();
        }

        sealed class Job
        {
            public MeshRenderer Source;
            public List<Slice> Slices = new();
            public List<(Vector3 min, Vector3 max)> SourceCuts = new();
            public Action<GameObject> OnCopy;
            public int Attempt;
        }

        static Vector3 V(float x, float y, float z) => new(x, y, z);

        static bool Skip(MeshRenderer mr, GameObject owner = null)
        {
            if (!mr || !mr.enabled || !mr.gameObject.activeInHierarchy) return true;
            // skip parcels, doors' interactables etc. (the break room itself is an entity: its shell is fine)
            var e = mr.GetComponentInParent<Entity>();
            if (e && (!owner || e.gameObject != owner)) return true;
            var mf = mr.GetComponent<MeshFilter>();
            return !mf || !mf.sharedMesh;
        }

        static void SliceCorridor(GameObject corridor, Transform parent)
        {
            // west wall hole where the planetarium's doorway was cut, as it appears in the first slice
            float hz0 = 23.02f + Shift1, hz1 = 24.99f + Shift1;
            foreach (var mr in corridor.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (Skip(mr)) continue;
                var path = World.PathOf(mr.transform);
                if (path.Contains("prop_Basique_Ceiling_Light")) continue;   // lamps are placed by CloneLamps
                bool moved = mr.bounds.min.z > OldEnd + 1f;                   // the end frame, already moved
                if (moved) continue;
                bool flat = path.Contains("Floor") || path.Contains("Ceiling");
                var job = new Job { Source = mr };
                var s1 = new Slice { Min = V(3.5f, -2f, Cut0 - Shift1), Max = V(8.3f, 6f, Seam - Shift1), Offset = V(0, 0, Shift1), Tag = "_a" };
                var s2 = new Slice { Min = V(3.5f, -2f, Seam - Shift2), Max = V(8.3f, 6f, EndWallStart - Shift2), Offset = V(0, 0, Shift2), Tag = "_b" };
                // fill the copied planetarium doorway with plain wall from further down
                float pShift = hz0 - 19.37f;
                var patch = new Slice { Min = V(4.2f, -0.5f, hz0 - pShift), Max = V(4.85f, 3.62f, hz1 - pShift), Offset = V(0, 0, pShift), Tag = "_patch" };
                // the mirror room's doorway, cut into whichever slices it touches
                var mHole = (V(CX0 - 0.13f, -0.2f, MOpenZ0 - 0.16f), V(CX0 + 0.35f, 3.62f, MOpenZ1 + 0.16f));
                if (!flat)
                {
                    s1.Holes.Add(mHole);
                    s2.Holes.Add(mHole);
                }
                job.Slices.Add(s1);
                job.Slices.Add(s2);
                if (!flat) job.Slices.Add(patch);
                // the old end wall moves to the new end
                if (path.Contains("PileCorridor_Walls"))
                    job.Slices.Add(new Slice { Min = V(3.5f, -2f, EndWallStart - EndShift), Max = V(8.3f, 6f, OldEnd + 0.05f), Offset = V(0, 0, EndShift), Tag = "_end" });
                // and the originals stop where the new stretch begins
                job.SourceCuts.Add((V(3.5f, -2f, Cut0), V(8.3f, 6f, 28.2f)));
                if (path.Contains("prop_PileCorridor_Arch (1)/prop_PileCorridor_Door"))
                    job.OnCopy = go => { if (go.transform.position.z > 28f) _returnsDoor.Add(go); };
                Run(job, parent);
            }
        }

        static void SliceBreakRoom(GameObject breakRoom, Transform parent)
        {
            foreach (var mr in breakRoom.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (Skip(mr, breakRoom)) continue;
                var path = World.PathOf(mr.transform);
                // only the shell: walls, floor, ceiling, beams, rafters, skirting and the ceiling lamp
                bool shell = path.Contains("BreakRoom_Walls") || path.Contains("BreakRoom_Floor") || path.Contains("BreakRoom_Ceiling")
                    || path.Contains("prop_Ceiling_Beam") || path.Contains("Prop/prop_Dock_Column") || path.Contains("prop_Basique_Ceiling_Light");
                if (!shell) continue;
                var job = new Job { Source = mr };
                // everything but the south wall (the break room's north wall already stands there)
                job.Slices.Add(new Slice { Min = V(7.6f, -2f, 17.995f), Max = V(12.5f, 6f, 25.9f), Offset = V(0, 0, RoomShift), Tag = "_r" });
                // the break room's floor runs on under the old porch; the copy covers that now
                if (path.Contains("BreakRoom_Floor"))
                    job.SourceCuts.Add((V(7.6f, -2f, RZ0 + 0.01f), V(13.6f, 0.3f, 28.5f)));
                Run(job, parent);
            }
        }

        static void Run(Job job, Transform parent)
        {
            var mf = job.Source ? job.Source.GetComponent<MeshFilter>() : null;
            if (!mf || !_root) return;
            try
            {
                foreach (var s in job.Slices)
                {
                    var mesh = MeshCutter.Extract(mf, s.Min, s.Max);
                    if (!mesh) continue;
                    var go = Place(job.Source, mesh, s.Offset, s.Tag, parent);
                    var cmf = go.GetComponent<MeshFilter>();
                    foreach (var (hmin, hmax) in s.Holes)
                        if (Overlaps(go.GetComponent<MeshRenderer>().bounds, hmin, hmax)) MeshCutter.Cut(cmf, hmin, hmax);
                    job.OnCopy?.Invoke(go);
                }
                foreach (var (cmin, cmax) in job.SourceCuts)
                    if (Overlaps(job.Source.bounds, cmin, cmax)) MeshCutter.Cut(mf, cmin, cmax);
                if (job.Attempt > 0) Plugin.Log.Msg($"Pile wing: sliced {job.Source.name} (attempt {job.Attempt + 1})");
            }
            catch (Exception e)
            {
                // not readable yet: expose the GPU buffers and try again once Unity has re-uploaded them
                if (job.Attempt >= 5) { Plugin.Log.Warning($"Pile wing: slicing {World.PathOf(job.Source.transform)} failed: {e.Message}"); return; }
                if (job.Attempt == 0) MeshCutter.PrepareRead(mf.sharedMesh);
                job.Attempt++;
                _pending++;
                Scheduler.After(0.4f, () => { _pending--; Run(job, parent); });
            }
        }

        static GameObject Place(MeshRenderer src, Mesh mesh, Vector3 offset, string tag, Transform parent)
        {
            var go = new GameObject("CME_" + src.name + tag);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(src.transform.position + offset, src.transform.rotation);
            go.transform.localScale = src.transform.lossyScale;
            go.layer = src.gameObject.layer;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = src.sharedMaterials;
            r.shadowCastingMode = src.shadowCastingMode;
            r.receiveShadows = src.receiveShadows;
            if (src.lightmapIndex >= 0)
            {
                r.lightmapIndex = src.lightmapIndex;
                r.lightmapScaleOffset = src.lightmapScaleOffset;
            }
            return go;
        }

        static bool Overlaps(Bounds b, Vector3 min, Vector3 max) =>
            b.max.x > min.x && b.min.x < max.x && b.max.y > min.y && b.min.y < max.y && b.max.z > min.z && b.min.z < max.z;

        // ------------------------------------------------------------------ lamps

        /// <summary>Two more of the corridor's hanging lamps, each with a copy of its volumetric light cone.</summary>
        static void CloneLamps(GameObject corridor, Transform parent)
        {
            var lamp = corridor.transform.Find("Props/prop_Basique_Ceiling_Light (1)");
            if (!lamp) { _log.Add("no lamp"); return; }
            // the volumetric light cone hanging under that lamp
            GameObject cone = null;
            float best = 9f;
            foreach (var name in new[] { "p_VolumetricalLight_PileCorridor_01", "p_VolumetricalLight_PileCorridor_02" })
            {
                var c = World.Find(name, "p_Moods");
                if (!c) continue;
                var d = new Vector2(c.transform.position.x - lamp.position.x, c.transform.position.z - lamp.position.z).magnitude;
                if (d < best) { best = d; cone = c; }
            }
            foreach (var z in new[] { 29.6f, 36.3f })
            {
                var offset = new Vector3(0, 0, z - lamp.position.z);
                foreach (var mr in lamp.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (Skip(mr)) continue;
                    Place(mr, mr.GetComponent<MeshFilter>().sharedMesh, offset, "_lamp", parent);
                }
                if (cone)
                {
                    var c = UnityEngine.Object.Instantiate(cone, parent);
                    c.name = "CME_VolumetricLight";
                    c.transform.position = cone.transform.position + offset;
                }
            }
        }

        // ------------------------------------------------------------------ colliders

        static void BuildColliders(Transform t)
        {
            float top = 4.3f;
            void Box(string n, Vector3 lo, Vector3 hi) => World.Box(t, "CME_" + n, (lo + hi) / 2, hi - lo, WallLayer);
            // corridor extension
            Box("ext_floor", V(CX0, -0.34f, OldEnd), V(CX1, FloorY, End));
            Box("ext_ceiling", V(CX0, CeilY, OldEnd), V(CX1, 4.49f, End));
            Box("ext_west_a", V(CX0 - 0.2f, 0, OldEnd), V(CX0, top, MOpenZ0));
            Box("ext_west_b", V(CX0 - 0.2f, 0, MOpenZ1), V(CX0, top, End));
            Box("ext_west_top", V(CX0 - 0.2f, MOpenTop, MOpenZ0), V(CX0, top, MOpenZ1));
            Box("ext_east_a", V(7.48f, 0, OldEnd), V(CX1, top, 30.66f));
            Box("ext_east_b", V(7.48f, 0, 32.88f), V(CX1, top, End));
            Box("ext_east_top", V(7.48f, 3.1f, 30.66f), V(CX1, top, 32.88f));
            // the copied arch's jambs and lintel
            Box("arch_s", V(7.43f, 0.03f, 30.64f), V(7.93f, 2.77f, ROpenZ0));
            Box("arch_n", V(7.43f, 0.03f, ROpenZ1), V(7.93f, 2.77f, 32.89f));
            Box("arch_top", V(7.36f, ROpenTop, 30.49f), V(7.98f, 3.15f, 33.02f));
            // the closed door until the room unlocks
            _returnsDoorColliders.Add(World.Box(t, "CME_returns_door", V(7.68f, 1.38f, (ROpenZ0 + ROpenZ1) / 2), V(0.16f, 2.7f, ROpenZ1 - ROpenZ0), WallLayer));
            // returns room (its south wall is the break room's north wall)
            Box("room_floor", V(RX0, -0.34f, RZ0), V(RX1, FloorY, RZ1));
            Box("room_ceiling", V(RX0, CeilY, RZ0), V(RX1, 4.49f, RZ1));
            Box("room_east", V(11.99f, 0, RZ0), V(12.3f, top, RZ1));
            Box("room_north", V(RX0, 0, 33.41f), V(RX1, top, 33.68f));
            Box("room_west_top", V(7.67f, 3.1f, 30.49f), V(7.93f, top, 33.02f));
        }

        static void BuildLightBlockers(Transform t)
        {
            // the game shades its rooms with shadow-only boxes above the ceilings
            var src = World.Find("LightBlocker (4)", "PostOffice_Environment");
            var mat = src ? src.GetComponent<MeshRenderer>()?.sharedMaterial : null;
            void Blocker(string n, Vector3 lo, Vector3 hi)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "CME_LightBlocker_" + n;
                UnityEngine.Object.Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(t, false);
                go.transform.position = (lo + hi) / 2;
                go.transform.localScale = hi - lo;
                var r = go.GetComponent<MeshRenderer>();
                if (mat) r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            Blocker("corridor", V(4.40f, 4.5f, 30.0f), V(7.75f, 7.1f, End + 0.1f));
            Blocker("returns", V(7.6f, 4.5f, 30.0f), V(12.45f, 7.1f, RZ1 + 0.1f));
            Blocker("mirror", V(MirrorRoom.OX0, 4.2f, MirrorRoom.OZ0), V(CX0, 7.1f, MirrorRoom.OZ1));
        }

        // ------------------------------------------------------------------ the returns room door

        /// <summary>The returns room's door: shut until the room unlocks, then gone (like the game's own rooms).</summary>
        public static void SetGiftRoomOpen(bool open)
        {
            foreach (var go in _returnsDoor) if (go) go.SetActive(!open);
            foreach (var c in _returnsDoorColliders) if (c) c.enabled = !open;
        }

        public static string Describe() =>
            $"wing built={Built} pending={_pending} doorParts={_returnsDoor.Count} log=[{string.Join("; ", _log)}]";
    }
}
