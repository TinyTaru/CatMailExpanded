using System;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Tracks when the new content unlocks for the current save:
    ///  - Outdoor yard + scented parcels: once the post office reaches <see cref="Plugin.YardUnlockLevel"/>.
    ///  - Speaker room + restless parcels: only after the game's ending (the Old Postman scene) is finished.
    /// State lives in the game's own save (GameProgression table) under CME_* keys.
    /// </summary>
    internal static class ModProgress
    {
        const string KeyYardAnnounced = "CME_YardAnnounced";
        const string KeySpeakerAnnounced = "CME_SpeakerRoomAnnounced";
        const string KeyPlanetariumAnnounced = "CME_PlanetariumAnnounced";
        const string KeyFinishedEnding = "FinishedEnding";

        public static bool YardUnlocked { get; private set; }
        public static bool SpeakerRoomUnlocked { get; private set; }
        public static bool PlanetariumUnlocked { get; private set; }
        public static bool GameStarted { get; private set; }

        /// <summary>
        /// Set when the ending cinematic completes this session. The game only writes "FinishedEnding" to the
        /// save file on the next save, so reading it back immediately would still return false.
        /// </summary>
        public static bool EndingSeenThisSession { get; set; }
        static readonly System.Collections.Generic.HashSet<string> _announced = new();

        public static event Action Changed;

        static bool IsServer => NetworkManager.Instance == null || NetworkManager.Instance.IsServer;

        public static void Reset()
        {
            YardUnlocked = false;
            SpeakerRoomUnlocked = false;
            PlanetariumUnlocked = false;
            GameStarted = false;
            EndingSeenThisSession = false;
            _announced.Clear();
        }

        /// <summary>Called after the game replays its own progression for a loaded/new save.</summary>
        public static void OnGameStarted()
        {
            GameStarted = true;
            if (Plugin.PrepareUninstall.Value) { UninstallHelper.ConvertAll(); return; }
            WorldSetup.EnsureBuilt();
            Yard.SetOpen(false, animate: false);
            Planetarium.SetOpen(false, animate: false);
            Refresh(fromSave: true);
            SpeakerRoom.ApplyState();
        }

        public static void Refresh(bool fromSave)
        {
            if (Plugin.PrepareUninstall.Value) return;
            var save = SaveManager.Instance;
            var prog = ProgressionManager.Instance;
            if (save == null || prog == null) return;

            bool ending = EndingSeenThisSession || save.GetBool(TableType.GameProgression, KeyFinishedEnding, false, null) || Plugin.ForceUnlockAll.Value;
            bool yard = prog.CurrentLevel >= Plugin.YardUnlockLevel.Value || ending || Plugin.ForceUnlockAll.Value;
            bool planetarium = prog.CurrentLevel >= Plugin.PlanetariumUnlockLevel.Value || ending || Plugin.ForceUnlockAll.Value;

            bool changed = false;
            WorldSetup.EnsureBuilt();
            if (yard && !YardUnlocked)
            {
                YardUnlocked = true;
                changed = true;
                Yard.SetOpen(true, animate: !fromSave);
                UnlockConstraints(ParcelKinds.Scented);
                Announce(KeyYardAnnounced, Localization.Text("CME/Unlock_Yard"), fromSave, "sticker_strawberry");
            }
            if (planetarium && !PlanetariumUnlocked)
            {
                PlanetariumUnlocked = true;
                changed = true;
                Planetarium.SetOpen(true, animate: !fromSave);
                UnlockConstraints(new[] { ParcelKinds.Otherworldly });
                Announce(KeyPlanetariumAnnounced, Localization.Text("CME/Unlock_Planetarium"), fromSave, "sticker_otherworldly");
            }
            if (ending && !SpeakerRoomUnlocked)
            {
                SpeakerRoomUnlocked = true;
                changed = true;
                UnlockConstraints(new[] { ParcelKinds.Restless });
                SpeakerRoom.Build();
                Announce(KeySpeakerAnnounced, Localization.Text("CME/Unlock_SpeakerRoom"), fromSave, "sticker_restless");
            }
            if (changed)
            {
                Plugin.Log.Msg($"Progress: yard={YardUnlocked} planetarium={PlanetariumUnlocked} speakerRoom={SpeakerRoomUnlocked} (level {prog.CurrentLevel}, ending {ending})");
                Changed?.Invoke();
            }
        }

        static void UnlockConstraints(StorageConstraint[] values)
        {
            var epm = EntityPropertiesManager.Instance;
            if (epm == null) return;
            ConstraintRegistration.EnsureRegistered(epm);
            epm.UnlockStorageConstraints(new Il2CppStructArray<StorageConstraint>(values));
        }

        /// <summary>
        /// Shows a notification once per save (remembered in the save) and, for unlocks earned during play,
        /// also queues the game's own end-of-shift "unlocked" card.
        /// </summary>
        static void Announce(string key, string text, bool fromSave, string iconTexture)
        {
            var save = SaveManager.Instance;
            if (_announced.Contains(key) || save.GetBool(TableType.GameProgression, key, false, null)) return;
            _announced.Add(key);
            if (IsServer) save.SetBool(TableType.GameProgression, key, true);
            try
            {
                InterfaceManager.Instance?.NotificationInterface?.ShowNotification(text);
            }
            catch (Exception e) { Plugin.Log.Warning("Notification failed: " + e.Message); }
            if (fromSave) return;
            try
            {
                var tex = Core.ModAssets.Texture(iconTexture);
                var sprite = UnityEngine.Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new UnityEngine.Vector2(0.5f, 0.5f), 100f);
                var data = new UnlockData(sprite, text);
                InterfaceManager.Instance?.RecapScreenInterface?.QueueUnlockData(new Il2CppReferenceArray<UnlockData>(new[] { data }));
            }
            catch (Exception e) { Plugin.Log.Warning("Unlock card failed: " + e.Message); }
        }
    }
}
