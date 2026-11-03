using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    internal enum ModKind { None, Strawberry, Banana, Restless }

    /// <summary>
    /// The new parcel types are stored as new raw values of the game's StorageConstraint field, so they
    /// save, load, sync over the network and flow through customer requests exactly like Cold/Hot parcels.
    /// </summary>
    internal static class ParcelKinds
    {
        public const StorageConstraint Strawberry = (StorageConstraint)0x20;
        public const StorageConstraint Banana = (StorageConstraint)0x40;
        public const StorageConstraint Restless = (StorageConstraint)0x80;

        public static readonly StorageConstraint[] Scented = { Strawberry, Banana };
        public static readonly StorageConstraint[] All = { Strawberry, Banana, Restless };

        public static bool IsModValue(StorageConstraint c) => c == Strawberry || c == Banana || c == Restless;

        public static ModKind KindOf(StorageConstraint c) => c switch
        {
            Strawberry => ModKind.Strawberry,
            Banana => ModKind.Banana,
            Restless => ModKind.Restless,
            _ => ModKind.None,
        };

        public static bool IsScented(ModKind k) => k == ModKind.Strawberry || k == ModKind.Banana;

        public static Color FogColor(ModKind k) => k switch
        {
            ModKind.Strawberry => new Color(0.98f, 0.56f, 0.67f),
            ModKind.Banana => new Color(1.00f, 0.87f, 0.40f),
            _ => Color.white,
        };

        public static string StickerTexture(ModKind k) => k switch
        {
            ModKind.Strawberry => "sticker_strawberry",
            ModKind.Banana => "sticker_banana",
            ModKind.Restless => "sticker_restless",
            _ => null,
        };
    }
}
