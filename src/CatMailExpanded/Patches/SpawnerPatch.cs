using System;
using CatMailExpanded.Content;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;

namespace CatMailExpanded.Patches
{
    /// <summary>
    /// The cheat spawner lists StorageConstraint enum names and turns the chosen label back into a value
    /// with Enum.Parse. Our types aren't enum members, so list them by name and, while the spawned parcel
    /// is being set up, show the game their numeric value instead (Enum.Parse accepts numbers).
    /// </summary>
    static class SpawnerPatch
    {
        static readonly (string label, StorageConstraint value)[] Extra =
        {
            ("Strawberry", ParcelKinds.Strawberry),
            ("Banana", ParcelKinds.Banana),
            ("Restless", ParcelKinds.Restless),
            ("Otherworldly", ParcelKinds.Otherworldly),
            ("Spectral", ParcelKinds.Spectral),
        };

        [HarmonyPatch(typeof(SpawnerInterface), nameof(SpawnerInterface.InitializeConstraintsDropdown))]
        static class AddOptions
        {
            static void Postfix(SpawnerInterface __instance)
            {
                try
                {
                    var dd = __instance.ConstraintDropdown;
                    if (dd == null) return;
                    foreach (var (label, _) in Extra)
                    {
                        bool present = false;
                        foreach (var o in dd.options) if (o.text == label) { present = true; break; }
                        if (!present) dd.options.Add(new TMP_Dropdown.OptionData(label));
                    }
                    dd.RefreshShownValue();
                }
                catch (Exception e) { Plugin.Log.Warning("Spawner options: " + e.Message); }
            }
        }

        [HarmonyPatch(typeof(SpawnerInterface), nameof(SpawnerInterface.NetworkSpawnerManager_OnEntitySpawned))]
        static class UseValue
        {
            static void Prefix(SpawnerInterface __instance, out string __state)
            {
                __state = null;
                var caption = __instance.ConstraintDropdown?.captionText;
                if (caption == null) return;
                foreach (var (label, value) in Extra)
                    if (caption.text == label)
                    {
                        __state = label;
                        caption.text = ((int)value).ToString();
                        return;
                    }
            }

            static Exception Finalizer(SpawnerInterface __instance, string __state, Exception __exception)
            {
                if (__state != null)
                {
                    var caption = __instance.ConstraintDropdown?.captionText;
                    if (caption != null) caption.text = __state;
                }
                return __exception;
            }
        }
    }
}
