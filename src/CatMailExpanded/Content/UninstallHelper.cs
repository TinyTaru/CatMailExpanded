using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>Turns all mod parcel types back into ordinary parcels so a save works without the mod.</summary>
    internal static class UninstallHelper
    {
        public static void ConvertAll()
        {
            int n = 0;
            foreach (var p in Object.FindObjectsByType<EntityParcel>(FindObjectsSortMode.None))
            {
                var props = p.Properties;
                if (props == null || !ParcelKinds.IsModValue(props.StorageConstraint)) continue;
                props._StorageConstraint_k__BackingField = StorageConstraint.None;
                n++;
            }
            // the game only knows three ribbon colours: the gift room's other three become red
            foreach (var p in Object.FindObjectsByType<EntityParcel>(FindObjectsSortMode.None))
            {
                var props = p.Properties;
                if (props != null && (int)props._RibbonColor_k__BackingField > 3) props._RibbonColor_k__BackingField = ParcelRibbonColor.Red;
            }
            // Shelves added by the mod disappear without it: take parcels off them and set them down indoors.
            int moved = 0;
            var drop = new Vector3(3.2f, 0.3f, 11.5f);
            foreach (var store in Object.FindObjectsByType<EntityInteractableStore>(FindObjectsSortMode.None))
            {
                if (!store.name.StartsWith("CME_Store_")) continue;
                foreach (var child in store.GetAllChildren())
                {
                    if (child == null) continue;
                    var parcelStore = child.Interactable?.Store?.ParentStore;
                    if (parcelStore != null && parcelStore.Pointer == store.Pointer) store.RemoveEntity(child);
                    child.transform.position = drop + new Vector3((moved % 5) * 0.7f, (moved / 25) * 0.6f, (moved / 5 % 5) * 0.7f);
                    moved++;
                }
            }
            Plugin.Log.Msg($"PrepareUninstall: converted {n} parcels to normal parcels and moved {moved} off mod shelves. Save the game, then remove the mod.");
            try { InterfaceManager.Instance?.NotificationInterface?.ShowNotification($"Cat Mail Expanded: {n} parcels made ordinary. Save now, then remove the mod."); } catch { }
        }
    }
}
