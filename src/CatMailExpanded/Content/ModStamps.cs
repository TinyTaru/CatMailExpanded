using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Real constraint stickers for the new parcel types, built from a clone of a native constraint sticker
    /// with our art. With these attached to our constraint settings the game treats the new types like Cold
    /// or Hot: they appear in the sticker menu's constraint tab, the player sticks them on customer parcels
    /// (which arrive blank), and parcels the game itself stamps get them automatically.
    ///
    /// Stamps are saved and synced by StampData name and reloaded through Addressables, which knows nothing
    /// of ours, so AddStampFromNetwork (also used by save loading) is handled here for our names.
    /// </summary>
    internal static class ModStamps
    {
        static readonly Dictionary<string, StampDataStorageConstraint> _byName = new();
        static readonly Dictionary<ModKind, StampDataStorageConstraint> _byKind = new();
        static GameObject _holder;

        public static string NameFor(ModKind k) => "stamp_Constraint_CME_" + k;

        public static bool TryGet(string name, out StampDataStorageConstraint data)
        {
            data = null;
            return name != null && _byName.TryGetValue(name, out data) && data;
        }

        /// <summary>Gives every mod constraint setting its sticker (creating the stickers on first use).</summary>
        public static void Attach(EntityPropertiesManager epm)
        {
            if (epm == null || epm.Constraints == null) return;
            StampData template = null;
            foreach (var cs in epm.Constraints)
                if (cs != null && cs.Stamp && cs.Stamp.StampPrefab && !ParcelKinds.IsModValue(cs.Constraint) &&
                    cs.Stamp.GetIl2CppType().Name == nameof(StampDataStorageConstraint)) { template = cs.Stamp; break; }
            if (!template) { Plugin.Log.Warning("No native constraint sticker to copy"); return; }
            int made = 0;
            foreach (var cs in epm.Constraints)
            {
                if (cs == null || !ParcelKinds.IsModValue(cs.Constraint)) continue;
                var kind = ParcelKinds.KindOf(cs.Constraint);
                if (ParcelKinds.StickerTexture(kind) == null) continue;   // return items carry no sticker
                if (!_byKind.TryGetValue(kind, out var data) || !data)
                {
                    try { data = Create(template, kind, cs.Constraint); made++; }
                    catch (Exception e) { Plugin.Log.Error($"Sticker for {kind} failed: {e}"); continue; }
                }
                cs._Stamp_k__BackingField = data;
            }
            if (made > 0) Plugin.Log.Msg($"Created {made} constraint stickers");
        }

        static StampDataStorageConstraint Create(StampData template, ModKind kind, StorageConstraint value)
        {
            if (!_holder)
            {
                _holder = new GameObject("CME_StampPrefabs");
                _holder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(_holder);
                _holder.hideFlags = HideFlags.HideAndDontSave;
            }
            var go = UnityEngine.Object.Instantiate(template.StampPrefab.gameObject, _holder.transform);
            go.name = "p_Stamp_Constraint_CME_" + kind;
            var mesh = ParcelTracker.StickerQuad(0.25f);
            var tex = ModAssets.Texture(ParcelKinds.StickerTexture(kind));
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) mf.sharedMesh = mesh;
            Texture atlas = null;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                // same sticker material as the native constraint stamps, with our art instead of the atlas
                if (!atlas && mr.sharedMaterial) atlas = mr.sharedMaterial.GetTexture("_BaseMap");
                mr.sharedMaterial = Retexture(mr.sharedMaterial, atlas, tex, "m_Sticker_CME_" + kind);
            }
            // StampHelper re-applies StartMaterial once the stamping animation ends and draws the animation
            // with FeedbackMaterial; both still reference the native sticker sheet, so give them our art too
            var helper = go.GetComponent<StampHelper>();
            if (helper)
            {
                if (helper.StartMaterial)
                    helper._StartMaterial_k__BackingField = Retexture(helper.StartMaterial, atlas, tex, "m_Sticker_CME_" + kind);
                if (helper.FeedbackMaterial)
                    helper._FeedbackMaterial_k__BackingField = Retexture(helper.FeedbackMaterial, atlas, tex, "m_StickerFeedback_CME_" + kind);
            }
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "sp_CME_Sticker_" + kind;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;

            var data = ScriptableObject.CreateInstance<StampDataStorageConstraint>();
            data.name = NameFor(kind);
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;
            data._StampPrefab_k__BackingField = go.GetComponent<StampHelper>();
            data._StampPreviewSprite_k__BackingField = sprite;
            data._MaximumRandomizedTilt_k__BackingField = template.MaximumRandomizedTilt;
            data._Constraint_k__BackingField = value;
            _byName[data.name] = data;
            _byKind[kind] = data;
            return data;
        }

        static readonly Dictionary<string, Material> _retextured = new();

        /// <summary>A copy of a native sticker material with every reference to the sticker sheet replaced by ours.</summary>
        static Material Retexture(Material source, Texture atlas, Texture art, string name)
        {
            string key = name + "|" + source.name;
            if (_retextured.TryGetValue(key, out var cached) && cached) return cached;
            var m = new Material(source) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset };
            var sh = m.shader;
            for (int i = 0; i < sh.GetPropertyCount(); i++)
            {
                if (sh.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Texture) continue;
                var pn = sh.GetPropertyName(i);
                var t = m.GetTexture(pn);
                if (pn == "_BaseMap" || (atlas && t == atlas)) m.SetTexture(pn, art);
            }
            _retextured[key] = m;
            return m;
        }

        /// <summary>Recreates one of our stickers on a parcel from a save or the network.</summary>
        public static void AddFromNetwork(EntityInteractableStamp target, StampDataStorageConstraint data, int identifier,
            Vector3 localPosition, Quaternion localRotation, bool fromPlayer)
        {
            var parent = target.StampsParent ? target.StampsParent : target.transform;
            var helper = UnityEngine.Object.Instantiate(data.StampPrefab, parent);
            helper.gameObject.SetActive(true);
            helper.transform.localPosition = localPosition;
            helper.transform.localRotation = localRotation;
            var instance = new StampInstance(identifier, helper, data, fromPlayer);
            target._stampInstances.Add(instance);
            if (identifier >= target._stampNextIdentifier) target._stampNextIdentifier = identifier + 1;
            try { target.StampAdded?.Invoke(instance); } catch (Exception e) { Plugin.Log.Warning("StampAdded: " + e.Message); }
            try { target.CheckOnboardingValidation(); } catch { }
        }
    }

    [HarmonyPatch(typeof(EntityInteractableStamp), nameof(EntityInteractableStamp.AddStampFromNetwork))]
    static class EntityInteractableStamp_AddStampFromNetwork
    {
        static bool Prefix(EntityInteractableStamp __instance, string addressable_key, int identifier,
            Vector3 local_position, Quaternion local_rotation, bool from_player)
        {
            if (!ModStamps.TryGet(addressable_key, out var data)) return true;
            try { ModStamps.AddFromNetwork(__instance, data, identifier, local_position, local_rotation, from_player); }
            catch (Exception e) { Plugin.Log.Error("Restoring sticker failed: " + e); }
            return false;
        }
    }
}
