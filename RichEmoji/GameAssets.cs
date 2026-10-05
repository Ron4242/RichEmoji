using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace RichEmoji;

// looks up the game's own UI sprites/fonts by name, so our UI matches vanilla
public static class GameAssets
{
    public static readonly Color ValheimOrange = new(1f, 0.631f, 0.235f, 1f);
    public static readonly Color ValheimBeige = new(0.95f, 0.87f, 0.72f, 1f);

    private static readonly Dictionary<string, Sprite> Sprites = new();
    private static readonly Dictionary<string, TMP_FontAsset> Fonts = new();

    // only hits are cached, so asking before the game's assets are loaded doesn't stick
    public static Sprite GetSprite(string name)
    {
        if (Sprites.TryGetValue(name, out var cached) && cached)
            return cached;

        var sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == name);
        if (sprite)
            Sprites[name] = sprite;
        else
            RichEmoji.Log.LogDebug($"Game sprite '{name}' not found, using fallback");

        return sprite;
    }

    public static TMP_FontAsset GetFont(string name)
    {
        if (Fonts.TryGetValue(name, out var cached) && cached)
            return cached;

        var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == name);
        if (font)
            Fonts[name] = font;
        else
            RichEmoji.Log.LogDebug($"Game font '{name}' not found, using fallback");

        return font;
    }
}