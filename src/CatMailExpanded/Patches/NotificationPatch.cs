using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;

namespace CatMailExpanded.Patches
{
    /// <summary>Toast text is sized for the game's short messages; longer (and translated) ones run off the
    /// paper. Let TextMeshPro shrink the text until it fits, never growing it past the prefab's size.</summary>
    [HarmonyPatch(typeof(NotificationItemInterface), nameof(NotificationItemInterface.SetText))]
    static class NotificationItemInterface_SetText
    {
        const float MinScale = 0.5f;

        static void Postfix(NotificationItemInterface __instance)
        {
            try
            {
                TMP_Text text = __instance.LinkedText;
                if (text == null) return;
                if (!text.enableAutoSizing)
                {
                    float size = text.fontSize;
                    text.fontSizeMax = size;
                    text.fontSizeMin = size * MinScale;
                    text.enableAutoSizing = true;
                }
                text.ForceMeshUpdate();
            }
            catch (Exception e) { Plugin.Log.Warning("Toast autosize failed: " + e.Message); }
        }
    }
}
