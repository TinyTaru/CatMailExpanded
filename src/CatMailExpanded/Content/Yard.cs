using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The outdoor yard: a wooden pier deck behind the storage corridor's service door, with a striped
    /// pergola sheltering three storage shelves, planters, a lamp and a sign. The door stays shut until
    /// the yard unlocks, then swings open.
    /// </summary>
    internal static class Yard
    {
        const float X0 = -13.5f, X1 = -2.45f, Z0 = -9.0f, Z1 = 2.62f;
        const float DoorHingeZ = 0.84f + 0.18f;
        const float DoorX = -2.25f;
        static readonly Vector3 RingPos = new(-2.70f, 1.35f, -6.0f);   // flat on the facade siding, clear of the crates
        const float RingYaw = 270f;
        static readonly Vector3 GarlandPos = new(-10.75f, 2.25f, -2.6f);
        static readonly float[] PoleX = { -12.33f, -8.99f, -5.76f };
        const float PoleZ = -4.0f;

        static GameObject _root;
        static Transform _doorPivot;
        static readonly List<Collider> _doorwayColliders = new();
        static Collider _originalWallCollider;
        static readonly List<Light> _lights = new();
        static bool _open;
        static float _doorAngle, _doorTarget;

        public static bool Built => _root;

        public static void Reset()
        {
            _root = null;
            _doorPivot = null;
            _doorwayColliders.Clear();
            _originalWallCollider = null;
            _lights.Clear();
            _open = false;
            _doorAngle = _doorTarget = 0;
        }

        public static void Build()
        {
            if (_root) return;
            try { BuildInternal(); }
            catch (Exception e) { Plugin.Log.Error("Yard build failed: " + e); }
        }

        static void BuildInternal()
        {
            _root = World.Root("CME_Yard");
            var t = _root.transform;

            // --- structure (my models, authored in world coordinates)
            World.Mesh("yard_deck", "tex_yard_deck", t, Vector3.zero, 0);
            World.Mesh("yard_pergola", "tex_yard_pergola", t, Vector3.zero, 0);
            World.Mesh("yard_planters", "tex_yard_planters", t, Vector3.zero, 0);
            World.Mesh("yard_strawberries", "tex_yard_strawberries", t, Vector3.zero, 0);
            World.Mesh("yard_banana", "tex_yard_banana", t, Vector3.zero, 0);
            World.Mesh("yard_poles", "tex_yard_poles", t, Vector3.zero, 0);
            World.Mesh("yard_facade", "tex_yard_facade", t, Vector3.zero, 0);

            // (the light blockers and the roof that replaces them outside are handled by Outdoors)

            // (the sea under the pier is part of Outdoors' ocean)

            // --- colliders: deck floor, tall invisible railings so parcels stay aboard
            int layer = 0;
            World.Box(t, "deck_floor", new Vector3((X0 + X1) / 2, -0.05f, (Z0 + Z1) / 2), new Vector3(X1 - X0, 0.16f, Z1 - Z0), layer);
            World.Box(t, "rail_west", new Vector3(X0 + 0.06f, 1.7f, (Z0 + Z1) / 2), new Vector3(0.16f, 3.5f, Z1 - Z0), layer);
            World.Box(t, "rail_south", new Vector3((X0 + X1) / 2, 1.7f, Z0 + 0.06f), new Vector3(X1 - X0, 3.5f, 0.16f), layer);
            World.Box(t, "rail_east", new Vector3(X1 - 0.06f, 1.7f, (Z0 - 7.2f) / 2), new Vector3(0.16f, 3.5f, Math.Abs(-7.2f - Z0)), layer);
            foreach (var px in new[] { -12.9f, -9.05f, -5.2f })
                World.Box(t, "pergola_post", new Vector3(px, 1.35f, 0.15f), new Vector3(0.2f, 2.7f, 0.2f), layer);
            World.Box(t, "planter_a", new Vector3(-11.4f, 0.27f, Z0 + 0.55f), new Vector3(2.2f, 0.5f, 0.6f), layer);
            World.Box(t, "planter_b", new Vector3(-8.2f, 0.27f, Z0 + 0.55f), new Vector3(2.2f, 0.5f, 0.6f), layer);
            World.Box(t, "banana_pot", new Vector3(X0 + 0.7f, 0.33f, -3.4f), new Vector3(0.85f, 0.62f, 0.85f), layer);
            World.Box(t, "banana_stem", new Vector3(X0 + 0.7f, 1.0f, -3.4f), new Vector3(0.2f, 1.0f, 0.2f), layer);
            foreach (var px in PoleX)
                World.Box(t, "festoon_pole", new Vector3(px, 1.68f, PoleZ), new Vector3(0.16f, 3.35f, 0.16f), layer);
            World.Box(t, "lamp", new Vector3(X0 + 0.45f, 1.2f, Z0 + 1.6f), new Vector3(0.3f, 2.4f, 0.3f), layer);

            // --- sign facing the door
            var signPos = new Vector3(X0 + 0.4f, 0.03f, -1.2f);
            World.Mesh("sign_frame", "tex_sign_frame", t, signPos, 180);
            World.Mesh("sign_face", "sign_yard", t, signPos, 180);
            World.Box(t, "sign", signPos + new Vector3(0, 1.0f, 0), new Vector3(0.2f, 2.0f, 1.7f), layer);

            // --- shelves under the pergola: native dock shelf visuals + cloned storage grids
            var shelfTemplate = World.Find("p_Shelves_M (3)", "p_Dock") ?? World.Find("p_Shelves_M");
            int id = 0;
            // bookcases packed together (ids 917000.. stay in this order so stored parcels follow their shelf)
            foreach (var sx in new[] { -10.63f, -8.58f, -6.53f })
                BuildShelf(shelfTemplate, t, new Vector3(sx, 0.04f, 2.12f), 0f, ref id);
            // the repair room's open rack, west of the bookcases with its back against the dock wall
            var rackTemplate = World.Find("p_Shelves_L (1)", "p_AdditionalStorageRoom") ?? World.Find("p_Shelves_L");
            // turned so its storage tag sits on a front post facing the yard (rotated, it is 1.1 m deep)
            const float rackYaw = 90f;
            var rackPos = new Vector3(-12.4f, 0.04f, 1.87f);   // west end of the row
            BuildShelf(rackTemplate, t, rackPos, rackYaw, ref id);
            // the tag, offset and turned with the rack exactly as in the repair room (rack yaw 180, tag yaw 0 there)
            var tag = World.Find("prop_StorageTag_Right (2)");
            var tagCopy = tag ? World.CopyVisual(tag, t, Vector3.zero, 0f, 1f, false) : null;
            if (tagCopy)
                tagCopy.transform.SetPositionAndRotation(
                    rackPos + Quaternion.Euler(0, rackYaw, 0) * new Vector3(0.5f, 2.29f, 0.55f),
                    Quaternion.Euler(0, rackYaw - 180f, 0));

            // --- decoration borrowed from the dock (visual copies only)
            var crate = World.Find("prop_Dock_Crate_Small");
            World.CopyVisual(crate, t, new Vector3(-3.25f, 0.03f, -6.5f), 15);
            World.CopyVisual(crate, t, new Vector3(-3.3f, 0.03f, -5.5f), -10);
            var buoy = World.Find("prop_Dock_Buoy");
            // hung on the lavender facade wall above the crates, facing the yard
            var ring = World.CopyVisual(buoy, t, RingPos, RingYaw, 1f, false);
            if (ring)
            {
                ring.name = "CME_LifeRing";
                ring.transform.SetPositionAndRotation(RingPos, Quaternion.Euler(0f, RingYaw, 0f));
            }
            // the big rock west of the deck reaches 1.3 m onto it; slide it out into the water
            var rock = World.Find("prop_Rock_04 (1)", "p_Outside");
            if (rock) rock.transform.position += new Vector3(-1.6f, 0f, 0f);
            var rope = World.Find("prop_Dock_Rope");
            World.CopyVisual(rope, t, new Vector3(-4.1f, 0.03f, -8.1f), 40);
            // potted palm on the east side (clear of the crates and the door swing), watering can between the planters
            var palm = World.Mesh("syn_palm", "tex_synty_gang", t, new Vector3(-3.3f, 0.03f, -3.2f), 20);
            palm.transform.localScale = Vector3.one * 1.25f;
            Placement.SettleWithBounds(palm);
            World.Box(t, "palm_pot", palm.transform.position + new Vector3(0, 0.33f, 0), new Vector3(0.6f, 0.66f, 0.6f), layer);
            var can = World.Mesh("syn_wateringcan", "tex_synty_gang", t, new Vector3(-9.8f, 0.03f, -8.3f), 70);
            Placement.SettleWithBounds(can);
            World.Box(t, "watering_can", can.transform.position + new Vector3(0, 0.2f, 0), new Vector3(0.6f, 0.4f, 0.6f), layer);

            var plant = World.Find("p_Plant");
            World.CopyVisual(plant, t, new Vector3(-12.95f, 0.03f, -8.45f), 0);
            World.CopyVisual(plant, t, new Vector3(-3.0f, 0.03f, -8.45f), 90);

            // --- lights: string lights under the pergola and the lamp post
            // festoon lights: the Bright Room's garland, strung from the pergola beam to three poles mid-deck.
            // Its low hanging points (local z +2.7) meet the beam, the high ones (local z -1.4) the poles.
            var garland = World.Find("prop_BrightRoom_LightsGarland");
            var g = World.CopyVisual(garland, t, GarlandPos, 0, 1f, false);
            if (g) g.name = "CME_FestoonLights";
            _lights.Add(World.PointLight(t, new Vector3(-10.4f, 2.4f, -2.0f), new Color(1f, 0.82f, 0.58f), 1.6f, 6.5f));
            _lights.Add(World.PointLight(t, new Vector3(-7.2f, 2.4f, -2.0f), new Color(1f, 0.82f, 0.58f), 1.6f, 6.5f));
            _lights.Add(World.PointLight(t, new Vector3(X0 + 0.87f, 2.05f, Z0 + 1.6f), new Color(1f, 0.8f, 0.55f), 2.0f, 7f));

            BuildDoor(t);
            Plugin.Log.Msg("Yard built");
        }

        static void BuildShelf(GameObject template, Transform parent, Vector3 pos, float yaw, ref int id)
        {
            if (!template) return;
            var unit = new GameObject("CME_YardShelf");
            unit.transform.SetParent(parent, false);
            unit.transform.position = pos;
            unit.transform.rotation = Quaternion.Euler(0, yaw, 0);
            // visuals (mesh + fake shadow) and the unit's plain collider boxes
            foreach (var mr in template.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.transform.parent != template.transform) continue; // only the unit's own meshes, not stored parcels
                if (mr.name.Contains("FakeShadow")) continue;           // indoor-only blob shadow; glows in sunlight
                var mf = mr.GetComponent<MeshFilter>();
                if (!mf) continue;
                var v = new GameObject(mr.name);
                v.transform.SetParent(unit.transform, false);
                v.transform.localPosition = mr.transform.localPosition;
                v.transform.localRotation = mr.transform.localRotation;
                v.transform.localScale = mr.transform.localScale;
                v.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                v.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
            }
            var cols = template.transform.Find("Colliders");
            if (cols)
            {
                var c = UnityEngine.Object.Instantiate(cols.gameObject, unit.transform);
                c.name = "Colliders";
                c.transform.localPosition = cols.localPosition;
                c.transform.localRotation = cols.localRotation;
            }
            for (int i = 0; i < template.transform.childCount; i++)
            {
                var child = template.transform.GetChild(i);
                if (!child.GetComponent<EntityInteractableStore>()) continue;
                var name = child.name;
                int paren = name.IndexOf(" (", StringComparison.Ordinal);
                if (paren > 0) name = name.Substring(0, paren);
                var worldPos = unit.transform.TransformPoint(child.localPosition);
                StoreFactory.Create(name, unit.transform, worldPos, yaw + child.localEulerAngles.y, id++);
            }
        }

        // ------------------------------------------------------------------ door

        static void BuildDoor(Transform t)
        {
            var original = World.Find("prop_StorageCorridor_ServiceDoor");
            if (original)
            {
                foreach (var r in original.GetComponentsInChildren<MeshRenderer>(true)) r.enabled = false;
            }
            World.Mesh("door_frame", "tex_door_frame", t, Vector3.zero, 0);
            var pivot = new GameObject("CME_DoorPivot");
            pivot.transform.SetParent(t, false);
            pivot.transform.position = new Vector3(DoorX, 0f, DoorHingeZ);
            World.Mesh("door_leaf", "tex_door_leaf", pivot.transform, pivot.transform.position, 0);
            var leafCol = World.Box(pivot.transform, "leaf_collider", pivot.transform.position + new Vector3(0, 1.37f, 0.83f), new Vector3(0.08f, 2.7f, 1.66f), 0);
            leafCol.transform.SetParent(pivot.transform, true);
            _doorPivot = pivot.transform;

            // The corridor's west wall is one big collider; prepare a version with a doorway cut out.
            var wall = World.Find("Collider_01", "p_StorageCorridor/Colliders");
            _originalWallCollider = wall ? wall.GetComponent<Collider>() : null;
            if (_originalWallCollider)
            {
                var b = _originalWallCollider.bounds;
                float zA = 0.98f, zB = 2.72f, yTop = 2.76f;
                var segs = new List<(Vector3 c, Vector3 s)>
                {
                    (new Vector3(b.center.x, b.center.y, (b.min.z + zA) / 2), new Vector3(b.size.x, b.size.y, zA - b.min.z)),
                    (new Vector3(b.center.x, b.center.y, (zB + b.max.z) / 2), new Vector3(b.size.x, b.size.y, b.max.z - zB)),
                    (new Vector3(b.center.x, (yTop + b.max.y) / 2, (zA + zB) / 2), new Vector3(b.size.x, b.max.y - yTop, zB - zA)),
                };
                foreach (var (c, s) in segs)
                {
                    if (s.z <= 0.01f || s.y <= 0.01f) continue;
                    var col = World.Box(t, "doorway_wall", c, s, _originalWallCollider.gameObject.layer);
                    col.enabled = false;
                    _doorwayColliders.Add(col);
                }
            }
        }

        /// <summary>Opens the yard (instantly when loading a save that already unlocked it).</summary>
        public static void SetOpen(bool open, bool animate)
        {
            if (!_root) return;
            if (_open == open && !animate) { ApplyColliders(); return; }
            _open = open;
            _doorTarget = open ? -100f : 0f;
            if (!animate) _doorAngle = _doorTarget;
            ApplyColliders();
            if (animate && open) SoundFx.Play(new Vector3(DoorX, 1.2f, 1.8f), 1f, "Unlockable_Door_Activation", "Boat_Door");
        }

        static void ApplyColliders()
        {
            if (_originalWallCollider) _originalWallCollider.enabled = !_open;
            foreach (var c in _doorwayColliders) if (c) c.enabled = _open;
        }

        public static void Update()
        {
            if (!_root) return;
            if (_doorPivot && Math.Abs(_doorAngle - _doorTarget) > 0.01f)
            {
                _doorAngle = Mathf.MoveTowards(_doorAngle, _doorTarget, Time.deltaTime * 85f);
                float eased = _doorAngle;
                _doorPivot.localRotation = Quaternion.Euler(0, eased, 0);
            }
            else if (_doorPivot)
            {
                _doorPivot.localRotation = Quaternion.Euler(0, _doorAngle, 0);
            }
            // lights glow in the evening / at night only
            float night = Lighting.NightFactor();
            foreach (var l in _lights) if (l) l.intensity = Mathf.Lerp(0.004f, 0.035f, night);
        }
    }
}
