using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    internal enum ModKind { None, Strawberry, Banana, Restless, Otherworldly, Spectral, Return, ReturnWrapped, Gift, GiftWrapped }

    /// <summary>
    /// The new parcel types are stored as new raw values of the game's StorageConstraint field, so they
    /// save, load, sync over the network and flow through customer requests exactly like Cold/Hot parcels.
    /// </summary>
    internal static class ParcelKinds
    {
        public const StorageConstraint Strawberry = (StorageConstraint)0x20;
        public const StorageConstraint Banana = (StorageConstraint)0x40;
        public const StorageConstraint Restless = (StorageConstraint)0x80;
        public const StorageConstraint Otherworldly = (StorageConstraint)0x100;
        public const StorageConstraint Spectral = (StorageConstraint)0x200;
        // Legacy: Catazon return items from an earlier build. Kept only so old saves load; GiftRoom turns them
        // back into ordinary parcels.
        public const StorageConstraint Return = (StorageConstraint)0x400;
        public const StorageConstraint ReturnWrapped = (StorageConstraint)0x800;
        // Gifts (GiftRoom): a gift waiting to be wrapped, and one value per wrapping paper (a bit each, above the
        // native flags). Like the legacy values they have settings entries but are never unlocked, so never rolled.
        public const StorageConstraint Gift = (StorageConstraint)0x1000;
        public const int PaperCount = 8;
        public static StorageConstraint GiftPaper(int paper) => (StorageConstraint)(0x2000 << paper);

        public static readonly StorageConstraint[] Scented = { Strawberry, Banana };
        public static readonly StorageConstraint[] All =
        {
            Strawberry, Banana, Restless, Otherworldly, Spectral, Return, ReturnWrapped, Gift,
            GiftPaper(0), GiftPaper(1), GiftPaper(2), GiftPaper(3), GiftPaper(4), GiftPaper(5), GiftPaper(6), GiftPaper(7),
        };
        /// <summary>The kinds that arrive as ordinary parcels (pile, boat, customers), once unlocked.</summary>
        public static readonly StorageConstraint[] Mail = { Strawberry, Banana, Restless, Otherworldly, Spectral };

        public static bool IsModValue(StorageConstraint c) => KindOf(c) != ModKind.None;
        public static bool IsReturn(StorageConstraint c) => c == Return || c == ReturnWrapped;
        public static bool IsReturn(ModKind k) => k == ModKind.Return || k == ModKind.ReturnWrapped;
        public static bool IsGift(StorageConstraint c) => c == Gift || PaperOf(c) >= 0;
        public static bool IsGift(ModKind k) => k == ModKind.Gift || k == ModKind.GiftWrapped;

        /// <summary>The wrapping paper of a wrapped gift, -1 for anything else.</summary>
        public static int PaperOf(StorageConstraint c)
        {
            int v = (int)c;
            for (int i = 0; i < PaperCount; i++) if (v == 0x2000 << i) return i;
            return -1;
        }

        public static ModKind KindOf(StorageConstraint c) => c switch
        {
            Strawberry => ModKind.Strawberry,
            Banana => ModKind.Banana,
            Restless => ModKind.Restless,
            Otherworldly => ModKind.Otherworldly,
            Spectral => ModKind.Spectral,
            Return => ModKind.Return,
            ReturnWrapped => ModKind.ReturnWrapped,
            Gift => ModKind.Gift,
            _ => PaperOf(c) >= 0 ? ModKind.GiftWrapped : ModKind.None,
        };

        public static bool IsScented(ModKind k) => k == ModKind.Strawberry || k == ModKind.Banana;

        public static StorageConstraint ValueOf(ModKind k) => k switch
        {
            ModKind.Strawberry => Strawberry,
            ModKind.Banana => Banana,
            ModKind.Restless => Restless,
            ModKind.Otherworldly => Otherworldly,
            ModKind.Spectral => Spectral,
            ModKind.Return => Return,
            ModKind.ReturnWrapped => ReturnWrapped,
            ModKind.Gift => Gift,
            ModKind.GiftWrapped => GiftPaper(0),
            _ => StorageConstraint.None,
        };

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
            ModKind.Otherworldly => "sticker_otherworldly",
            ModKind.Spectral => "sticker_spectral",
            _ => null,
        };
    }
}
