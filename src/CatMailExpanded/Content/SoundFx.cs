using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>Plays the game's own audio assets (looked up by name) through its AudioManager.</summary>
    internal static class SoundFx
    {
        static readonly Dictionary<string, AudioAsset_ScriptableObject> _cache = new();

        public static AudioAsset_ScriptableObject Find(params string[] nameFragments)
        {
            var key = string.Join("|", nameFragments);
            if (_cache.TryGetValue(key, out var a) && a) return a;
            foreach (var frag in nameFragments)
            {
                foreach (var asset in Resources.FindObjectsOfTypeAll<AudioAsset_ScriptableObject>())
                {
                    if (asset && asset.name.Contains(frag, StringComparison.OrdinalIgnoreCase))
                    {
                        _cache[key] = asset;
                        return asset;
                    }
                }
            }
            return null;
        }

        public static void Play(Vector3 pos, float volume, params string[] names)
        {
            try
            {
                var asset = Find(names);
                if (asset == null || AudioManager.Instance == null) return;
                AudioManager.Instance.PlayOneShot(asset, pos, volume, 1f, 0);
            }
            catch (Exception e) { Plugin.Log.Warning("Sound failed: " + e.Message); }
        }

        public static void PlayBoing(Vector3 pos) => Play(pos, 0.8f, "Parcel_Jump", "Jump", "Parcel_Impact", "Impact_Parcel", "Parcel_Place");
        public static void PlayTrash(Vector3 pos) => Play(pos, 1f, "Trash", "Bin", "Metal", "Can", "Parcel_Place");

        public static void ClearCache() => _cache.Clear();
    }
}
