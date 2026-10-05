using System;
using System.Collections.Generic;
using System.Linq;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RichEmoji;

public class EmojiAutocomplete : MonoBehaviour
{
    private const int MaxSuggestions = 8;
    private const int MinQueryLength = 2;
    private const float RowHeight = 28f;
    private const float HeaderHeight = 20f;
    private const float Padding = 10f;
    private const float PopupWidth = 280f;

    // the most recently opened popup, only one input field can be focused at a time
    private static EmojiAutocomplete _lastOpened;
    private readonly List<SuggestionRow> _rows = new();

    // frame a navigation key was consumed on, so Terminal doesn't also act on it (history, tab-complete)
    private int _consumedFrame = -1;

    private List<string> _currentMatches = new();

    private GuiInputField _field;

    // built lazily on first show, so input fields that never see a shortcode never get any extra UI
    private GameObject _popup;
    private RectTransform _rowContainer;
    private int _selectedIndex = -1;
    private int _triggerPos = -1;

    public bool IsOpen => _popup && _popup.activeSelf;
    public bool BlocksGameInput => IsOpen || _consumedFrame == Time.frameCount;
    public static bool AnyBlocksGameInput => _lastOpened && _lastOpened.BlocksGameInput;

    private TMP_FontAsset FieldFont => _field.textComponent ? _field.textComponent.font : TMP_Settings.defaultFontAsset;
    private TMP_FontAsset BodyFont => GameAssets.GetFont("Valheim-AveriaSerifLibre") ?? FieldFont;

    private TMP_FontAsset HeaderFont =>
        GameAssets.GetFont("Valheim-Norsebold") ?? GameAssets.GetFont("Valheim-Norse") ?? BodyFont;

    private void Awake()
    {
        _field = GetComponent<GuiInputField>();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (_popup)
            Destroy(_popup);
    }

    public void OnTextChanged(string text)
    {
        if (!RichEmoji.AutocompleteEnabled.Value || !_field || !_field.isFocused ||
            RichEmoji.EmojiNameLookup.Count == 0)
        {
            Hide();
            return;
        }

        var caret = Mathf.Clamp(_field.stringPosition, 0, text.Length);
        if (!TryGetPartialShortcode(text, caret, out var partial, out var triggerPos))
        {
            Hide();
            return;
        }

        var matches = RichEmoji.EmojiNameLookup.Keys
            .Where(k => k.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k.Length)
            .ThenBy(k => k)
            .Take(MaxSuggestions)
            .ToList();

        if (matches.Count == 0)
        {
            Hide();
            return;
        }

        _triggerPos = triggerPos;
        _currentMatches = matches;
        _selectedIndex = 0;

        // activate before populating so new rows' TMP components run Awake (and get a material) as they're created
        EnsurePopup();
        _popup.SetActive(true);
        _lastOpened = this;
        Populate(matches, partial);
    }

    // called from the TMP_InputField.KeyPressed prefix; returns true if the key was consumed
    public bool HandleKey(Event evt)
    {
        if (!IsOpen) return false;

        switch (evt.keyCode)
        {
            case KeyCode.DownArrow:
                MoveSelection(1);
                break;
            case KeyCode.UpArrow:
                MoveSelection(-1);
                break;
            case KeyCode.Tab:
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                Commit();
                break;
            case KeyCode.Escape:
                Hide();
                break;
            default:
                return false;
        }

        _consumedFrame = Time.frameCount;
        return true;
    }

    private void MoveSelection(int delta)
    {
        if (_currentMatches.Count == 0) return;
        _selectedIndex = (_selectedIndex + delta + _currentMatches.Count) % _currentMatches.Count;
        for (var i = 0; i < _rows.Count; i++)
            _rows[i].SetSelected(i == _selectedIndex);
    }

