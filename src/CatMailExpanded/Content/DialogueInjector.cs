using System.Linq;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using Random = UnityEngine.Random;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Customers requesting a Scented/Restless parcel describe it with a type-specific line.
    /// Because the type is a StorageConstraint value, the game already inserted a generic
    /// "storage" hint line; we swap that for one of ours (or insert one if it is missing).
    /// </summary>
    internal static class DialogueInjector
    {
        public static void Apply(EntityParcel parcel, DialogueMood mood, List<DialogueData> lines)
        {
            if (parcel == null || lines == null) return;
            var kind = ParcelKinds.KindOf(parcel.Properties.StorageConstraint);
            if (kind == ModKind.None) return;
            Localization.Inject();

            var all = Localization.DialogueLines.Where(l => l.Kind == kind).ToList();
            var moodLines = all.Where(l => l.Mood == mood && mood != DialogueMood.Neutral).ToList();
            var pool = moodLines.Count > 0 && Random.value < 0.6f ? moodLines : all.Where(l => l.Mood == DialogueMood.Neutral).ToList();
            if (pool.Count == 0) pool = all;
            var pick = pool[Random.Range(0, pool.Count)];
            var data = new DialogueData(pick.Key, DialogueType.StorageConstraint, DialogueAge.Any, DialogueMood.Neutral, DialogueCategory.None);

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null && lines[i].Type == DialogueType.StorageConstraint)
                {
                    lines[i] = data;
                    return;
                }
            }
            int insertAt = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var t = lines[i]?.Type;
                if (t == DialogueType.Hello || t == DialogueType.PersonName) insertAt = i + 1;
            }
            lines.Insert(insertAt, data);
        }
    }
}
