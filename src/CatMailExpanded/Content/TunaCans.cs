using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Turns the break room's empty tuna cans into carryable objects (spawned through the game's network
    /// spawner from the carryable stool, re-skinned as a can) that can be thrown in the trash.
    /// Disposed cans are remembered in the save; cans that were moved but not trashed return to their shelf.
    /// </summary>
    internal static class TunaCans
    {
        const string SaveKey = "CME_DisposedCans";
        const string SpawnKey = "p_Entity_Carryable_Stool";
        const string ArgPrefix = "CME_Can:";

        sealed class Can
        {
            public int Index;
            public GameObject Static;
            public Vector3 Pos;
            public Quaternion Rot;
            public bool Disposed;
            public Entity Entity;
            public bool Requested;
        }

        static readonly List<Can> _cans = new();
        static Vector3 _trashPos;
        static bool _active;
        static bool _subscribed;
        static float _nextCheck;

        public static void SetTrashPos(Vector3 p) => _trashPos = p;

        public static int Remaining => _cans.Count(c => !c.Disposed);

        static bool IsServer => NetworkManager.Instance == null || NetworkManager.Instance.IsServer;

        public static void Reset()
        {
            _cans.Clear();
            _tossing.Clear();
            _active = false;
            _subscribed = false;
        }

        public static void Begin(Vector3 trashPos)
        {
            _trashPos = trashPos;
            _cans.Clear();
            var statics = new List<GameObject>();
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go || !go.scene.IsValid() || !go.name.StartsWith("prop_TunaCan")) continue;
                if (!World.PathOf(go.transform).Contains("p_BreakRoom")) continue;
                statics.Add(go);
            }
            // stable order across sessions: by hierarchy path
            statics.Sort((a, b) => string.CompareOrdinal(World.PathOf(a.transform), World.PathOf(b.transform)));
            var saved = SaveManager.Instance?.GetString(TableType.GameProgression, SaveKey, "", null) ?? "";
            for (int i = 0; i < statics.Count; i++)
            {
                var s = statics[i];
                _cans.Add(new Can
                {
                    Index = i, Static = s, Pos = s.transform.position, Rot = s.transform.rotation,
                    Disposed = i < saved.Length && saved[i] == '1',
                });
            }
            ReplaceShelfColliders();
            Subscribe();
            foreach (var c in _cans)
            {
                SetStaticVisible(c, false);
                if (!c.Disposed && IsServer) RequestSpawn(c);
            }
            _active = true;
            Plugin.Log.Msg($"Tuna cans: {_cans.Count} total, {Remaining} remaining");
        }

        static void SetStaticVisible(Can c, bool on)
        {
            if (!c.Static) return;
            foreach (var r in c.Static.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            foreach (var col in c.Static.GetComponentsInChildren<Collider>(true)) col.enabled = on;
        }

        static void Subscribe()
        {
            if (_subscribed || NetworkSpawnerManager.Instance == null) return;
            NetworkSpawnerManager.EntitySpawnedHandler handler = new Action<ulong, uint, Entity, string>(OnEntitySpawned);
            NetworkSpawnerManager.Instance.add_EntitySpawned(handler);
            _subscribed = true;
        }

        static void RequestSpawn(Can c)
        {
            if (c.Requested) return;
            c.Requested = true;
            try
            {
                NetworkSpawnerManager.Instance.HandleSpawnRequestFromNetwork(0, (uint)(4100000 + c.Index), SpawnKey,
                    c.Pos + Vector3.up * 0.01f, c.Rot, Vector3.zero, ArgPrefix + c.Index, uint.MaxValue);
            }
            catch (Exception e) { Plugin.Log.Warning("Can spawn failed: " + e.Message); }
        }

        static void OnEntitySpawned(ulong client, uint request, Entity entity, string args)
        {
            if (entity == null || string.IsNullOrEmpty(args) || !args.StartsWith(ArgPrefix)) return;
            if (!int.TryParse(args.Substring(ArgPrefix.Length), out int idx)) return;
            var can = _cans.FirstOrDefault(c => c.Index == idx);
            try { Reskin(entity, can); }
            catch (Exception e) { Plugin.Log.Warning("Can reskin failed: " + e.Message); }
            if (can != null) can.Entity = entity;
        }

        /// <summary>Hides the stool and dresses the entity as the original tuna can.</summary>
        static void Reskin(Entity e, Can can)
        {
            e.SetIsPreventedFromSaving(true);
            e.name = "CME_TunaCan";
            var visuals = e.transform.Find("Visuals");
            var stool = visuals ? visuals.Find("prop_Stool") : null;
            BoxCollider keep = null;
            if (stool)
            {
                foreach (var r in stool.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                foreach (var bc in stool.GetComponentsInChildren<BoxCollider>(true))
                {
                    if (keep == null && bc.gameObject.name == "Collider_TopPlateforme") { keep = bc; continue; }
                    bc.gameObject.SetActive(false);
                }
            }
            if (can?.Static)
            {
                var mr = can.Static.GetComponentInChildren<MeshRenderer>(true);
                var mf = mr ? mr.GetComponent<MeshFilter>() : null;
                if (mf && visuals)
                {
                    var v = new GameObject("CME_CanVisual");
                    v.transform.SetParent(visuals, false);
                    v.transform.localPosition = mr.transform.localPosition;
                    v.transform.localRotation = mr.transform.localRotation;
                    v.transform.localScale = mr.transform.localScale;
                    v.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    v.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                }
                if (keep && mf && mf.sharedMesh)
                {
                    // The game targets with a very thin sphere cast, so the hitbox must cover the whole visible can
                    // (the static prop's own collider was a thin, offset slab). Built from the mesh bounds, padded.
                    var b = mf.sharedMesh.bounds;
                    var t = mr.transform;
                    keep.transform.localPosition = Vector3.zero;
                    keep.transform.localRotation = Quaternion.identity;
                    keep.transform.localScale = Vector3.one;
                    var worldCentre = t.TransformPoint(b.center);
                    var ext = Vector3.Scale(b.extents, t.lossyScale);
                    // the can may lie on its side: take the axis-aligned extents of the rotated box
                    var r = t.rotation;
                    var ax = new Vector3(
                        Mathf.Abs((r * Vector3.right).x) * ext.x + Mathf.Abs((r * Vector3.up).x) * ext.y + Mathf.Abs((r * Vector3.forward).x) * ext.z,
                        Mathf.Abs((r * Vector3.right).y) * ext.x + Mathf.Abs((r * Vector3.up).y) * ext.y + Mathf.Abs((r * Vector3.forward).y) * ext.z,
                        Mathf.Abs((r * Vector3.right).z) * ext.x + Mathf.Abs((r * Vector3.up).z) * ext.y + Mathf.Abs((r * Vector3.forward).z) * ext.z);
                    var size = ax * 2f * 1.15f;
                    size = new Vector3(Mathf.Max(size.x, 0.16f), Mathf.Max(size.y, 0.14f), Mathf.Max(size.z, 0.16f));
                    keep.center = e.transform.InverseTransformPoint(worldCentre);
                    keep.size = size;
                }
            }
            var rb = e.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.mass = 0.3f;
                // rest still on the shelf until picked up (no jostling between neighbouring cans);
                // the game makes it dynamic again when it is carried and dropped
                rb.isKinematic = true;
            }
        }

        // ------------------------------------------------------------------ throwing away a held can

        static readonly List<(Can can, Vector3 from, float t)> _tossing = new();

        static Can CanFor(Entity e)
        {
            if (e == null) return null;
            foreach (var c in _cans) if (c.Entity != null && c.Entity.Pointer == e.Pointer) return c;
            return null;
        }

        /// <summary>
        /// Called when the player presses Interact or Drop. If they are holding a tuna can and looking at the bin
        /// (or standing right next to it), the can is tossed into the bin. Returns true when handled.
        /// </summary>
        public static bool TryTrashHeld(PlayerInventory inv)
        {
            if (!_active || inv == null) return false;
            Entity held;
            try { held = inv.PickedUpEntity; } catch { return false; }
            var can = CanFor(held);
            if (can == null || can.Disposed) return false;
            var cam = Core.Cameras.View;
            if (!cam) return false;
            var binCentre = _trashPos + Vector3.up * 0.6f;
            var toBin = binCentre - cam.transform.position;
            float dist = toBin.magnitude;
            float angle = Vector3.Angle(cam.transform.forward, toBin);
            var flat = new Vector2(toBin.x, toBin.z).magnitude;
            bool looking = dist < 2.6f && angle < 28f;
            bool besideIt = flat < 1.1f && angle < 60f;
            if (!looking && !besideIt) return false;
            try { inv.InstantlyDrop(held); } catch (Exception e) { Plugin.Log.Warning("Release can failed: " + e.Message); return false; }
            var rb = held.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
            foreach (var col in held.GetComponentsInChildren<Collider>()) col.enabled = false;
            _tossing.Add((can, held.transform.position, 0f));
            return true;
        }

        /// <summary>Animates tossed cans in a little arc into the bin, then disposes them.</summary>
        static bool UpdateTosses()
        {
            bool changed = false;
            for (int i = _tossing.Count - 1; i >= 0; i--)
            {
                var (can, from, t) = _tossing[i];
                t += Time.deltaTime / 0.35f;
                var e = can.Entity;
                if (e == null || !e) { _tossing.RemoveAt(i); continue; }
                var to = _trashPos + Vector3.up * 0.35f;
                var p = Vector3.Lerp(from, to, t);
                p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.35f;
                e.transform.position = p;
                e.transform.Rotate(0, 0, 540f * Time.deltaTime, Space.Self);
                if (t >= 1f)
                {
                    _tossing.RemoveAt(i);
                    Dispose(can);
                    changed = true;
                }
                else _tossing[i] = (can, from, t);
            }
            return changed;
        }

        /// <summary>Dev helper: throws away every remaining can.</summary>
        public static int DisposeAll()
        {
            int n = 0;
            foreach (var c in _cans)
            {
                if (c.Disposed || c.Entity == null) continue;
                Dispose(c);
                n++;
            }
            return n;
        }

        public static bool AnyCanIn(Box area)
        {
            foreach (var c in _cans) if (!c.Disposed && area.Contains(c.Pos)) return true;
            return false;
        }

        static bool IsCarried(Entity e)
        {
            try
            {
                var p = e.Interactable?.Pickable;
                return p != null && (p.CarryingEntity != null || p.IsInInventory);
            }
            catch { return false; }
        }

        /// <summary>Returns true when a can was disposed this frame.</summary>
        public static bool Update()
        {
            if (!_active) return false;
            bool changed = UpdateTosses();
            foreach (var c in _cans)
            {
                if (c.Disposed || c.Entity == null) continue;
                if (_tossing.Exists(x => x.can == c)) continue;
                bool alive;
                try { alive = c.Entity && !c.Entity.IsDisposed; } catch { alive = false; }
                if (!alive) { c.Entity = null; continue; }
                var p = c.Entity.transform.position;
                var d = new Vector2(p.x - _trashPos.x, p.z - _trashPos.z);
                // a funnel in and above the open bin: dropped or thrown cans count as soon as they enter it
                float funnel = Mathf.Lerp(0.42f, 0.62f, Mathf.Clamp01((p.y - _trashPos.y - 0.4f) / 1.2f));
                if (d.magnitude < funnel && p.y < _trashPos.y + 1.6f && !IsCarried(c.Entity))
                {
                    Dispose(c);
                    changed = true;
                }
            }
            return changed;
        }

        static void Dispose(Can c)
        {
            var before = new HashSet<int>(_cans.Where(x => !x.Disposed).Select(x => x.Index));
            c.Disposed = true;
            try { c.Entity.Dispose(false); } catch (Exception e) { Plugin.Log.Warning("Dispose can failed: " + e.Message); }
            c.Entity = null;
            SoundFx.Play(_trashPos + Vector3.up * 0.6f, 1f, "Key_Scanner_Hit", "Parcel_Place_Small");
            Save();
            if (Remaining == 0) SpeakerRoom.OnShelfCleared();
        }

        static void Save()
        {
            if (!IsServer || SaveManager.Instance == null) return;
            var sb = new StringBuilder();
            foreach (var c in _cans) sb.Append(c.Disposed ? '1' : '0');
            SaveManager.Instance.SetString(TableType.GameProgression, SaveKey, sb.ToString());
        }

        // ------------------------------------------------------------------ shelf colliders

        /// <summary>
        /// The bookshelf and pantry use one solid box collider each, which would trap spawned cans and hide the
        /// storage grids from interaction. Replace them with boards, sides, backs and dividers.
        /// </summary>
        static void ReplaceShelfColliders()
        {
            var root = GameObject.Find("CME_ShelfColliders");
            if (root) return;
            root = new GameObject("CME_ShelfColliders");
            var t = root.transform;
            var book = World.Find("prop_Bookshelf", "p_BreakRoom");
            var pantry = World.Find("prop_Pantry", "p_BreakRoom");
            DisableBigCollider(book);
            DisableBigCollider(pantry);

            // bookshelf: x 8.08..11.82, z 18.11..18.75 (front +z)
            void B(float x0, float x1, float y0, float y1, float z0, float z1) =>
                World.Box(t, "book", new Vector3((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, y1 - y0, z1 - z0), 0);
            B(8.08f, 11.82f, 0.05f, 4.10f, 18.11f, 18.20f);           // back
            B(8.08f, 8.16f, 0.05f, 4.10f, 18.11f, 18.75f);            // sides
            B(11.74f, 11.82f, 0.05f, 4.10f, 18.11f, 18.75f);
            B(8.08f, 11.82f, 0.05f, 0.91f, 18.11f, 18.75f);           // lower cabinet
            foreach (var y in new[] { 1.66f, 2.47f, 3.18f, 3.86f }) B(8.08f, 11.82f, y - 0.05f, y, 18.11f, 18.75f);
            B(10.67f, 10.73f, 0.91f, 1.61f, 18.11f, 18.75f);          // dividers
            B(9.27f, 9.33f, 1.66f, 2.42f, 18.11f, 18.75f);
            B(10.67f, 10.73f, 2.47f, 3.13f, 18.11f, 18.75f);
            B(9.27f, 9.33f, 3.18f, 3.81f, 18.11f, 18.75f);
            // pantry: x 11.16..12.08 (front -x), z 23.77..25.54
            void P(float x0, float x1, float y0, float y1, float z0, float z1) =>
                World.Box(t, "pantry", new Vector3((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), new Vector3(x1 - x0, y1 - y0, z1 - z0), 0);
            P(11.98f, 12.08f, 0.07f, 4.0f, 23.77f, 25.54f);
            P(11.16f, 12.08f, 0.07f, 4.0f, 23.77f, 23.85f);
            P(11.16f, 12.08f, 0.07f, 4.0f, 25.47f, 25.54f);
            foreach (var y in new[] { 0.22f, 0.94f, 1.72f, 2.48f, 3.26f, 3.88f }) P(11.16f, 12.08f, y - 0.06f, y, 23.77f, 25.54f);
        }

        static void DisableBigCollider(GameObject shelf)
        {
            if (!shelf) return;
            foreach (var bc in shelf.GetComponentsInChildren<BoxCollider>(true))
            {
                // the solid box is the one directly on the shelf's own "Collider" child, not the cans' colliders
                if (bc.transform.parent == shelf.transform) bc.enabled = false;
            }
        }
    }
}
