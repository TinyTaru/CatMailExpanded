using System.Linq;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace CatMailExpanded.Content
{
    /// <summary>Adds settings entries for the new constraint values so the game can roll them for new parcels.</summary>
    internal static class ConstraintRegistration
    {
        public static void EnsureRegistered(EntityPropertiesManager epm)
        {
            var current = epm.Constraints;
            var list = current != null ? current.ToList() : new System.Collections.Generic.List<ConstraintSettings>();
            bool added = false;
            foreach (var value in ParcelKinds.All)
            {
                if (list.Any(s => s != null && s.Constraint == value)) continue;
                var s = new ConstraintSettings();
                s.Constraint = value;
                list.Add(s);
                added = true;
            }
            if (added)
            {
                epm.Constraints = new Il2CppReferenceArray<ConstraintSettings>(list.ToArray());
                Plugin.Log.Msg($"Registered new parcel constraint settings ({list.Count} total)");
            }
            GiftRoom.ExtendRibbons(epm);
            ModStamps.Attach(epm);
        }

        /// <summary>
        /// Our settings carry no StampData (the stickers are drawn by ParcelTracker). Game code that walks the
        /// settings to build sticker buttons or stamp lists would hit those nulls, so for the duration of such
        /// calls swap in an array without them. Returns the original array to hand back to Restore.
        /// </summary>
        public static ConstraintSettings[] HideStampless(EntityPropertiesManager epm)
        {
            if (epm == null) return null;
            var current = epm.Constraints;
            if (current == null || !current.Any(s => s != null && s.Stamp == null)) return null;
            epm.Constraints = new Il2CppReferenceArray<ConstraintSettings>(current.Where(s => s != null && s.Stamp != null).ToArray());
            return current;
        }

        public static void Restore(EntityPropertiesManager epm, ConstraintSettings[] original)
        {
            if (epm != null && original != null) epm.Constraints = new Il2CppReferenceArray<ConstraintSettings>(original);
        }
    }
}
