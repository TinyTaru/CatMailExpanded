using System;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using Random = UnityEngine.Random;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Customer dialogue for the mod's parcels:
    ///  - requests: customers asking for a mod-kind parcel describe it with a type-specific line (the game inserts a
    ///    generic "storage" line for constraint values; we swap that for one of ours, or insert one if missing);
    ///  - drop-offs: customers handing in a mod-kind parcel usually mention what it is;
    ///  - gift customers ask for their wrapping in riddles, and the people picking gifts up describe it and react to it;
    ///  - riddle-style descriptions join the game's own pools for native features (hot, fragile, red ribbon...).
    /// </summary>
    internal static class DialogueInjector
    {
        static DialogueData Line(string key, DialogueType type) =>
            new(key, type, DialogueAge.Any, DialogueMood.Neutral, DialogueCategory.None);

        static DialogueData PickRequestLine(ModKind kind, DialogueMood mood)
        {
            Localization.Inject();
            var all = Localization.DialogueLines.Where(l => l.Kind == kind).ToList();
            if (all.Count == 0) return null;
            var moodLines = all.Where(l => l.Mood == mood && mood != DialogueMood.Neutral).ToList();
            var pool = moodLines.Count > 0 && Random.value < 0.6f ? moodLines : all.Where(l => l.Mood == DialogueMood.Neutral).ToList();
            if (pool.Count == 0) pool = all;
            return Line(pool[Random.Range(0, pool.Count)].Key, DialogueType.StorageConstraint);
        }

        public static void Apply(EntityParcel parcel, DialogueMood mood, List<DialogueData> lines)
        {
            if (parcel == null || lines == null) return;
            var kind = ParcelKinds.KindOf(parcel.Properties.StorageConstraint);
            if (kind == ModKind.None || ParcelKinds.IsReturn(kind)) return;
            DialogueData data;
            if (ParcelKinds.IsGift(kind))
            {
                // the person picking up a gift describes the wrapping they were promised (paper and ribbon), so the
                // ribbon line the game adds from the gift's actual ribbon goes
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    var c = lines[i]?.Category;
                    if (c >= DialogueCategory.RibbonRed && c <= DialogueCategory.RibbonAny) lines.RemoveAt(i);
                }
                data = Line(GiftRoom.WantLineKey(parcel), DialogueType.StorageConstraint);
            }
            else data = PickRequestLine(kind, mood);
            if (data == null) return;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null && lines[i].Type == DialogueType.StorageConstraint)
                {
                    lines[i] = data;
                    return;
                }
            }
            int insertAt = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var t = lines[i]?.Type;
                if (t == DialogueType.Hello || t == DialogueType.PersonName) insertAt = i + 1;
            }
            lines.Insert(insertAt, data);
        }

        static EntityParcel Depositing(EntityCustomer c)
        {
            try { return c != null ? c._depositingParcel : null; }
            catch { return null; }
        }

        /// <summary>The game picks the "storage" line by mapping the constraint to a dialogue category; ours have
        /// none, so answer for them directly (the native pick would find no line at all).</summary>
        [HarmonyPatch(typeof(CustomerDialogueManager), nameof(CustomerDialogueManager.GetRandomDialogue),
            new[] { typeof(DialogueType), typeof(StorageConstraint), typeof(CustomerAge), typeof(DialogueMood) })]
        static class GetRandomDialogue_Storage
        {
            static bool Prefix(StorageConstraint storage_constraint, DialogueMood mood, ref DialogueData __result)
            {
                try
                {
                    var kind = ParcelKinds.KindOf(storage_constraint);
                    if (kind == ModKind.None) return true;
                    __result = ParcelKinds.IsReturn(kind) ? null
                        : ParcelKinds.IsGift(kind) ? Line("CME/GiftPaperWant_0", DialogueType.StorageConstraint)   // replaced in Apply
                        : PickRequestLine(kind, mood);
                    return false;
                }
                catch (Exception e) { Plugin.Log.Warning("Storage dialogue: " + e.Message); return true; }
            }
        }

        [HarmonyPatch(typeof(CustomerDialogueManager), nameof(CustomerDialogueManager.GetDepositDialogue))]
        static class GetDepositDialogue
        {
            static void Postfix(EntityCustomer customer, ref DialogueData __result)
            {
                try
                {
                    var p = Depositing(customer);
                    if (p == null || p.Properties == null) return;
                    var kind = ParcelKinds.KindOf(p.Properties.StorageConstraint);
                    if (ParcelKinds.IsGift(kind))
                    {
                        __result = Line(GiftRoom.AskLineKey(p), DialogueType.HelloDeposit);
                        return;
                    }
                    if (kind == ModKind.None || Random.value > 0.75f) return;
                    var pool = Localization.DepositLines.Where(l => l.kind == kind).ToList();
                    if (pool.Count > 0) __result = Line(pool[Random.Range(0, pool.Count)].key, DialogueType.HelloDeposit);
                }
                catch (Exception e) { Plugin.Log.Warning("Deposit dialogue: " + e.Message); }
            }
        }

        [HarmonyPatch(typeof(CustomerDialogueManager), nameof(CustomerDialogueManager.GetDialogue))]
        static class GetDialogue
        {
            static void Postfix(DialogueType dialogue_type, EntityCustomer customer, ref DialogueData __result)
            {
                try
                {
                    if (dialogue_type == DialogueType.Goodbye)
                    {
                        var react = GiftRoom.ReactionKeyFor(customer);
                        if (react != null) __result = Line(react, DialogueType.Goodbye);
                        return;
                    }
                    if (dialogue_type != DialogueType.ByeDeposit) return;
                    var p = Depositing(customer);
                    if (p == null || p.Properties == null || !ParcelKinds.IsGift(p.Properties.StorageConstraint)) return;
                    __result = Line($"CME/GiftBye_{Random.Range(0, Localization.GiftByeCount)}", DialogueType.ByeDeposit);
                }
                catch (Exception e) { Plugin.Log.Warning("Gift goodbye: " + e.Message); }
            }
        }

        /// <summary>Adds the riddle-style descriptions to the game's dialogue pools once they are parsed.</summary>
        [HarmonyPatch(typeof(CustomerDialogueManager), nameof(CustomerDialogueManager.ParseDialogs))]
        static class ParseDialogs
        {
            static void Postfix(CustomerDialogueManager __instance)
            {
                try
                {
                    Localization.Inject();
                    var dict = __instance._dialogues;
                    if (dict == null) return;
                    int added = 0;
                    foreach (var (type, category, key) in Localization.Riddles)
                    {
                        if (!dict.ContainsKey(type)) continue;
                        var list = dict[type];
                        bool present = false;
                        for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].Key == key) { present = true; break; }
                        if (present) continue;
                        list.Add(new DialogueData(key, type, DialogueAge.Any, DialogueMood.Neutral, category));
                        added++;
                    }
                    if (added > 0) Plugin.Log.Msg($"Added {added} riddle descriptions to the customer dialogue");
                }
                catch (Exception e) { Plugin.Log.Warning("Riddles: " + e.Message); }
            }
        }
    }
}
