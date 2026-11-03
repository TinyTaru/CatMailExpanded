using System;
using CatMailExpanded.Content;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CatMailExpanded.Patches
{
    [HarmonyPatch(typeof(EntityPropertiesManager), nameof(EntityPropertiesManager.Awake))]
    static class EntityPropertiesManager_Awake
    {
        static void Postfix(EntityPropertiesManager __instance)
        {
            try { ConstraintRegistration.EnsureRegistered(__instance); }
            catch (Exception e) { Plugin.Log.Error("Constraint registration failed: " + e); }
        }
    }

    /// <summary>New constraints have no native stamp: hide their settings while the game collects stamps
    /// (it dereferences each settings' StampData), and drop any null entries from the result.</summary>
    [HarmonyPatch(typeof(EntityPropertiesManager), nameof(EntityPropertiesManager.GetStamps))]
    static class EntityPropertiesManager_GetStamps
    {
        static void Prefix(EntityPropertiesManager __instance, out ConstraintSettings[] __state) =>
            __state = ConstraintRegistration.HideStampless(__instance);

        static Exception Finalizer(EntityPropertiesManager __instance, ConstraintSettings[] __state, Exception __exception)
        {
            ConstraintRegistration.Restore(__instance, __state);
            return __exception;
        }

        static void Postfix(List<StampData> __result)
        {
            if (__result == null) return;
            for (int i = __result.Count - 1; i >= 0; i--)
                if (__result[i] == null) __result.RemoveAt(i);
        }
    }

    /// <summary>The sticker menu builds one button per unlocked settings entry; ours have no sticker.</summary>
    [HarmonyPatch(typeof(StampSelectionCategoryInterface), nameof(StampSelectionCategoryInterface.Populate))]
    static class StampSelectionCategoryInterface_Populate
    {
        static void Prefix(out ConstraintSettings[] __state) =>
            __state = ConstraintRegistration.HideStampless(EntityPropertiesManager.Instance);

        static Exception Finalizer(StampSelectionCategoryInterface __instance, ConstraintSettings[] __state, Exception __exception)
        {
            ConstraintRegistration.Restore(EntityPropertiesManager.Instance, __state);
            if (__exception != null) Plugin.Log.Warning($"Sticker menu populate ({__instance?.name}): {__exception.Message}");
            return null;
        }
    }

    [HarmonyPatch(typeof(StampSelectionCategoryInterface), nameof(StampSelectionCategoryInterface.EntityPropertiesManager_OnUnlockedStampSettings))]
    static class StampSelectionCategoryInterface_OnUnlocked
    {
        static bool Prefix(StampSettings settings) => settings == null || settings.Stamp != null;
    }

    /// <summary>
    /// Scented and restless parcels have their own consequences (fog / hopping) instead of recap damage, so their
    /// constraint value is hidden from the native storage damage check. Otherworldly parcels are damaged the way
    /// cold parcels are: inside the planetarium the check sees no constraint, anywhere else it sees a native
    /// constraint that no room the parcel is in protects, so the game's own rules (timing, boat, piles) decide.
    /// </summary>
    [HarmonyPatch(typeof(EntityProperties), nameof(EntityProperties.CheckForParcelDamage))]
    static class EntityProperties_CheckForParcelDamage
    {
        static void Prefix(EntityProperties __instance, out StorageConstraint __state)
        {
            __state = __instance._StorageConstraint_k__BackingField;
            if (!ParcelKinds.IsModValue(__state)) return;
            __instance._StorageConstraint_k__BackingField =
                __state == ParcelKinds.Otherworldly ? OtherworldlyStandIn(__instance) : StorageConstraint.None;
        }

        static StorageConstraint OtherworldlyStandIn(EntityProperties props)
        {
            if (Zones.InPlanetarium(props.transform.position)) return StorageConstraint.None;
            StorageConstraint safe = StorageConstraint.None;
            try { foreach (var room in props._storageRoomsIn) if (room) safe |= room.SafeConstraints; } catch { }
            foreach (var c in new[] { StorageConstraint.Cold, StorageConstraint.Hot, StorageConstraint.Dark, StorageConstraint.Bright })
                if ((safe & c) == 0) return c;
            return StorageConstraint.Cold;
        }

        static void Postfix(EntityProperties __instance, StorageConstraint __state)
        {
            if (ParcelKinds.IsModValue(__state))
                __instance._StorageConstraint_k__BackingField = __state;
        }
    }

    [HarmonyPatch(typeof(ParcelManager), nameof(ParcelManager.RegisterParcel))]
    static class ParcelManager_RegisterParcel
    {
        static void Postfix(EntityParcel parcel)
        {
            try { ParcelTracker.Register(parcel); }
            catch (Exception e) { Plugin.Log.Warning("Track parcel failed: " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(CustomerDialogueManager), nameof(CustomerDialogueManager.Awake))]
    static class CustomerDialogueManager_Awake
    {
        static void Postfix()
        {
            try { Localization.Inject(); }
            catch (Exception e) { Plugin.Log.Error("Localization inject failed: " + e); }
        }
    }

    [HarmonyPatch(typeof(CustomerDialogueManager), nameof(CustomerDialogueManager.GetRequestDialogue))]
    static class CustomerDialogueManager_GetRequestDialogue
    {
        static void Postfix(EntityParcel requested_parcel, DialogueMood mood, List<DialogueData> __result)
        {
            try { DialogueInjector.Apply(requested_parcel, mood, __result); }
            catch (Exception e) { Plugin.Log.Warning("Dialogue injection failed: " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(ProgressionManager), nameof(ProgressionManager.GameManager_OnGameStarted))]
    static class ProgressionManager_OnGameStarted
    {
        static void Postfix()
        {
            try { ModProgress.OnGameStarted(); }
            catch (Exception e) { Plugin.Log.Error("OnGameStarted failed: " + e); }
        }
    }

    [HarmonyPatch(typeof(ProgressionManager), nameof(ProgressionManager.SetLevel))]
    static class ProgressionManager_SetLevel
    {
        static void Postfix(bool is_from_save)
        {
            if (!ModProgress.GameStarted) return;
            try { ModProgress.Refresh(is_from_save); }
            catch (Exception e) { Plugin.Log.Error("SetLevel refresh failed: " + e); }
        }
    }

    [HarmonyPatch(typeof(EntityOldPostman), nameof(EntityOldPostman.EndCinematic))]
    static class EntityOldPostman_EndCinematic
    {
        static void Postfix()
        {
            try { Scheduler_RefreshSoon(); }
            catch (Exception e) { Plugin.Log.Error("EndCinematic refresh failed: " + e); }
        }

        static void Scheduler_RefreshSoon()
        {
            ModProgress.EndingSeenThisSession = true;
            Core.Scheduler.After(1.5f, () => ModProgress.Refresh(false));
        }
    }
}

namespace CatMailExpanded.Patches
{
    /// <summary>Our extra shelves must exist before saved parcels look up their parent storage.</summary>
    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.SaveManager), nameof(Il2Cpp.SaveManager.LoadAllEntities))]
    static class SaveManager_LoadAllEntities
    {
        static void Prefix()
        {
            try { CatMailExpanded.Content.WorldSetup.EnsureBuilt(); }
            catch (System.Exception e) { Plugin.Log.Error("World setup before load failed: " + e); }
        }
    }
}

namespace CatMailExpanded.Patches
{
    /// <summary>Holding a tuna can and looking at the bin: Drop or Interact throws it away.</summary>
    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.PlayerInventory), nameof(Il2Cpp.PlayerInventory.InputManager_OnDropPerformed))]
    static class PlayerInventory_OnDropPerformed
    {
        static bool Prefix(Il2Cpp.PlayerInventory __instance)
        {
            try { return !CatMailExpanded.Content.TunaCans.TryTrashHeld(__instance); }
            catch (System.Exception e) { Plugin.Log.Warning("Trash on drop failed: " + e.Message); return true; }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.PlayerInventory), nameof(Il2Cpp.PlayerInventory.InputManager_OnInteractPerformed))]
    static class PlayerInventory_OnInteractPerformed
    {
        static bool Prefix(Il2Cpp.PlayerInventory __instance)
        {
            try { return !CatMailExpanded.Content.TunaCans.TryTrashHeld(__instance); }
            catch (System.Exception e) { Plugin.Log.Warning("Trash on interact failed: " + e.Message); return true; }
        }
    }
}

namespace CatMailExpanded.Patches
{
    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.PlayerInteraction), nameof(Il2Cpp.PlayerInteraction.InputManager_OnInteractPerformed))]
    static class PlayerInteraction_OnInteractPerformed
    {
        static bool Prefix()
        {
            try
            {
                var inv = Il2Cpp.PlayerManager.Instance?.LocalPlayerEntity?.PlayerInventory;
                return !CatMailExpanded.Content.TunaCans.TryTrashHeld(inv);
            }
            catch (System.Exception e) { Plugin.Log.Warning("Trash on interact failed: " + e.Message); return true; }
        }
    }
}
