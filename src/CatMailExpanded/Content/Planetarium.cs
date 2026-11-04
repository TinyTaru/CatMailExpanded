using System;
using System.Collections.Generic;
using System.Text;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The planetarium: a domed wing built in the grassy nook west of the pile corridor, entered through a new
    /// door across from the break room. Inside, a painted night sky turns slowly overhead, an orrery of planets
    /// circles a little sun, black-light tubes glow along the walls, and starry shelves (with working storage
    /// labels) hold otherworldly parcels. Otherworldly parcels must be stored here or they get damaged, like cold
    /// parcels outside the cold room; in here their whites glow (BlackLight).
    /// </summary>
    internal static class Planetarium
    {
        // interior (keep in sync with Art/Blender/planetarium.py)
        public const float IX0 = -1.6f, IX1 = 4.25f, IZ0 = 21.2f, IZ1 = 26.9f, FY = 0.04f, WH = 3.8f;
        const float OX0 = IX0 - 0.2f, OZ0 = IZ0 - 0.2f, OZ1 = IZ1 + 0.2f;
        const float EX = 4.43f;                        // the corridor's west wall plane
        internal const float HallFace = 4.71f;         // its visible hallway surface (and the dock columns' faces)
        const float OpenZ0 = 23.18f, OpenZ1 = 24.83f, OpenTop = 2.76f;   // door opening inside the frame
        const float DomeRadius = 2.7f;
        public static readonly Vector3 Centre = new((IX0 + IX1) / 2, 0f, (IZ0 + IZ1) / 2);
        // the yard's door models are authored at the storage corridor's service door; this moves them here
        static readonly Vector3 DoorModelOffset = new(6.68f, 0f, 22.16f);
        static readonly Vector3 Hinge = new(4.43f, 0f, 23.18f);
        const int StoreIdBase = 300, LabelIdBase = 400;

        sealed class Orbiter
        {
            public Transform Pivot, Body, Anchor;
            public float Speed;
        }

        static GameObject _root;
        static Transform _doorPivot, _skyPivot;
        static readonly List<Orbiter> _orbiters = new();
        static readonly List<Collider> _openingColliders = new();
        static readonly List<Collider> _replacedColliders = new();
        static readonly List<Renderer> _tubes = new();
        static Transform _sun;
        static Light _violet, _sunLight;
        static bool _open;
        static readonly List<string> _log = new();

        public static bool Built => _root;

        public static void Reset()
        {
            _root = null;
            _doorPivot = _skyPivot = _sun = null;
            _orbiters.Clear();
            _openingColliders.Clear();
            _replacedColliders.Clear();
            _tubes.Clear();
            _violet = _sunLight = null;
            _armillary = null;
            _open = false;
            _log.Clear();
        }

        public static void Build()
        {
            if (_root) return;
            try { BuildInternal(); }
            catch (Exception e) { Plugin.Log.Error("Planetarium build failed: " + e); }
        }

        static void BuildInternal()
        {
            _root = World.Root("CME_Planetarium");
            var t = _root.transform;

            // --- structure (authored in world coordinates)
            var room = World.Mesh("planet_room", "tex_planet_room", t, Vector3.zero, 0);
            room.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            // sky dome: unlit painted nebula, turning slowly about the dome's axis like a projector
            _skyPivot = new GameObject("CME_SkyPivot").transform;
            _skyPivot.SetParent(t, false);
            _skyPivot.position = Centre;
            var sky = new GameObject("CME_planet_sky");
            sky.transform.SetParent(_skyPivot, false);
            sky.transform.position = Vector3.zero;
            sky.AddComponent<MeshFilter>().sharedMesh = ModAssets.Mesh("planet_sky");
            var skyRenderer = sky.AddComponent<MeshRenderer>();
            var skyTex = ModAssets.Texture("tex_planet_sky");
            skyTex.wrapMode = TextureWrapMode.Repeat;
            skyRenderer.sharedMaterial = ModAssets.UnlitMaterial("planet_sky", skyTex, new Color(0.95f, 0.92f, 1.05f));
            skyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            BuildDoor(t);
            CutDoorway();
            MovePainting();
            BuildColliders(t);
            BuildShelves(t);
            BuildOrrery(t);
            BuildDecor(t);
            BuildArmillary(t, new Vector3(3.35f, FY, 22.05f));
            Plugin.Log.Msg("Planetarium built" + (_log.Count > 0 ? ": " + string.Join("; ", _log) : ""));
        }

        // ------------------------------------------------------------------ door + doorway

        static void BuildDoor(Transform t)
        {
            FitDoorFrame(World.Mesh("door_frame", "tex_door_frame", t, DoorModelOffset, 0)).name = "CME_PlanetDoorFrame";
            var pivot = new GameObject("CME_PlanetDoorPivot");
            pivot.transform.SetParent(t, false);
            pivot.transform.position = Hinge;
            World.Mesh("door_leaf", "tex_door_leaf", pivot.transform, Hinge, 0);
            var leaf = World.Box(pivot.transform, "leaf_collider", Hinge + new Vector3(0, 1.37f, 0.83f), new Vector3(0.08f, 2.7f, 1.66f), 0);
            leaf.transform.SetParent(pivot.transform, true);
            _doorPivot = pivot.transform;

            // a starry plaque beside the door so the room reads from the corridor (on the wallpaper's surface, which
            // stands out from the wall plane EX here)
            var signMat = ModAssets.PropMaterial("tex_planet_sign");
            var sign = Shapes.Make("CME_PlanetSign", Shapes.Quad(), signMat, t, new Vector3(HallFace + 0.015f, 1.85f, 25.95f),
                Quaternion.Euler(0, -90f, 0), new Vector3(0.62f, 0.62f, 1f));
            sign.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// The door frame model is authored for the storage corridor's thinner wall. In the pile corridor's west wall
        /// the hallway surface (wallpaper and the dock columns either side of a door) stands at x 4.71-4.72, so the
        /// frame is stretched on its hallway side to cover the wall's cut edges; its room side stays where it is.
        /// </summary>
        internal static GameObject FitDoorFrame(GameObject frame)
        {
            const float RoomSide = 4.20f, AuthoredHall = 4.66f, WantedHall = HallFace + 0.04f;
            float s = (WantedHall - RoomSide) / (AuthoredHall - RoomSide);
            var tr = frame.transform;
            var p = tr.position;
            tr.position = new Vector3(RoomSide - s * (RoomSide - p.x), p.y, p.z);
            tr.localScale = new Vector3(s, 1f, 1f);
            return frame;
        }

        static bool Overlaps(Bounds b, Vector3 min, Vector3 max) =>
            b.max.x > min.x && b.min.x < max.x && b.max.y > min.y && b.min.y < max.y && b.max.z > min.z && b.min.z < max.z;

        /// <summary>Removes the corridor wall (and its skirting beam) where the new door goes.</summary>
        static void CutDoorway()
        {
            var min = new Vector3(EX - 0.13f, -0.2f, 23.02f);
            var max = new Vector3(EX + 0.35f, 3.62f, 24.99f);
            foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!mr || !mr.enabled) continue;
                var path = World.PathOf(mr.transform);
                if (!path.Contains("PostOffice_Environment") || path.Contains("LightBlocker") || path.Contains("Floor")
                    || path.Contains("Ceiling") || path.Contains("PaintingSlot")) continue;
                if (mr.GetComponentInParent<Entity>()) continue;
                if (!Overlaps(mr.bounds, min, max)) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                try
                {
                    int n = MeshCutter.Cut(mf, min, max);
                    if (n > 0) _log.Add($"cut {n} tris from {mr.name}");
                }
                catch (Exception)
                {
                    // not readable as it is: expose its buffers and try again once Unity has re-uploaded them
                    MeshCutter.PrepareRead(mf.sharedMesh);
                    var target = mf;
                    Scheduler.After(0.3f, () => RetryCut(target, path, min, max, 1));
                }
            }

            SplitColliders();
        }

        static void RetryCut(MeshFilter mf, string path, Vector3 min, Vector3 max, int attempt)
        {
            if (!mf || !_root) return;
            try
            {
                int n = MeshCutter.Cut(mf, min, max);
                Plugin.Log.Msg($"Planetarium doorway: cut {n} triangles from {mf.name}");
            }
            catch (Exception e)
            {
                if (attempt < 4) Scheduler.After(0.5f, () => RetryCut(mf, path, min, max, attempt + 1));
                else Plugin.Log.Warning($"Doorway cut failed on {path}: {e.Message}");
            }
        }

        static void SplitColliders()
        {
            // colliders: split anything blocking the opening into pieces around it
            var oMin = new Vector3(EX - 0.12f, 0.1f, OpenZ0 + 0.02f);
            var oMax = new Vector3(EX + 0.4f, OpenTop - 0.05f, OpenZ1 - 0.02f);
            foreach (var c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                if (!c || !c.enabled || c.isTrigger) continue;
                var path = World.PathOf(c.transform);
                if (!path.Contains("p_PileCorridor") || c.GetComponentInParent<Entity>()) continue;
                var b = c.bounds;
                if (!Overlaps(b, oMin, oMax)) continue;
                var parent = _root.transform;   // world-aligned (the originals may sit under rotated, scaled props)
                int layer = c.gameObject.layer;
                void Seg(Vector3 lo, Vector3 hi)
                {
                    if (hi.x - lo.x < 0.01f || hi.y - lo.y < 0.01f || hi.z - lo.z < 0.01f) return;
                    var s = World.Box(parent, "CME_doorway_" + c.name, (lo + hi) / 2, hi - lo, layer);
                    _openingColliders.Add(s);
                }
                Seg(b.min, new Vector3(b.max.x, b.max.y, OpenZ0));
                Seg(new Vector3(b.min.x, b.min.y, OpenZ1), b.max);
                Seg(new Vector3(b.min.x, Mathf.Max(b.min.y, OpenTop), OpenZ0), new Vector3(b.max.x, b.max.y, OpenZ1));
                c.enabled = false;
                _replacedColliders.Add(c);
                _log.Add("split collider " + c.name);
            }
        }

        static void MovePainting()
        {
            var p = World.Find("p_PaintingSlot_WinnerPanel");
            if (!p) return;
            var pos = p.transform.position;
            if (pos.z > 22.2f && pos.z < 25.5f) p.transform.position = new Vector3(pos.x, pos.y, 21.62f);
        }

        // ------------------------------------------------------------------ colliders

        static void BuildColliders(Transform t)
        {
            const int wall = 9;
            float top = WH + 0.4f;
            World.Box(t, "floor", new Vector3((OX0 + EX) / 2, FY - 0.15f, (OZ0 + OZ1) / 2), new Vector3(EX - OX0, 0.3f, OZ1 - OZ0), wall);
            World.Box(t, "ceiling", new Vector3((OX0 + EX) / 2, WH + 0.15f, (OZ0 + OZ1) / 2), new Vector3(EX - OX0, 0.3f, OZ1 - OZ0), wall);
            World.Box(t, "wall_w", new Vector3(OX0 + 0.1f, top / 2, (OZ0 + OZ1) / 2), new Vector3(0.2f, top, OZ1 - OZ0), wall);
            World.Box(t, "wall_s", new Vector3((OX0 + EX) / 2, top / 2, OZ0 + 0.1f), new Vector3(EX - OX0, top, 0.2f), wall);
            World.Box(t, "wall_n", new Vector3((OX0 + EX) / 2, top / 2, OZ1 - 0.1f), new Vector3(EX - OX0, top, 0.2f), wall);
            World.Box(t, "wall_e1", new Vector3((IX1 + EX) / 2, top / 2, (OZ0 + OpenZ0) / 2), new Vector3(EX - IX1, top, OpenZ0 - OZ0), wall);
            World.Box(t, "wall_e2", new Vector3((IX1 + EX) / 2, top / 2, (OpenZ1 + OZ1) / 2), new Vector3(EX - IX1, top, OZ1 - OpenZ1), wall);
            World.Box(t, "wall_e3", new Vector3((IX1 + EX) / 2, (OpenTop + top) / 2, (OpenZ0 + OpenZ1) / 2), new Vector3(EX - IX1, top - OpenTop, OpenZ1 - OpenZ0), wall);
            World.Box(t, "telescope", new Vector3(3.35f, 0.75f, 26.05f), new Vector3(0.7f, 1.45f, 0.7f), 0);
        }

        // ------------------------------------------------------------------ shelves and labels

        static void BuildShelves(Transform t)
        {
            var shelfL = World.Find("p_Shelves_Dark_L", "p_DarkRoom") ?? World.Find("p_Shelves_L");
            var shelfM = World.Find("p_Shelves_Dark_M", "p_DarkRoom") ?? World.Find("p_Shelves_M");
            var tagL = World.Find("prop_StorageTag_Left", "p_DarkRoom");
            var tagM = World.Find("prop_StorageTag_Left (1)", "p_DarkRoom") ?? tagL;
            // M units face their local -z, L units their local +z (as in the dark room)
            var units = new (GameObject tpl, GameObject tag, Vector3 pos, float yaw, Vector3 tagLocal, float tagYaw)[]
            {
                (shelfM, tagM, new Vector3(-0.55f, FY, 26.54f), 0f, new Vector3(-0.79f, 1.60f, -0.22f), 90f),
                (shelfM, tagM, new Vector3(1.30f, FY, 26.54f), 0f, new Vector3(-0.79f, 1.60f, -0.22f), 90f),
                (shelfM, tagM, new Vector3(-1.24f, FY, 24.20f), 270f, new Vector3(-0.79f, 1.60f, -0.22f), 90f),
                (shelfL, tagL, new Vector3(-0.85f, FY, 21.80f), 0f, new Vector3(0.49f, 2.14f, 0.54f), 270f),
                (shelfL, tagL, new Vector3(0.45f, FY, 21.80f), 0f, new Vector3(0.49f, 2.14f, 0.54f), 270f),
            };
            int id = StoreIdBase, label = LabelIdBase;
            foreach (var u in units)
            {
                var unit = Yard.BuildShelf(u.tpl, t, u.pos, u.yaw, ref id, keepLightmap: true);
                if (!unit) continue;
                unit.name = "CME_PlanetShelf";
                DimShelf(unit);
                var rot = Quaternion.Euler(0, u.yaw, 0);
                CloneLabel(u.tag, t, u.pos + rot * u.tagLocal, u.yaw + u.tagYaw, label++);
            }
            // a floor pallet in the gap between the south shelves and the armillary sphere (ids continue after the
            // shelves so earlier saves keep their parcels where they were)
            var pallet = World.Find("p_Floor_Storage_Dark_XL", "p_DarkRoom");
            var palletPos = new Vector3(1.95f, FY, 21.92f);
            var p = Yard.BuildShelf(pallet, t, palletPos, 90f, ref id, keepLightmap: true);
            if (p) { p.name = "CME_PlanetPallet"; DimShelf(p); }
            PalletLabel(p, t, Vector3.forward, label++);
        }

        /// <summary>The copied shelves are lit like the corridor; tone them down to the room's starlight.</summary>
        internal static void DimShelf(GameObject unit)
        {
            var mpb = new MaterialPropertyBlock();
            int lit = Shader.PropertyToID("_DiffuseColor"), shade = Shader.PropertyToID("_DiffuseColor_1");
            for (int i = 0; i < unit.transform.childCount; i++)
            {
                var r = unit.transform.GetChild(i).GetComponent<MeshRenderer>();
                var m = r ? r.sharedMaterial : null;
                if (!m || !m.HasProperty(lit)) continue;
                var c = m.GetColor(lit);
                mpb.Clear();
                mpb.SetColor(lit, new Color(c.r * 0.5f, c.g * 0.48f, c.b * 0.72f, c.a));
                mpb.SetColor(shade, new Color(0.10f, 0.09f, 0.20f, 1f));
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>
        /// A real storage tag: the label entity players click to pick an icon. It gets its own permanent identifier
        /// so the chosen icon saves and syncs like the game's own tags.
        /// </summary>
        /// <summary>
        /// A pallet's label: the game's flat front-mounted tag (as on its low shelves), on the pallet's edge that
        /// faces the room (<paramref name="outward"/>), where stacked boxes never hide it.
        /// </summary>
        internal static void PalletLabel(GameObject pallet, Transform parent, Vector3 outward, int idOffset)
        {
            if (!pallet) return;
            var tag = World.Find("prop_StorageTag_02");
            if (!tag) { _log.Add("no pallet tag template"); return; }
            var b = World.RendererBounds(pallet);
            outward.y = 0;
            outward.Normalize();
            float yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg + 180f;   // the plate faces the tag's local -z
            var edge = b.center + Vector3.Scale(b.extents, new Vector3(Mathf.Abs(outward.x), 0, Mathf.Abs(outward.z))).magnitude * outward;
            var plate = new Vector3(edge.x, b.min.y + 0.13f, edge.z) + outward * 0.015f;
            // the plate sits (0.10, 0.07, -0.01) from the tag's pivot
            CloneLabel(tag, parent, plate - Quaternion.Euler(0, yaw, 0) * new Vector3(0.10f, 0.07f, -0.01f), yaw, idOffset);
        }

        internal static void CloneLabel(GameObject source, Transform parent, Vector3 pos, float yaw, int idOffset)
        {
            if (!source) { _log.Add("no storage tag template"); return; }
            var holder = new GameObject("CME_LabelHolder");
            holder.SetActive(false);
            var clone = UnityEngine.Object.Instantiate(source, holder.transform);
            int id = StoreFactory.IdBase + idOffset;
            clone.name = "CME_StorageTag_" + id;
            foreach (var pid in clone.GetComponentsInChildren<EntityPermanentIdentifier>(true))
                pid._PermanentIdentifier_k__BackingField = id;
            clone.transform.SetParent(parent, false);
            clone.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            UnityEngine.Object.Destroy(holder);
        }

        // ------------------------------------------------------------------ orrery

        static readonly (string tex, float radius, float orbit, float height, float speed, float start, bool ring, float moon)[] Planets =
        {
            ("red", 0.13f, 0.75f, 2.98f, 14f, 30f, false, 0f),
            ("blue", 0.22f, 1.15f, 3.15f, 9f, 150f, false, 0.07f),
            ("pink", 0.18f, 1.58f, 2.86f, 7f, 260f, false, 0f),
            ("amber", 0.34f, 2.12f, 3.35f, 5f, 80f, false, 0f),
            ("ice", 0.20f, 1.25f, 4.25f, 6f, 300f, false, 0.06f),
            ("violet", 0.40f, 1.80f, 4.62f, 4f, 200f, true, 0f),
        };

        static void BuildOrrery(Transform t)
        {
            var brass = ModAssets.PropMaterial("tex_planet_brass");
            var origin = new GameObject("CME_Orrery").transform;
            origin.SetParent(t, false);
            origin.position = Centre;
            float apex = WH + DomeRadius - 0.04f;
            float sunY = 3.05f;
            Shapes.RodBetween("pole", brass, origin, new Vector3(0, apex, 0), new Vector3(0, sunY, 0), 0.03f);

            var sunMat = ModAssets.UnlitMaterial("planet_sun", ModAssets.Texture("tex_planet_sun"), new Color(1.6f, 1.35f, 1.0f));
            _sun = Shapes.Make("CME_Sun", Shapes.Sphere(), sunMat, origin, new Vector3(0, sunY, 0), Quaternion.identity, Vector3.one * 0.3f).transform;
            _sun.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            foreach (var p in Planets)
            {
                var pivot = new GameObject("CME_Orbit_" + p.tex).transform;
                pivot.SetParent(origin, false);
                pivot.localPosition = Vector3.zero;
                pivot.localRotation = Quaternion.Euler(0, p.start, 0);
                // arm from the pole out to the planet
                Shapes.RodBetween("arm", brass, pivot, new Vector3(0, p.height, 0), new Vector3(p.orbit - p.radius * 0.6f, p.height, 0), 0.016f);
                Shapes.Make("collar", Shapes.Rod(), brass, pivot, new Vector3(0, p.height, 0), Quaternion.identity, new Vector3(0.05f, 0.06f, 0.05f));
                var anchor = new GameObject("anchor").transform;
                anchor.SetParent(pivot, false);
                anchor.localPosition = new Vector3(p.orbit, p.height, 0);
                var mat = ModAssets.UnlitMaterial("planet_" + p.tex, ModAssets.Texture("tex_planet_" + p.tex), new Color(0.8f, 0.78f, 0.86f));
                var body = Shapes.Make("CME_Planet_" + p.tex, Shapes.Sphere(), mat, anchor, Vector3.zero, Quaternion.identity, Vector3.one * p.radius).transform;
                body.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _orbiters.Add(new Orbiter { Pivot = pivot, Body = body, Anchor = anchor, Speed = p.speed });
                if (p.ring)
                {
                    var ringTex = ModAssets.Texture("tex_planet_ring");
                    var ringMat = ModAssets.UnlitMaterial("planet_ring", ringTex, new Color(0.7f, 0.66f, 0.85f));
                    var ring = Shapes.Make("ring", Shapes.Ring(p.radius * 1.35f, p.radius * 1.95f), ringMat, anchor, Vector3.zero,
                        Quaternion.Euler(18f, 0, 12f), Vector3.one);
                    ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                if (p.moon > 0)
                {
                    var moonPivot = new GameObject("moon_orbit").transform;
                    moonPivot.SetParent(anchor, false);
                    moonPivot.localPosition = new Vector3(0, 0.12f, 0);
                    var moonMat = ModAssets.UnlitMaterial("planet_moon", ModAssets.Texture("tex_planet_moon"), new Color(0.8f, 0.8f, 0.86f));
                    var moonBody = Shapes.Make("CME_Moon", Shapes.Sphere(24, 12), moonMat, moonPivot, new Vector3(p.radius + 0.16f, 0, 0),
                        Quaternion.identity, Vector3.one * p.moon).transform;
                    moonBody.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _orbiters.Add(new Orbiter { Pivot = moonPivot, Body = moonBody, Speed = 40f });
                }
            }
        }

        // ------------------------------------------------------------------ rug, black-light tubes, lights

        static void BuildDecor(Transform t)
        {
            var rugMat = ModAssets.PropMaterial("tex_planet_rug");
            var rug = Shapes.Make("CME_PlanetRug", Shapes.Disc(), rugMat, t, Centre + new Vector3(0, FY + 0.006f, 0), Quaternion.identity, new Vector3(1.55f, 1f, 1.55f));
            rug.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var tubeMat = ModAssets.UnlitMaterial("planet_tube", ModAssets.Texture("tex_planet_tube"), new Color(1.5f, 0.9f, 2.4f));
            var brass = ModAssets.PropMaterial("tex_planet_brass");
            const float ty = 3.5f;
            void Tube(Vector3 a, Vector3 b, Vector3 wallNormal)
            {
                var r = Shapes.RodBetween("CME_BlackLightTube", tubeMat, t, a, b, 0.032f).GetComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _tubes.Add(r);
                foreach (var e in new[] { a, b })
                    Shapes.RodBetween("CME_TubeCap", brass, t, e - (b - a).normalized * 0.06f, e + (b - a).normalized * 0.04f, 0.045f);
                foreach (var e in new[] { a + (b - a) * 0.15f, a + (b - a) * 0.85f })
                    Shapes.RodBetween("CME_TubeBracket", brass, t, e, e - wallNormal * 0.08f, 0.012f);
            }
            Tube(new Vector3(IX0 + 0.09f, ty, 21.9f), new Vector3(IX0 + 0.09f, ty, 23.3f), Vector3.right);
            Tube(new Vector3(IX0 + 0.09f, ty, 24.9f), new Vector3(IX0 + 0.09f, ty, 26.3f), Vector3.right);
            Tube(new Vector3(-1.0f, ty, IZ1 - 0.09f), new Vector3(0.6f, ty, IZ1 - 0.09f), Vector3.back);
            Tube(new Vector3(2.0f, ty, IZ1 - 0.09f), new Vector3(3.6f, ty, IZ1 - 0.09f), Vector3.back);
            Tube(new Vector3(-1.0f, ty, IZ0 + 0.09f), new Vector3(0.6f, ty, IZ0 + 0.09f), Vector3.forward);
            Tube(new Vector3(2.0f, ty, IZ0 + 0.09f), new Vector3(3.6f, ty, IZ0 + 0.09f), Vector3.forward);

            // no real lights: the game's volumetric fog would turn them into a haze; the room stays dark
        }

        /// <summary>A brass armillary sphere on a pedestal for the corner beside the doorway.</summary>
        static void BuildArmillary(Transform t, Vector3 at)
        {
            var brass = ModAssets.PropMaterial("tex_planet_brass");
            var root = new GameObject("CME_Armillary").transform;
            root.SetParent(t, false);
            root.position = at;
            Shapes.Make("base", Shapes.Rod(24), brass, root, new Vector3(0, 0.04f, 0), Quaternion.identity, new Vector3(0.28f, 0.08f, 0.28f));
            Shapes.Make("foot", Shapes.Rod(24), brass, root, new Vector3(0, 0.11f, 0), Quaternion.identity, new Vector3(0.16f, 0.06f, 0.16f));
            Shapes.Make("stem", Shapes.Rod(), brass, root, new Vector3(0, 0.55f, 0), Quaternion.identity, new Vector3(0.035f, 0.85f, 0.035f));
            Shapes.Make("cup", Shapes.Sphere(16, 8), brass, root, new Vector3(0, 0.98f, 0), Quaternion.identity, new Vector3(0.07f, 0.04f, 0.07f));
            var sphere = new GameObject("rings").transform;
            sphere.SetParent(root, false);
            sphere.localPosition = new Vector3(0, 1.38f, 0);
            sphere.localRotation = Quaternion.Euler(0, 0, 23.5f);           // tilted like the earth's axis
            var torus = Shapes.Torus(0.035f);
            const float r = 0.36f;
            Shapes.Make("meridian", torus, brass, sphere, Vector3.zero, Quaternion.Euler(90, 0, 0), Vector3.one * r);
            Shapes.Make("meridian2", torus, brass, sphere, Vector3.zero, Quaternion.Euler(90, 90, 0), Vector3.one * r * 0.97f);
            Shapes.Make("equator", torus, brass, sphere, Vector3.zero, Quaternion.identity, Vector3.one * r);
            Shapes.Make("ecliptic", torus, brass, sphere, Vector3.zero, Quaternion.Euler(0, 0, 23.5f), Vector3.one * r * 0.94f);
            Shapes.Make("tropicN", torus, brass, sphere, new Vector3(0, r * 0.4f, 0), Quaternion.identity, Vector3.one * r * 0.92f);
            Shapes.Make("tropicS", torus, brass, sphere, new Vector3(0, -r * 0.4f, 0), Quaternion.identity, Vector3.one * r * 0.92f);
            Shapes.RodBetween("axis", brass, sphere, new Vector3(0, -r * 1.25f, 0), new Vector3(0, r * 1.25f, 0), 0.012f);
            var globeMat = ModAssets.UnlitMaterial("armillary_globe", ModAssets.Texture("tex_planet_blue"), new Color(0.75f, 0.75f, 0.85f));
            Shapes.Make("globe", Shapes.Sphere(24, 12), globeMat, sphere, Vector3.zero, Quaternion.identity, Vector3.one * 0.1f);
            // the stand's arm holding the rings at the south pole
            Shapes.RodBetween("arm", brass, root, new Vector3(0, 0.98f, 0), new Vector3(0, 1.38f, 0) + Quaternion.Euler(0, 0, 23.5f) * new Vector3(0, -r, 0), 0.02f);
            _armillary = sphere;
            World.Box(t, "armillary", at + new Vector3(0, 0.8f, 0), new Vector3(0.75f, 1.6f, 0.75f), 0);
        }

        static Transform _armillary;

        // ------------------------------------------------------------------ state

        /// <summary>Opens the door once the planetarium unlocks (instantly when loading a save).</summary>
        public static void SetOpen(bool open, bool animate)
        {
            if (!_root) return;
            if (_open == open && !animate) return;
            _open = open;
            // like the game's own unlockable rooms, the door simply goes away once the room is unlocked
            if (_doorPivot) _doorPivot.gameObject.SetActive(!open);
            if (animate && open) SoundFx.Play(Hinge + new Vector3(0, 1.2f, 0.8f), 1f, "Unlockable_Door_Activation", "Boat_Door");
        }

        public static void Update()
        {
            if (!_root) return;
            float dt = Time.deltaTime;
            var cam = Cameras.View;
            // everything below only matters when someone can see it
            if (!cam || (cam.transform.position - Centre).sqrMagnitude > 20f * 20f) return;
            if (_skyPivot) _skyPivot.Rotate(0f, dt * 0.8f, 0f, Space.World);
            var sunPos = _sun ? _sun.position : Centre;
            foreach (var o in _orbiters)
            {
                if (!o.Pivot) continue;
                o.Pivot.Rotate(0f, dt * o.Speed, 0f, Space.Self);
                if (o.Body)
                {
                    // the planet textures carry their lighting towards local +Z: keep it facing the sun
                    var d = sunPos - o.Body.position;
                    if (d.sqrMagnitude > 1e-6f) o.Body.rotation = Quaternion.LookRotation(d, Vector3.up);
                }
            }
            if (_sun) _sun.Rotate(0f, dt * 6f, 0f, Space.Self);
            if (_armillary) _armillary.Rotate(0f, dt * 5f, 0f, Space.Self);
        }

        public static string Describe()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"built={Built} open={_open} orbiters={_orbiters.Count} tubes={_tubes.Count}");
            foreach (var l in _log) sb.AppendLine("  " + l);
            foreach (var c in _openingColliders) if (c) sb.AppendLine($"  seg {c.name} {c.bounds.min}..{c.bounds.max}");
            return sb.ToString();
        }
    }
}
