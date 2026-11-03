using System;
using CatMailExpanded.Content;
using CatMailExpanded.Core;
using MelonLoader;

[assembly: MelonInfo(typeof(CatMailExpanded.Plugin), "Cat Mail Expanded", "1.0.0", "Cat Mail Expanded")]
[assembly: MelonGame("Maracas Studio", "CatMailCo")]

namespace CatMailExpanded
{
    public class Plugin : MelonMod
    {
        internal static Plugin Instance;
        internal static MelonLogger.Instance Log => Instance.LoggerInstance;

        internal static MelonPreferences_Entry<bool> DevMode;
        internal static MelonPreferences_Entry<int> YardUnlockLevel;
        internal static MelonPreferences_Entry<int> PlanetariumUnlockLevel;
        internal static MelonPreferences_Entry<bool> ForceUnlockAll;
        internal static MelonPreferences_Entry<float> RestlessMinInterval;
        internal static MelonPreferences_Entry<float> RestlessMaxInterval;
        internal static MelonPreferences_Entry<bool> PrepareUninstall;

        bool _inPostOffice;

        public override void OnInitializeMelon()
        {
            Instance = this;
            var cat = MelonPreferences.CreateCategory("CatMailExpanded", "Cat Mail Expanded");
            YardUnlockLevel = cat.CreateEntry("YardUnlockLevel", 10, "Yard unlock level",
                "Post office level at which the outdoor yard opens and scented parcels start arriving (the game has 15 levels).");
            PlanetariumUnlockLevel = cat.CreateEntry("PlanetariumUnlockLevel", 12, "Planetarium unlock level",
                "Post office level at which the planetarium opens and otherworldly parcels start arriving.");
            RestlessMinInterval = cat.CreateEntry("RestlessMinInterval", 7f, "Restless hop min seconds");
            RestlessMaxInterval = cat.CreateEntry("RestlessMaxInterval", 14f, "Restless hop max seconds");
            PrepareUninstall = cat.CreateEntry("PrepareUninstall", false, "Prepare for uninstall",
                "When true, every scented/restless parcel is turned back into a normal parcel when a save loads. Save once, then the mod can be removed safely.");
            ForceUnlockAll = cat.CreateEntry("ForceUnlockAll", false, "Force unlock everything",
                "Testing aid: unlock the yard, speaker room and both parcel types regardless of progress.");
            DevMode = cat.CreateEntry("DevMode", false, "Developer mode",
                "Enables the developer command file UserData/CME/cmd.txt.");
            if (DevMode.Value) Dev.DevConsole.Init();
            LoggerInstance.Msg("Cat Mail Expanded loaded");
        }

        public override void OnUpdate()
        {
            if (DevMode.Value) Dev.DevConsole.Update();
            Scheduler.Update();
            if (!_inPostOffice) return;
            try
            {
                ParcelTracker.Update();
                Yard.Update();
                Planetarium.Update();
                MirrorRoom.Update();
                GiftRoom.Update();
                Spectral.Update();
                RecapRow.Update();
                FrontPath.Update();
                OutdoorAtmosphere.Update();
                SpeakerRoom.Update();
            }
            catch (Exception e) { LoggerInstance.Error("Update failed: " + e); }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName}");
            if (sceneName == "sc_PostOffice")
            {
                _inPostOffice = true;
                Localization.Inject();
            }
            else if (sceneName == "sc_MainMenu" || sceneName == "sc_Bootstrap")
            {
                _inPostOffice = false;
                ResetSession();
            }
        }

        static void ResetSession()
        {
            OutdoorAtmosphere.Reset();
            ParcelTracker.Clear();
            ModProgress.Reset();
            SpeakerRoom.Reset();
            Yard.Reset();
            Planetarium.Reset();
            PileWing.Reset();
            PileExtension.Reset();
            MirrorRoom.Reset();
            GiftRoom.Reset();
            RecapRow.Reset();
            FrontPath.Reset();
            Outdoors.Reset();
            Patches.EntityScanner_ScanParcel.Reset();
            Scheduler.Clear();
            SoundFx.ClearCache();
            ModAssets.ClearSceneCaches();
            Localization.ResetInjection();
        }
    }
}