    private void Commit()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _currentMatches.Count || _triggerPos < 0)
        {
            Hide();
            return;
        }

        var chosen = _currentMatches[_selectedIndex];
        var text = _field.text;
        var caret = Mathf.Clamp(_field.stringPosition, 0, text.Length);

        if (_triggerPos < caret && RichEmoji.EmojiNameLookup.TryGetValue(chosen, out var fakeChar))
        {
            // insert the fake unicode directly, the same thing the shortcode would convert to
            var replacement = fakeChar + " ";
            var newText = text.Substring(0, _triggerPos) + replacement + text.Substring(caret);
            var newCaret = _triggerPos + replacement.Length;

            _field.SetTextWithoutNotify(newText);
            _field.stringPosition = newCaret;
            _field.onValueChanged.Invoke(newText);
        }

        Hide();
    }

    private void Hide()
    {
        if (_popup) _popup.SetActive(false);
        _currentMatches.Clear();
        _selectedIndex = -1;
        _triggerPos = -1;
    }

    private void Populate(List<string> names, string partial)
    {
        while (_rows.Count < names.Count)
            _rows.Add(CreateRow());

        for (var i = 0; i < _rows.Count; i++)
        {
            var active = i < names.Count;
            _rows[i].Root.SetActive(active);
            if (!active) continue;

            var name = names[i];
            _rows[i].SetContent(name, partial, RichEmoji.EmojiNameLookup.TryGetValue(name, out var fc) ? fc : null);
            _rows[i].SetSelected(i == _selectedIndex);
        }

        var rt = (RectTransform)_popup.transform;
        rt.sizeDelta = new Vector2(PopupWidth, names.Count * (RowHeight + 1f) + HeaderHeight + Padding * 2f);
    }

    private static void ApplySprite(Image image, string spriteName, Color fallback)
    {
        var sprite = GameAssets.GetSprite(spriteName);
        if (sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.color = fallback;
        }
    }

    private void EnsurePopup()
    {
        if (_popup) return;

        _popup = new GameObject("EmojiAutocomplete", typeof(RectTransform));
        _popup.SetActive(false);
        _popup.transform.SetParent(transform, false);

        // never let a layout group on the input field position us
        _popup.AddComponent<LayoutElement>().ignoreLayout = true;

        // own canvas so we draw over everything and escape any RectMask2D the input field sits in
        var canvas = _popup.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;

        var rt = (RectTransform)_popup.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(0f, 6f);

        // same wood panel the vanilla menus use
        var bg = _popup.AddComponent<Image>();
        ApplySprite(bg, "woodpanel_trophys", new Color(0.18f, 0.13f, 0.09f, 0.95f));
        bg.raycastTarget = false;

        var headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(_popup.transform, false);
        var headerRt = (RectTransform)headerObj.transform;
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0f, 1f);
        headerRt.offsetMin = new Vector2(Padding + 4f, -Padding - HeaderHeight);
        headerRt.offsetMax = new Vector2(-Padding, -Padding);
        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.font = HeaderFont;
        headerText.text = "Emoji";
        headerText.fontSize = 16f;
        headerText.color = GameAssets.ValheimOrange;
        headerText.alignment = TextAlignmentOptions.MidlineLeft;
        headerText.raycastTarget = false;

        var container = new GameObject("Rows", typeof(RectTransform));
        container.transform.SetParent(_popup.transform, false);
        _rowContainer = (RectTransform)container.transform;
        _rowContainer.anchorMin = Vector2.zero;
        _rowContainer.anchorMax = Vector2.one;
        _rowContainer.offsetMin = new Vector2(Padding, Padding);
        _rowContainer.offsetMax = new Vector2(-Padding, -Padding - HeaderHeight);
        var layout = container.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 1f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private SuggestionRow CreateRow()
    {
        var root = new GameObject("Row", typeof(RectTransform));
        root.transform.SetParent(_rowContainer, false);
        ((RectTransform)root.transform).sizeDelta = new Vector2(0f, RowHeight);

        // sunken slot background for contrast
        var bg = root.AddComponent<Image>();
        ApplySprite(bg, "item_background_sunken", new Color(0f, 0f, 0f, 0.7f));
        bg.raycastTarget = false;

        var previewObj = new GameObject("Preview", typeof(RectTransform));
        previewObj.transform.SetParent(root.transform, false);
        var previewRt = (RectTransform)previewObj.transform;
        previewRt.anchorMin = new Vector2(0f, 0f);
        previewRt.anchorMax = new Vector2(0f, 1f);
        previewRt.pivot = new Vector2(0f, 0.5f);
        previewRt.anchoredPosition = new Vector2(6f, 0f);
        previewRt.sizeDelta = new Vector2(22f, 0f);
        var previewText = previewObj.AddComponent<TextMeshProUGUI>();
        previewText.font = FieldFont;
        previewText.fontSize = 18f;
        previewText.alignment = TextAlignmentOptions.Midline;
        previewText.overflowMode = TextOverflowModes.Overflow;
        previewText.raycastTarget = false;

        var labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(root.transform, false);
        var labelRt = (RectTransform)labelObj.transform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(32f, 0f);
        labelRt.offsetMax = new Vector2(-6f, 0f);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.font = BodyFont;
        labelText.fontSize = 16f;
        labelText.color = GameAssets.ValheimBeige;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.raycastTarget = false;
        // the label shows the literal shortcode, so don't let our preprocessor turn it back into an emoji
        labelText.textPreprocessor = null;

        return new SuggestionRow(root, bg, previewText, labelText);
    }

    private static bool TryGetPartialShortcode(string text, int caretPos, out string partial, out int triggerPos)
    {
        partial = null;
        triggerPos = -1;
        const int maxLookback = 40;

        for (var i = caretPos - 1; i >= 0 && caretPos - i <= maxLookback; i--)
        {
            var c = text[i];
            if (c == ':')
            {
                var candidate = text.Substring(i + 1, caretPos - i - 1);
                if (candidate.Length < MinQueryLength) return false;
                partial = candidate;
                triggerPos = i;
                return true;
            }

            if (!IsShortcodeChar(c)) return false;
        }

        return false;
    }

    // keep in sync with EmojiConverter.EmojiNamePattern
    private static bool IsShortcodeChar(char c) =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_';

    private class SuggestionRow
    {
        private const float UnselectedBgAlpha = 0.6f;
        public readonly GameObject Root;

        private readonly Image _bg;
        private readonly Color _bgColor;
        private readonly TextMeshProUGUI _label;
        private readonly TextMeshProUGUI _preview;

        public SuggestionRow(GameObject root, Image bg, TextMeshProUGUI preview, TextMeshProUGUI label)
        {
            Root = root;
            _bg = bg;
            _bgColor = bg.color;
            _preview = preview;
            _label = label;
        }

        public void SetContent(string emojiName, string partial, string fakeChar)
        {
            _preview.text = fakeChar ?? "?";

            // matches are prefix matches, so the highlighted part is always the start
            var match = emojiName.Substring(0, Mathf.Min(partial.Length, emojiName.Length));
            var rest = emojiName.Substring(match.Length);
            _label.text = $":<color=#FFA13C>{match}</color>{rest}:";
        }

        public void SetSelected(bool selected)
        {
            var color = _bgColor;
            if (!selected) color.a *= UnselectedBgAlpha;
            _bg.color = color;
            _label.color = selected ? Color.white : GameAssets.ValheimBeige;
        }
    }
}