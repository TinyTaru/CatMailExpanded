using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Drives the new parcel types at runtime:
    ///  - sticker decal identifying the type (in the slot a native constraint stamp would use)
    ///  - Scented: coloured fog cloud that builds up while the parcel is not stored outdoors
    ///  - Restless: wiggles and hops off shelves (knocking neighbours loose) unless it is in the
    ///    speaker room while music is playing
    /// </summary>
    internal static class ParcelTracker
    {
        sealed class Tracked
        {
            public EntityParcel Parcel;
            public ModKind Kind;
            public GameObject Sticker;
            public FogCloud Fog;
            public float FogIntensity;
            public float NextHop;
            public float WiggleUntil;
            public bool PendingImpulse;
            public Transform Visuals;
            public Quaternion VisualsRest;
            // stickers live under Tech/StampParent, not under Visuals: they get the same wiggle
            public Transform Stamps;
            public Vector3 StampsRestPos;
            public Quaternion StampsRestRot;
            public bool StampsMoved;
        }

        static readonly Dictionary<IntPtr, Tracked> _tracked = new();
        static readonly List<IntPtr> _dead = new();
        static float _nextLogic;
        static Material _stickerTemplate;
        static Mesh _stickerQuad;
        static readonly Dictionary<ModKind, Material> _stickerMats = new();

        const int MaxFogEmitters = 14;

        public static int Count => _tracked.Count;

        public static string Describe()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in _tracked.Values)
            {
                if (t.Kind == ModKind.None) continue;
                var p = t.Parcel;
                sb.AppendLine($"{t.Kind} {p.name} pos={p.transform.position} sticker={(t.Sticker ? t.Sticker.transform.position.ToString() : "none")} " +
                              $"fog={(t.Fog != null ? t.Fog.Count : -1)} int={t.FogIntensity:0.00} pile={IsLinkedToPile(p)} stored={p.Interactable?.Pickable?.IsStored} nextHop={t.NextHop - Time.time:0.0}");
            }
            return sb.ToString();
        }

        public static void Register(EntityParcel parcel)
        {
            if (parcel == null) return;
            var ptr = parcel.Pointer;
            if (_tracked.ContainsKey(ptr)) return;
            _tracked[ptr] = new Tracked { Parcel = parcel, NextHop = Time.time + Random.Range(4f, 10f) };
        }

        public static void Clear()
        {
            foreach (var t in _tracked.Values)
            {
                t.Fog?.Destroy();
            }
            _tracked.Clear();
            FogCloud.ResetShared();
            _stickerTemplate = null;
            _stickerQuad = null;
            _stickerMats.Clear();
        }

        static bool IsServer => NetworkManager.Instance == null || NetworkManager.Instance.IsServer;

        public static void Update()
        {
            if (_tracked.Count == 0) return;
            float now = Time.time;
            bool logicTick = now >= _nextLogic;
            if (logicTick) _nextLogic = now + 0.25f;

            var camera = Core.Cameras.View;
            Vector3 cam = camera ? camera.transform.position : Vector3.zero;
            float dt = Time.deltaTime;
            _dead.Clear();
            List<(float, Tracked)> fogCandidates = logicTick ? new List<(float, Tracked)>() : null;

            foreach (var kv in _tracked)
            {
                var t = kv.Value;
                EntityParcel p = t.Parcel;
                bool alive;
                try { alive = p != null && p && !p.IsDisposed; } catch { alive = false; }
                if (!alive) { _dead.Add(kv.Key); t.Fog?.Destroy(); continue; }

                if (logicTick)
                {
                    var props = p.Properties;
                    var kind = props != null ? ParcelKinds.KindOf(props.StorageConstraint) : ModKind.None;
                    if (kind != t.Kind) SetKind(t, kind);
                    if (t.Kind != ModKind.None && ParcelKinds.IsScented(t.Kind))
                        fogCandidates.Add(((p.transform.position - cam).sqrMagnitude, t));
                    if (t.Kind == ModKind.Restless) RestlessLogic(t, now);
                }
                if (t.Kind == ModKind.Restless) AnimateWiggle(t, now);
                if (t.Fog != null)
                {
                    t.Fog.Origin = p.transform.position + Vector3.up * 0.15f;
                    t.Fog.Update(dt, camera);
                }
            }
            foreach (var d in _dead) _tracked.Remove(d);

            if (logicTick && fogCandidates != null) UpdateFog(fogCandidates);
        }

        // ------------------------------------------------------------------ state helpers

        static bool IsLinkedToPile(EntityParcel p)
        {
            try { return p.LinkedPileType != PileType.None; } catch { return false; }
        }

        static EntityInteractableStore RootStore(EntityParcel p)
        {
            try
            {
                var store = p.Interactable?.Store;
                if (store == null) return null;
                var parent = store.ParentStore;
                return parent == null ? null : parent.GetRootStore(true);
            }
            catch { return null; }
        }

        static bool IsCarried(EntityParcel p)
        {
            try
            {
                var pick = p.Interactable?.Pickable;
                return pick != null && (pick.CarryingEntity != null || pick.IsInInventory);
            }
            catch { return false; }
        }

        /// <summary>Scented parcels are calm outdoors (yard) or when loaded on the boat.</summary>
        static bool ScentContained(EntityParcel p)
        {
            if (IsLinkedToPile(p)) return true;
            if (Zones.InYard(p.transform.position)) return true;
            var root = RootStore(p);
            if (root != null && root.StorageType == StorageType.Boat) return true;
            return false;
        }

        /// <summary>Restless parcels are calm in the speaker room while the music plays.</summary>
        static bool RestlessCalm(EntityParcel p)
        {
            if (IsLinkedToPile(p)) return true;
            if (SpeakerRoom.MusicAudibleAt(p.transform.position)) return true;
            var root = RootStore(p);
            if (root != null && (root.StorageType != StorageType.Other || root.AllSafe)) return true; // boat, customer counter, workstations
            return false;
        }

        // ------------------------------------------------------------------ kind / sticker

        static void SetKind(Tracked t, ModKind kind)
        {
            RestoreStamps(t);
            t.Kind = kind;
            if (t.Sticker) UnityEngine.Object.Destroy(t.Sticker);
            t.Sticker = null;
            t.Fog?.Destroy();
            t.Fog = null;
            t.FogIntensity = 0;
            t.Visuals = t.Parcel.transform.Find("Visuals");
            if (t.Visuals) t.VisualsRest = t.Visuals.localRotation;
            t.Stamps = t.Parcel.transform.Find("Tech/StampParent");
            t.StampsMoved = false;
            // the sticker is now a real game stamp (ModStamps): the player applies it to customer parcels
        }

        static void EnsureStickerResources()
        {
            if (_stickerTemplate && _stickerQuad) return;
            foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (m && m.name == "m_Stamps&Stickers_Constraints") { _stickerTemplate = m; break; }
            }
            _stickerQuad = BuildStickerQuad(0.29f);
        }

        /// <summary>
        /// Native stamps are 0.25m quads in their local XY plane, facing local +Z (away from the box).
        /// Front face reads correctly from outside; a back face is added so it never vanishes.
        /// </summary>
        static Mesh BuildStickerQuad(float size)
        {
            float h = size * 0.5f;
            const float zf = 0.004f, zb = 0.003f;
            var verts = new[]
            {
                // front (seen from +Z: screen right = -X)
                new Vector3(h, h, zf), new Vector3(-h, h, zf), new Vector3(-h, -h, zf), new Vector3(h, -h, zf),
                // back (seen from -Z: screen right = +X)
                new Vector3(-h, h, zb), new Vector3(h, h, zb), new Vector3(h, -h, zb), new Vector3(-h, -h, zb),
            };
            var uvs = new[]
            {
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0),
            };
            var normals = new[]
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            };
            var m = new Mesh { name = "CME_StickerQuad" };
            m.vertices = verts;
            m.uv = uvs;
            m.uv2 = uvs;
            m.normals = normals;
            m.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            m.RecalculateBounds();
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return m;
        }

        public static Mesh StickerQuad() { EnsureStickerResources(); return _stickerQuad; }
        public static Mesh StickerQuad(float size)
        {
            if (_quads.TryGetValue(size, out var q) && q) return q;
            return _quads[size] = BuildStickerQuad(size);
        }
        static readonly Dictionary<float, Mesh> _quads = new();
        public static Material StickerMaterialFor(ModKind kind) { EnsureStickerResources(); return StickerMaterial(kind); }

        static Material StickerMaterial(ModKind kind)
        {
            if (_stickerMats.TryGetValue(kind, out var m) && m) return m;
            m = _stickerTemplate ? new Material(_stickerTemplate) : new Material(ModAssets.PropTemplate());
            var tex = ModAssets.Texture(ParcelKinds.StickerTexture(kind));
            m.SetTexture("_BaseMap", tex);
            m.name = "CME_Sticker_" + kind;
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _stickerMats[kind] = m;
            return m;
        }

        static GameObject CreateSticker(EntityParcel parcel, ModKind kind)
        {
            EnsureStickerResources();
            var anchor = parcel.transform.Find("Tech/ConstraintStampPositions/1")
                         ?? parcel.transform.Find("Tech/ConstraintStampPositions/2")
                         ?? parcel.transform;
            var go = new GameObject("CME_Sticker_" + kind);
            go.transform.SetParent(anchor, false);
            go.transform.localPosition = Vector3.zero;
            // same random tilt range native stamps use
            float tilt = (parcel.Pointer.ToInt64() % 25) - 12;
            go.transform.localRotation = Quaternion.Euler(0, 0, tilt);
            go.transform.localScale = Vector3.one * 0.85f; // native stamps use 0.85
            go.layer = 10;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = _stickerQuad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = StickerMaterial(kind);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // ------------------------------------------------------------------ fog

        static void UpdateFog(List<(float dist, Tracked t)> candidates)
        {
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            float night = Lighting.NightFactor();
            for (int i = 0; i < candidates.Count; i++)
            {
                var t = candidates[i].t;
                bool exposed = !ScentContained(t.Parcel);
                // builds up over ~15s indoors, clears over ~4s once outside
                t.FogIntensity = Mathf.MoveTowards(t.FogIntensity, exposed ? 1f : 0f, exposed ? 0.017f : 0.07f);
                bool allowed = i < MaxFogEmitters;
                if (t.FogIntensity > 0.01f && allowed)
                {
                    t.Fog ??= new FogCloud("CME_ScentFog_" + t.Kind);
                    t.Fog.Rate = exposed ? Mathf.Lerp(0.8f, 9.5f, t.FogIntensity) : 0f;
                    t.Fog.Radius = Mathf.Lerp(0.3f, 0.75f, t.FogIntensity);
                    var c = ParcelKinds.FogColor(t.Kind);
                    var lit = Color.Lerp(c, new Color(c.r * 0.42f, c.g * 0.42f, c.b * 0.55f), night);
                    lit.a = 1f;
                    t.Fog.Tint = lit;
                }
                else if (t.Fog != null)
                {
                    t.Fog.Rate = 0f;
                    if (t.Fog.Count == 0)
                    {
                        t.Fog.Destroy();
                        t.Fog = null;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ restless

        static void RestlessLogic(Tracked t, float now)
        {
            var p = t.Parcel;
            if (t.PendingImpulse)
            {
                t.PendingImpulse = false;
                ApplyHop(p);
                return;
            }
            if (RestlessCalm(p))
            {
                t.NextHop = Mathf.Max(t.NextHop, now + Random.Range(3f, 6f));
                return;
            }
            if (IsCarried(p))
            {
                // squirms in the player's arms but cannot jump away
                if (now >= t.NextHop) { t.WiggleUntil = now + 0.7f; t.NextHop = now + Random.Range(2.5f, 5f); }
                return;
            }
            if (now < t.NextHop) return;
            if (t.WiggleUntil < now - 1f)
            {
                // telegraph the jump with a short wiggle
                t.WiggleUntil = now + 0.65f;
                t.NextHop = now + 0.65f;
                return;
            }
            t.NextHop = now + Random.Range(Plugin.RestlessMinInterval.Value, Plugin.RestlessMaxInterval.Value);
            if (!IsServer) return;
            Hop(t);
        }

        static void Hop(Tracked t)
        {
            var p = t.Parcel;
            try
            {
                var pick = p.Interactable?.Pickable;
                var store = p.Interactable?.Store?.ParentStore;
                if (pick != null && pick.IsStored && store != null)
                {
                    store.RemoveEntity(p);
                    t.PendingImpulse = true; // rigidbody is restored by the store; push next tick
                }
                else ApplyHop(p);
                KnockNeighbours(p);
            }
            catch (Exception e) { Plugin.Log.Warning("Restless hop failed: " + e.Message); }
        }

        static void ApplyHop(EntityParcel p)
        {
            var rb = p.GetComponent<Rigidbody>();
            if (!rb || rb.isKinematic) return;
            var dir = Random.insideUnitCircle.normalized * Random.Range(0.7f, 1.5f);
            float mass = Mathf.Max(rb.mass, 0.1f);
            rb.AddForce(new Vector3(dir.x, Random.Range(3.0f, 4.2f), dir.y), ForceMode.VelocityChange);
            rb.AddTorque(Random.onUnitSphere * Random.Range(2f, 5f), ForceMode.VelocityChange);
            SoundFx.PlayBoing(p.transform.position);
        }

        /// <summary>Restless parcels shove nearby shelved parcels out of place.</summary>
        static void KnockNeighbours(EntityParcel self)
        {
            var hits = Physics.OverlapSphere(self.transform.position, 0.85f);
            int knocked = 0;
            foreach (var c in hits)
            {
                if (knocked >= 2) break;
                var other = c.GetComponentInParent<EntityParcel>();
                if (other == null || other.Pointer == self.Pointer) continue;
                if (IsLinkedToPile(other) || IsCarried(other)) continue;
                var pick = other.Interactable?.Pickable;
                var store = other.Interactable?.Store?.ParentStore;
                if (pick == null || !pick.IsStored || store == null) continue;
                // do not touch parcels stacked on the restless parcel itself, they ride along
                if (store.LinkedEntity != null && store.LinkedEntity.Pointer == self.Pointer) continue;
                var root = store.GetRootStore(true);
                if (root != null && (root.AllSafe || root.StorageType != StorageType.Other)) continue;
                if (Random.value > 0.45f) continue;
                store.RemoveEntity(other);
                knocked++;
                var target = other;
                Scheduler.After(0.05f, () =>
                {
                    if (target == null || !target) return;
                    var rb = target.GetComponent<Rigidbody>();
                    if (!rb) return;
                    var d = (target.transform.position - self.transform.position);
                    d.y = 0;
                    d = d.sqrMagnitude < 0.001f ? Random.insideUnitSphere : d.normalized;
                    rb.AddForce(d * Random.Range(0.8f, 1.6f) + Vector3.up * Random.Range(0.8f, 1.5f), ForceMode.VelocityChange);
                    rb.AddTorque(Random.onUnitSphere * 2f, ForceMode.VelocityChange);
                });
            }
        }

        /// <summary>Dev check: starts a wiggle on every restless parcel and reports box vs sticker tilt shortly after.</summary>
        public static void DebugWiggle(Action<string> report)
        {
            float now = Time.time;
            foreach (var t in _tracked.Values)
                if (t.Kind == ModKind.Restless && t.Parcel) t.WiggleUntil = now + 0.7f;
            Scheduler.After(0.12f, () =>
            {
                var sb = new System.Text.StringBuilder();
                foreach (var t in _tracked.Values)
                {
                    if (t.Kind != ModKind.Restless || !t.Parcel || !t.Visuals) continue;
                    var root = t.Parcel.transform.rotation;
                    float box = Quaternion.Angle(root * t.VisualsRest, t.Visuals.rotation);
                    float stk = t.Stamps && t.Stamps.parent ? Quaternion.Angle(t.Stamps.parent.rotation * (t.StampsMoved ? t.StampsRestRot : t.Stamps.localRotation), t.Stamps.rotation) : -1;
                    sb.AppendLine($"{t.Parcel.name}: box tilt {box:0.0} deg, sticker tilt {stk:0.0} deg");
                }
                report(sb.ToString());
            });
        }

        /// <summary>Turns the sticker parent by the same wiggle, about the same pivot, as the box model.</summary>
        static void WiggleStamps(Tracked t, Quaternion wiggle)
        {
            if (!t.Stamps) return;
            if (!t.StampsMoved)
            {
                t.StampsRestPos = t.Stamps.localPosition;
                t.StampsRestRot = t.Stamps.localRotation;
                t.StampsMoved = true;
            }
            var root = t.Parcel.transform;
            var parent = t.Stamps.parent;
            // rest pose in world space, then the box model's rotation delta about its pivot
            var restPos = parent ? parent.TransformPoint(t.StampsRestPos) : t.StampsRestPos;
            var restRot = parent ? parent.rotation * t.StampsRestRot : t.StampsRestRot;
            var restVisualsWorld = root.rotation * t.VisualsRest;
            var delta = restVisualsWorld * wiggle * Quaternion.Inverse(restVisualsWorld);
            var pivot = t.Visuals.position;
            t.Stamps.SetPositionAndRotation(pivot + delta * (restPos - pivot), delta * restRot);
        }

        static void RestoreStamps(Tracked t)
        {
            if (!t.StampsMoved || !t.Stamps) return;
            t.Stamps.localPosition = t.StampsRestPos;
            t.Stamps.localRotation = t.StampsRestRot;
            t.StampsMoved = false;
        }

        static void AnimateWiggle(Tracked t, float now)
        {
            if (!t.Visuals) return;
            if (now < t.WiggleUntil)
            {
                float k = Mathf.Clamp01((t.WiggleUntil - now) / 0.65f);
                float a = Mathf.Sin(now * 55f) * 7f * k;
                float b = Mathf.Sin(now * 41f + 1.3f) * 4f * k;
                var wiggle = Quaternion.Euler(b, 0, a);
                t.Visuals.localRotation = t.VisualsRest * wiggle;
                WiggleStamps(t, wiggle);
            }
            else
            {
                if (t.Visuals.localRotation != t.VisualsRest) t.Visuals.localRotation = t.VisualsRest;
                RestoreStamps(t);
            }
        }
    }
}
