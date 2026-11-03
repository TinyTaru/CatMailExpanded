using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The main mail pile used to turn bottomless once it reached the break room. Now it keeps receding down the
    /// extended corridor (PileWing) through five more stages, copies of the game's own pile stages, revealing the
    /// Catazon returns room's door and then the mirror room's; only then does the bottomless pile take over, at
    /// the corridor's new end. Every machine inserts the same stages before the pile reads its saved stage, so
    /// stage numbers mean the same thing for host and clients. A save that had reached the bottomless pile
    /// (stage 7) carries on from the first new stage.
    /// </summary>
    internal static class PileExtension
    {
        public const int FirstNewStage = 7;
        // (template stage, z shift): each copy keeps its template's shape, a couple of metres further down
        static readonly (int template, float shift)[] NewStages =
        {
            (5, 6.40f),    // front 25.0: clear of the planetarium's and break room's doors, like the old bottomless pile
            (3, 10.90f),   // front 27.0
            (4, 11.20f),   // front 29.0
            (5, 12.40f),   // front 31.0
            (3, 16.80f),   // front 32.9: the returns room's door is clear
        };
        public static int ReturnsStage => FirstNewStage + NewStages.Length - 1;   // 11
        public static int InfiniteStage => FirstNewStage + NewStages.Length;      // 12: the mirror room's door is clear
        const int PackagesPerNewStage = 90;

        public static bool Extended { get; private set; }
        public static int CurrentStage { get; private set; } = -2;
        public static event Action StageChanged;

        public static void Reset()
        {
            Extended = false;
            CurrentStage = -2;
        }

        static EntityPile MainPile()
        {
            foreach (var p in UnityEngine.Object.FindObjectsByType<EntityPile>(FindObjectsSortMode.None))
                if (p && p.PileType == PileType.MainPile) return p;
            return null;
        }

        /// <summary>Stage index of the main pile (-1 once cleared, which the main pile never is).</summary>
        public static int Stage()
        {
            var p = MainPile();
            return p ? p.CurrentPileSizeIndex : -2;
        }

        public static bool ReturnsRevealed(int stage) => stage >= ReturnsStage || stage == -1;
        public static bool MirrorRevealed(int stage) => stage >= InfiniteStage || stage == -1;

        static void Extend(EntityPile pile)
        {
            var sizes = pile._PileSizes_k__BackingField;
            var counts = pile._PackagesPerSize_k__BackingField;
            if (sizes == null || counts == null || sizes.Length != 8 || counts.Length != 8) return;
            var last = sizes[7];
            if (last == null || !last.ParentGameObject || last.ParentGameObject.name != "Pile_Infinite") return;

            var newSizes = new List<PileSize>();
            var newCounts = new List<int>();
            for (int i = 0; i < FirstNewStage; i++) { newSizes.Add(sizes[i]); newCounts.Add(counts[i]); }
            int n = 0;
            foreach (var (template, shift) in NewStages)
            {
                var src = sizes[template];
                newSizes.Add(CloneStage(src, shift, "CME_Pile_" + (FirstNewStage + n)));
                newCounts.Add(PackagesPerNewStage);
                n++;
            }
            // the bottomless pile moves to the corridor's new end
            var inf = last.ParentGameObject.transform;
            if (inf.position.z < PileWing.OldEnd + 3f) inf.position += new Vector3(0, 0, PileWing.EndShift);
            newSizes.Add(last);
            newCounts.Add(counts[7]);

            pile._PileSizes_k__BackingField = new Il2CppReferenceArray<PileSize>(newSizes.ToArray());
            pile._PackagesPerSize_k__BackingField = new Il2CppStructArray<int>(newCounts.ToArray());

            // buried props that wake up when a later stage is reached keep pointing at the same pile
            foreach (var t in UnityEngine.Object.FindObjectsByType<TurnPhysicsOnOnPileRevealed>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t._LinkedPileType_k__BackingField == PileType.MainPile && t._LinkedPileIndex_k__BackingField >= FirstNewStage)
                    t._LinkedPileIndex_k__BackingField += NewStages.Length;
            Extended = true;
            Plugin.Log.Msg($"Main pile extended to {newSizes.Count} stages");
        }

        static PileSize CloneStage(PileSize src, float shift, string name)
        {
            var srcGo = src.ParentGameObject;
            bool wasActive = srcGo.activeSelf;
            var copy = UnityEngine.Object.Instantiate(srcGo, srcGo.transform.parent);
            copy.name = name;
            copy.SetActive(false);
            copy.transform.position = srcGo.transform.position + new Vector3(0, 0, shift);
            if (wasActive != srcGo.activeSelf) srcGo.SetActive(wasActive);

            // the anchors are transforms inside the stage: find the same ones in the copy
            var anchors = new List<Transform>();
            var srcAnchors = src.PackageAnchorPoints;
            if (srcAnchors != null)
                foreach (var a in srcAnchors)
                {
                    if (!a) continue;
                    var path = RelativePath(srcGo.transform, a);
                    var found = path != null ? copy.transform.Find(path) : null;
                    if (!found)
                    {
                        // not inside the stage: make a matching point in the copy
                        found = new GameObject(a.name).transform;
                        found.SetParent(copy.transform, false);
                        found.SetPositionAndRotation(a.position + new Vector3(0, 0, shift), a.rotation);
                    }
                    anchors.Add(found);
                }
            var size = new PileSize();
            size._ParentGameObject_k__BackingField = copy;
            size._PackageAnchorPoints_k__BackingField = new Il2CppReferenceArray<Transform>(anchors.ToArray());
            return size;
        }

        static string RelativePath(Transform root, Transform t)
        {
            if (t == root) return "";
            var parts = new List<string>();
            while (t && t != root) { parts.Insert(0, t.name); t = t.parent; }
            return t == root ? string.Join("/", parts) : null;
        }

        static void OnShown(EntityPile pile, int index)
        {
            if (pile.PileType != PileType.MainPile) return;
            if (index == CurrentStage) return;
            CurrentStage = index;
            StageChanged?.Invoke();
        }

        // ------------------------------------------------------------------ patches

        [HarmonyPatch(typeof(EntityPile), nameof(EntityPile.Start))]
        static class EntityPile_Start
        {
            static void Prefix(EntityPile __instance)
            {
                try { if (__instance.PileType == PileType.MainPile) Extend(__instance); }
                catch (Exception e) { Plugin.Log.Error("Pile extension failed: " + e); }
            }
        }

        [HarmonyPatch(typeof(EntityPile), nameof(EntityPile.ShowPileSize))]
        static class EntityPile_ShowPileSize
        {
            static void Postfix(EntityPile __instance, int index)
            {
                try { OnShown(__instance, index); }
                catch (Exception e) { Plugin.Log.Warning("Pile stage hook: " + e.Message); }
            }
        }

        /// <summary>
        /// A saved parcel remembers which anchor of the current stage it sat on. Saves made at the old bottomless
        /// stage now load into the first new stage, whose anchors are numbered differently; keep the index in range
        /// (the game throws otherwise, which would stall loading).
        /// </summary>
        [HarmonyPatch(typeof(EntityPile), nameof(EntityPile.AssignEntityToAnchorFromSave))]
        static class EntityPile_AssignFromSave
        {
            static void Prefix(EntityPile __instance, ref int anchor_index)
            {
                try
                {
                    var sizes = __instance._PileSizes_k__BackingField;
                    int idx = __instance.CurrentPileSizeIndex;
                    if (sizes == null || idx < 0 || idx >= sizes.Length) return;
                    var anchors = sizes[idx].PackageAnchorPoints;
                    if (anchors == null || anchors.Length == 0) return;
                    if (anchor_index < 0 || anchor_index >= anchors.Length) anchor_index = Math.Abs(anchor_index) % anchors.Length;
                }
                catch (Exception e) { Plugin.Log.Warning("Pile anchor clamp: " + e.Message); }
            }
        }
    }
}
