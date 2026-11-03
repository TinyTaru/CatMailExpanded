using System;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Creates working parcel storage grids by cloning the game's own storage entities.
    /// Clones get stable permanent identifiers so parcels stored on them save and reload.
    /// </summary>
    internal static class StoreFactory
    {
        public const int IdBase = 917000; // far above the game's own identifiers (~400)

        static GameObject FindTemplate(string storeName)
        {
            // Prefer a template sitting on a plain shelf (no special flags)
            GameObject fallback = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go || !go.scene.IsValid()) continue;
                if (!go.name.StartsWith(storeName)) continue;
                if (!go.GetComponent<EntityInteractableStore>()) continue;
                var path = World.PathOf(go.transform);
                if (path.Contains("CME_")) continue;
                if (path.Contains("p_Shelves_")) return go;
                fallback ??= go;
            }
            return fallback;
        }

        /// <summary>Clones a storage grid (e.g. "p_Entity_Storage_2x6") at a world pose.</summary>
        public static EntityInteractableStore Create(string storeName, Transform parent, Vector3 pos, float yaw, int idOffset, float maxHeight = -1f)
        {
            var template = FindTemplate(storeName);
            if (!template) { Plugin.Log.Warning("No store template " + storeName); return null; }
            var holder = new GameObject("CME_StoreHolder");
            holder.SetActive(false);
            var clone = UnityEngine.Object.Instantiate(template, holder.transform);
            // drop everything that is not part of the store itself (e.g. parcels currently on the template)
            for (int i = clone.transform.childCount - 1; i >= 0; i--)
            {
                var c = clone.transform.GetChild(i).gameObject;
                if (c.name == "Visuals" || c.name == "Colliders") continue;
                UnityEngine.Object.DestroyImmediate(c);
            }
            int id = IdBase + idOffset;
            clone.name = $"CME_Store_{id}_{storeName}";
            var pid = clone.GetComponent<EntityPermanentIdentifier>();
            if (pid) pid._PermanentIdentifier_k__BackingField = id;
            var store = clone.GetComponent<EntityInteractableStore>();
            if (store && maxHeight > 0) store._StorageMaximumHeight_k__BackingField = maxHeight;
            clone.transform.SetParent(parent, false);
            clone.transform.position = pos;
            clone.transform.rotation = Quaternion.Euler(0, yaw, 0);
            UnityEngine.Object.Destroy(holder);
            return store;
        }
    }
}
