using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;

namespace CatMailExpanded.Patches
{
    /// <summary>The toast's paper runs off the right edge of the screen, and so does its text box: the game's
    /// own short messages never reach that far, but longer (and translated) ones get cut off. Pull the text's
    /// right margin in to the visible screen edge so TextMeshPro's auto-sizing shrinks it to fit.</summary>
    [HarmonyPatch(typeof(NotificationItemInterface), nameof(NotificationItemInterface.SetText))]
    static class NotificationItemInterface_SetText
    {
        const float EdgePadding = 28f;   // canvas units kept clear of the screen edge
        const float MinScale = 0.5f;     // smallest allowed size, relative to the prefab's max size

        static void Postfix(NotificationItemInterface __instance)
        {
            try
            {
                TMP_Text text = __instance.LinkedText;
                var shown = InterfaceManager.Instance?.NotificationInterface?.ShownAnchor;
                if (text == null || shown == null) return;

                // The game moves the item's pivot onto ShownAnchor; find where the text's right edge lands then.
                RectTransform rt = text.rectTransform;
                float scale = rt.lossyScale.x;
                if (scale <= 0f) return;
                float rightNow = rt.TransformPoint(new Vector3(rt.rect.xMax, 0f, 0f)).x;
                float rightShown = rightNow - __instance.transform.position.x + shown.position.x;
                float overflow = (rightShown - Screen.width) / scale + EdgePadding;

                Vector4 m = text.margin;
                m.z = Mathf.Max(0f, overflow);
                text.margin = m;

                if (!text.enableAutoSizing)
                {
                    text.fontSizeMax = text.fontSize;
                    text.enableAutoSizing = true;
                }
                text.fontSizeMin = Mathf.Min(text.fontSizeMin, text.fontSizeMax * MinScale);
                text.ForceMeshUpdate();
            }
            catch (Exception e) { Plugin.Log.Warning("Toast text fit failed: " + e.Message); }
        }
    }
}
