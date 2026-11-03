namespace CatMailExpanded.Content
{
    /// <summary>Builds the mod's world additions once per session, before saved parcels are restored.</summary>
    internal static class WorldSetup
    {
        public static void EnsureBuilt()
        {
            Outdoors.Build();
            Yard.Build();
            SpeakerRoom.BuildStores();
        }
    }
}
