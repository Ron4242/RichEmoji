using System.Collections;
using GUIFramework;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RichEmoji;

public static class Patches
{
    // patch activate so input fields that contain shortcodes get converted to fake Unicode for better editing when opening them
    [HarmonyPatch(typeof(GuiInputField), "ActivateInputField")]
    public static class GuiInputFieldActivatePatch
    {
        private static void Postfix(GuiInputField __instance)
        {
            if (string.IsNullOrEmpty(__instance.text)) return;

            var converted = EmojiConverter.ToFakeUnicode(__instance.text);
            if (converted == __instance.text) return;

            __instance.SetTextWithoutNotify(converted);
            __instance.m_OriginalText = converted;
        }
    }

    // patch awake to start listening for text changed and replace captures with fake Unicode for better editing
    [HarmonyPatch(typeof(GuiInputField), "Awake")]
    public static class GuiInputFieldAwakePatch
    {
        private static void Postfix(GuiInputField __instance)
        {
            Coroutine pending = null;

            __instance.onValueChanged.AddListener(text =>
            {
                if (pending != null)
                    __instance.StopCoroutine(pending);

                var captured = text;
                pending = __instance.StartCoroutine(Routine());

                IEnumerator Routine()
                {
                    yield return null;
                    pending = null;
                    var newText = EmojiConverter.ToFakeUnicode(captured);
                    if (newText != captured) __instance.SetTextWithoutNotify(newText);
                }
            });
        }
    }

    // patch submit to replace with more universal shortcodes
    [HarmonyPatch(typeof(GuiInputField), "onInputSubmit")]
    public static class GuiInputFieldSubmitPatch
    {
        private static void Prefix(GuiInputField __instance, ref string text)
        {
            var canonical = EmojiConverter.ToShortcodes(text);
            if (canonical == text) return;
            __instance.SetTextWithoutNotify(canonical);
            text = canonical;
        }
    }

    // patch generic TMP text object to actually replace shortcodes with the matching client emojis
    [HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
    public static class TMPTextAwakePatch
    {
        private static void Postfix(TextMeshProUGUI __instance)
        {
            __instance.textPreprocessor ??= EmojiConverter.EmojiTextPreprocessor.Instance;
        }
    }
}