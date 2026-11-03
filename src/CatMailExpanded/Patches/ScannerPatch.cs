using System;
using System.Collections.Generic;
using CatMailExpanded.Content;
using CatMailExpanded.Core;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Patches
{
    /// <summary>
    /// The scanner screen draws constraint symbols with the game's sh_ScannerIcons shader from a 3x3 glyph atlas.
    /// For the new parcel types we hand the symbol renderer a copy of the game's own scanner material that samples
    /// our glyph atlas instead (the glyph is repeated in every cell, so whichever cell the shader picks shows it).
    /// Everything else — colour, fade-in animation, opacity — stays the game's.
    /// </summary>
    [HarmonyPatch(typeof(EntityScanner), nameof(EntityScanner.ScanParcel))]
    static class EntityScanner_ScanParcel
    {
        static MeshRenderer _renderer;
        static Material _original;
        static readonly Dictionary<ModKind, Material> _materials = new();

        static void Postfix(EntityScanner __instance, int child_parcel_count)
        {
            try { Apply(__instance, child_parcel_count); }
            catch (Exception e) { Plugin.Log.Warning("Scanner symbol failed: " + e.Message); }
        }

        static void Apply(EntityScanner scanner, int count)
        {
            var renderer = scanner.StorageConstraintRenderer;
            if (renderer == null) return;
            if (_renderer == null || _renderer.Pointer != renderer.Pointer)
            {
                _renderer = renderer;
                _original = renderer.sharedMaterial;
            }

            ModKind kind = ModKind.None;
            if (count == 1)
            {
                var children = scanner.LinkedStore?.GetAllChildren();
                if (children != null && children.Count > 0)
                {
                    var parcel = children[0]?.TryCast<EntityParcel>();
                    if (parcel != null) kind = ParcelKinds.KindOf(parcel.Properties.StorageConstraint);
                }
            }

            if (kind == ModKind.None || ParcelKinds.IsReturn(kind))
            {
                if (_original && renderer.sharedMaterial != _original) renderer.sharedMaterial = _original;
                return;
            }

            // The game has already set ConstraintTextureID / SymbolColor on the instance material; carry those over.
            var current = renderer.material;
            var mat = MaterialFor(kind);
            if (current)
            {
                if (current.HasProperty("_SymbolColor")) mat.SetColor("_SymbolColor", current.GetColor("_SymbolColor"));
                if (current.HasProperty("_ConstraintTextureID")) mat.SetFloat("_ConstraintTextureID", Mathf.Max(1f, current.GetFloat("_ConstraintTextureID")));
            }
            renderer.sharedMaterial = mat;
            renderer.enabled = true;
        }

        static Material MaterialFor(ModKind kind)
        {
            if (_materials.TryGetValue(kind, out var m) && m) return m;
            m = new Material(_original ? _original : _renderer.sharedMaterial) { name = "CME_ScannerIcons_" + kind };
            var tex = ModAssets.Texture(kind switch
            {
                ModKind.Strawberry => "scan_strawberry",
                ModKind.Banana => "scan_banana",
                ModKind.Otherworldly => "scan_otherworldly",
                ModKind.Spectral => "scan_spectral",
                ModKind.Gift or ModKind.GiftWrapped => "scan_gift",
                _ => "scan_restless",
            });
            tex.wrapMode = TextureWrapMode.Clamp;
            // the atlas texture property has a generated shader-graph name: replace whichever texture it uses
            var sh = m.shader;
            for (int i = 0; i < sh.GetPropertyCount(); i++)
                if (sh.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                    m.SetTexture(sh.GetPropertyName(i), tex);
            m.SetFloat("_UseRChannel", 0f);
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _materials[kind] = m;
            return m;
        }

        public static void Reset()
        {
            _renderer = null;
            _original = null;
            _materials.Clear();
        }
    }
}
