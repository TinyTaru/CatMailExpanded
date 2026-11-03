using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CatMailExpanded.Core
{
    /// <summary>
    /// Keeps runtime-placed props from clipping: snaps them onto the floor and pushes them horizontally out of
    /// any static scene geometry they overlap. Also reports overlaps for the dev placement tools.
    /// </summary>
    internal static class Placement
    {
        // Layers holding walls, furniture and floors in this game (0 = default props, 9 = room walls).
        const int StaticMask = (1 << 0) | (1 << 9);

        public struct Hit
        {
            public Collider Other;
            public Vector3 Direction;
            public float Distance;
        }

        static bool IsOwn(GameObject root, Collider c) => c.transform.IsChildOf(root.transform);

        static bool Ignore(Collider c)
        {
            if (!c || c.isTrigger || !c.enabled) return true;
            // dynamic entities (parcels, cans, players) are not obstacles for static placement
            if (c.attachedRigidbody && !c.attachedRigidbody.isKinematic) return true;
            var n = c.gameObject.name;
            return n.StartsWith("LightBlocker");
        }

        /// <summary>All static colliders overlapping the prop's own (non-trigger) colliders.</summary>
        public static List<Hit> Overlaps(GameObject root)
        {
            var hits = new List<Hit>();
            foreach (var own in root.GetComponentsInChildren<Collider>())
            {
                if (!own.enabled || own.isTrigger) continue;
                var b = own.bounds;
                foreach (var other in Physics.OverlapBox(b.center, b.extents, Quaternion.identity, StaticMask, QueryTriggerInteraction.Ignore))
                {
                    if (Ignore(other) || IsOwn(root, other)) continue;
                    if (Physics.ComputePenetration(own, own.transform.position, own.transform.rotation,
                            other, other.transform.position, other.transform.rotation, out var dir, out var dist) && dist > 0.002f
                        && Mathf.Abs(dir.y) < 0.7f) // vertical pushes are just floor/ceiling contact
                        hits.Add(new Hit { Other = other, Direction = dir, Distance = dist });
                }
            }
            return hits;
        }

        /// <summary>Drops the prop onto the highest static surface under it.</summary>
        public static bool SnapToFloor(GameObject root, float maxDrop = 1.5f)
        {
            var b = World.RendererBounds(root);
            float bestY = float.NegativeInfinity;
            // sample the footprint so a prop never ends up floating over an edge or sunk into a step
            for (int i = 0; i < 5; i++)
            {
                var o = b.center;
                if (i > 0)
                {
                    o.x += (i % 2 == 0 ? 1 : -1) * b.extents.x * 0.6f;
                    o.z += (i < 3 ? 1 : -1) * b.extents.z * 0.6f;
                }
                o.y = b.min.y + 0.3f;
                foreach (var h in Physics.RaycastAll(o, Vector3.down, maxDrop + 0.3f, StaticMask, QueryTriggerInteraction.Ignore))
                {
                    if (Ignore(h.collider) || IsOwn(root, h.collider)) continue;
                    if (h.point.y > bestY) bestY = h.point.y;
                }
            }
            if (float.IsNegativeInfinity(bestY)) return false;
            root.transform.position += Vector3.up * (bestY - b.min.y);
            return true;
        }

        /// <summary>Pushes the prop out of overlapping static geometry (horizontal moves only).</summary>
        public static Vector3 Resolve(GameObject root, int iterations = 10)
        {
            var start = root.transform.position;
            for (int i = 0; i < iterations; i++)
            {
                var hits = Overlaps(root);
                Vector3 push = Vector3.zero;
                foreach (var h in hits)
                {
                    var d = h.Direction * (h.Distance + 0.01f);
                    d.y = 0;
                    if (d.sqrMagnitude > push.sqrMagnitude) push = d;
                }
                if (push.sqrMagnitude < 1e-6f) break;
                root.transform.position += push;
                Physics.SyncTransforms();
            }
            return root.transform.position - start;
        }

        /// <summary>Floor snap + overlap resolution; logs what it had to change.</summary>
        public static void Settle(GameObject root, bool snapFloor = true)
        {
            if (!root) return;
            try
            {
                Physics.SyncTransforms();
                if (snapFloor) SnapToFloor(root);
                Physics.SyncTransforms();
                var moved = Resolve(root);
                if (snapFloor && moved.sqrMagnitude > 1e-6f) { SnapToFloor(root); Physics.SyncTransforms(); }
                if (moved.sqrMagnitude > 0.0004f)
                    Plugin.Log.Msg($"Placement: nudged {root.name} by {moved} to stay clear of walls/furniture");
            }
            catch (Exception e) { Plugin.Log.Warning("Placement failed for " + root.name + ": " + e.Message); }
        }

        /// <summary>
        /// Settle using the prop's whole visual volume (a temporary box), so parts without colliders —
        /// an open lid, speaker cones — can't end up inside walls either.
        /// </summary>
        public static void SettleWithBounds(GameObject root, bool snapFloor = true)
        {
            if (!root) return;
            var temp = new GameObject("CME_PlacementVolume");
            temp.transform.SetParent(root.transform, false);
            var box = temp.AddComponent<BoxCollider>();
            var mf = root.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                var b = mf.sharedMesh.bounds;
                box.center = b.center;
                box.size = new Vector3(b.size.x * 0.96f, b.size.y * 0.96f, b.size.z * 0.96f);
            }
            else
            {
                var b = World.RendererBounds(root);
                box.center = root.transform.InverseTransformPoint(b.center);
                box.size = b.size * 0.96f;
            }
            Physics.SyncTransforms();
            Settle(root, snapFloor);
            UnityEngine.Object.DestroyImmediate(temp);
            Physics.SyncTransforms();
        }

        public static string Describe(GameObject root)
        {
            var sb = new StringBuilder();
            var b = World.RendererBounds(root);
            sb.AppendLine(FormattableString.Invariant($"{root.name} pos={root.transform.position} yaw={root.transform.eulerAngles.y:0} bounds min=({b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00}) max=({b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00})"));
            var hits = Overlaps(root);
            if (hits.Count == 0) sb.AppendLine("  no overlaps");
            foreach (var h in hits) sb.AppendLine($"  overlaps {World.PathOf(h.Other.transform)} by {h.Distance:0.000} dir {h.Direction}");
            return sb.ToString();
        }
    }
}
