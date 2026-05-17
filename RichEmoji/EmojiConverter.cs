using System.Text;
using System.Text.RegularExpressions;
using TMPro;

namespace RichEmoji;

public static class EmojiConverter
{
    private const string BaseEmoji =
        "(?:" +
        @"(?:\uD83C[\uDDE6-\uDDFF]){2}|" +
        @"[\uD83C-\uD83F][\uDC00-\uDFFF]|" +
        @"[\u2000-\u3299]|" +
        @"[\u00A9\u00AE]|" +
        @"[0-9#*]\uFE0F?\u20E3" +
        ")" +
        @"(?:\uD83C[\uDFFB-\uDFFF])?\uFE0F?";

    public static readonly Regex EmojiNamePattern = new(":([a-zA-Z0-9_]+):", RegexOptions.Compiled);

    public static readonly Regex EmojiUnicodePattern = new(
        BaseEmoji + @"(?:\u200D" + BaseEmoji + ")*",
        RegexOptions.Compiled
    );

    // converts shortcodes and real Unicode emoji sequences to our fake unicode
    public static string ToFakeUnicode(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string result = text;

        if (result.Contains(":"))
        {
            result = EmojiNamePattern.Replace(result, match =>
                RichEmoji.EmojiNameLookup.TryGetValue(match.Groups[1].Value, out string unicodeStr)
                    ? unicodeStr
                    : match.Value);
        }

        result = EmojiUnicodePattern.Replace(result, match =>
            RichEmoji.EmojiUnicodeLookup.TryGetValue(match.Value, out string fakeUnicode)
                ? fakeUnicode
                : match.Value);

        return result;
    }

    // convert fake unicode to shortcodes
    public static string ToShortcodes(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        StringBuilder sb = new();
        foreach (char c in text)
        {
            if (RichEmoji.EmojiFakeUnicodeLookup.TryGetValue(c, out string shortcode))
                sb.Append(shortcode);
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    public class EmojiTextPreprocessor : ITextPreprocessor
    {
        public static readonly EmojiTextPreprocessor Instance = new();

        public string PreprocessText(string text) => ToFakeUnicode(text);
    }
}