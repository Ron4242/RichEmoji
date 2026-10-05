using System.Collections;
using GUIFramework;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace RichEmoji;

public static class Patches
{
    private static void ToShortcodesInPlace(GuiInputField field)
    {
        if (!field) return;
        var canonical = EmojiConverter.ToShortcodes(field.text);
        if (canonical != field.text) field.SetTextWithoutNotify(canonical);
    }

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
            var autocomplete = __instance.gameObject.AddComponent<EmojiAutocomplete>();
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
                    if (newText != captured)
                    {
                        // keep the caret where it was relative to the text around it
                        var caret = Mathf.Clamp(__instance.stringPosition, 0, captured.Length);
                        var newCaret = EmojiConverter.ToFakeUnicode(captured.Substring(0, caret)).Length;

                        __instance.SetTextWithoutNotify(newText);
                        __instance.stringPosition = newCaret;
                    }

                    autocomplete.OnTextChanged(__instance.text);
                }
            });
        }
    }

    // let the autocomplete popup take navigation/confirm keys before the input field acts on them
    [HarmonyPatch(typeof(TMP_InputField), "KeyPressed")]
    public static class TMPInputFieldKeyPressedPatch
    {
        private static bool Prefix(TMP_InputField __instance, Event evt, ref TMP_InputField.EditState __result)
        {
            if (!__instance.TryGetComponent<EmojiAutocomplete>(out var autocomplete) || !autocomplete.HandleKey(evt))
                return true;

            __result = TMP_InputField.EditState.Continue;
            return false;
        }
    }

    // stop chat/console history and tab-complete from also reacting while the popup is using those keys
    [HarmonyPatch(typeof(Terminal), "UpdateInput")]
    public static class TerminalUpdateInputPatch
    {
        private static bool Prefix(Terminal __instance)
        {
            return !__instance.m_input ||
                   !__instance.m_input.TryGetComponent<EmojiAutocomplete>(out var autocomplete) ||
                   !autocomplete.BlocksGameInput;
        }
    }

    // Tab is also the inventory key; signs etc. don't block it like chat does, so eat it while the popup uses it
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    public static class InventoryGuiUpdatePatch
    {
        private static void Prefix()
        {
            if (EmojiAutocomplete.AnyBlocksGameInput)
                ZInput.ResetButtonStatus("Inventory");
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

    // fake unicode is only valid for this client's current emoji set, so it must never be saved or sent.
    // these patch the places the game reads input text for storage, since not all of them go through submit
    // (e.g. the OK button on the sign/rename/portal dialog calls TextInput.OnEnter directly)

    // signs, tame names, portal tags
    [HarmonyPatch(typeof(TextInput), "setText")]
    public static class TextInputSetTextPatch
    {
        private static void Prefix(ref string text)
        {
            text = EmojiConverter.ToShortcodes(text);
        }
    }

    // map pin names
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnPinTextEntered))]
    public static class MinimapPinTextPatch
    {
        private static void Prefix(Minimap __instance)
        {
            ToShortcodesInPlace(__instance.m_nameInput);
        }
    }

    // chat and console
    [HarmonyPatch(typeof(Terminal), "SendInput")]
    public static class TerminalSendInputPatch
    {
        private static void Prefix(Terminal __instance)
        {
            ToShortcodesInPlace(__instance.m_input);
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