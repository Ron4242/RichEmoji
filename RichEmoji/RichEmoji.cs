using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace RichEmoji;

[BepInPlugin(ThisPluginInfo.PLUGIN_GUID, ThisPluginInfo.PLUGIN_NAME, ThisPluginInfo.PLUGIN_VERSION)]
public sealed class RichEmoji : BaseUnityPlugin
{
    // since our PUA only spans 6400 characters, we'll limit to 6400 emojis
    // we use the U+E000-U+F8FF PUA
    // nobody sane would exceed this?.
    private const int MaxEmojis = 6400;
    private const int EmojiMaxWidth = 64;
    private const int EmojiMaxHeight = 64;
    public static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(ThisPluginInfo.PLUGIN_NAME);
    public static TMP_SpriteAsset CustomEmojiAsset;

    private static readonly List<(Texture2D texture, string emojiName, string unicode)> PendingEmojis = new();
    public static readonly Dictionary<string, string> EmojiNameLookup = new();
    public static readonly Dictionary<char, string> EmojiFakeUnicodeLookup = new();
    public static readonly Dictionary<string, string> EmojiUnicodeLookup = new();

    // LoadImage fix from Jotunn AssetUtils
    private static MethodInfo LoadImageMethod { get; } = AccessTools.Method(typeof(ImageConversion),
        nameof(ImageConversion.LoadImage), [typeof(Texture2D), typeof(byte[])]);

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(Patches).Assembly);
        LoadEmojis();
    }

    private void LoadEmojis()
    {
        var pluginFolder = Path.GetDirectoryName(Info.Location);
        var baseEmojiFolder = Path.Combine(pluginFolder, "emojis");
        var configFolder = Path.Combine(
            Paths.ConfigPath,
            ThisPluginInfo.PLUGIN_NAME,
            "emojis"
        );
        Directory.CreateDirectory(configFolder);

        AddEmojis(baseEmojiFolder);
        AddEmojis(configFolder);

        BuildEmojis();
    }

    public static void AddEmojis(string folder)
    {
        if (!Directory.Exists(folder))
            return;

        var files = Directory.GetFiles(
            folder,
            "*.png",
            SearchOption.AllDirectories
        );

        if (files.Length == 0)
            return;

        var bytes = new byte[files.Length][];
        Parallel.For(0, files.Length, i => bytes[i] = File.ReadAllBytes(files[i]));

        for (var i = 0; i < files.Length; i++)
        {
            var tex = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );
            LoadImageMethod.Invoke(null, [tex, bytes[i]]);

            var fileName = Path.GetFileNameWithoutExtension(files[i]);
            var parts = fileName.Split(["__"], StringSplitOptions.None);
            var shortName = parts[0];
            var unicode = parts.Length > 1 ? parts[1] : null;

            AddEmoji(tex, shortName, unicode);
        }
    }

    public static void AddEmoji(Texture2D source, string emojiName, string unicode = null)
    {
        var resized = ResizeTexture(source, EmojiMaxWidth, EmojiMaxHeight);
        PendingEmojis.Add((resized, emojiName, unicode));
    }

    public static void BuildEmojis()
    {
        if (PendingEmojis.Count == 0)
            return;

        if (CustomEmojiAsset)
        {
            var defaultAsset = TMP_Settings.defaultSpriteAsset;

            defaultAsset.fallbackSpriteAssets?.Remove(CustomEmojiAsset);

            if (CustomEmojiAsset.material)
                Destroy(CustomEmojiAsset.material);

            if (CustomEmojiAsset.spriteSheet)
            {
                Destroy(CustomEmojiAsset.spriteSheet);

                Destroy(CustomEmojiAsset);

                EmojiNameLookup.Clear();
                EmojiFakeUnicodeLookup.Clear();
                EmojiUnicodeLookup.Clear();
            }
        }

        if (PendingEmojis.Count > MaxEmojis)
            Log.LogWarning(
                $"Emoji limit exceeded! Found {PendingEmojis.Count}, but only {MaxEmojis} are supported. Not all emojis will be loaded.");

        var total = Math.Min(PendingEmojis.Count, MaxEmojis);

        List<Texture2D> textures = new(total);
        for (var i = 0; i < total; i++)
            textures.Add(PendingEmojis[i].texture);

        var atlas = new Texture2D(
            2,
            2,
            TextureFormat.RGBA32,
            false
        );
        var rects = atlas.PackTextures(
            textures.ToArray(),
            2,
            8192,
            true
        );

        // don't need these anymore
        foreach (var tex in textures)
            Destroy(tex);

        CustomEmojiAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        CustomEmojiAsset.name = "RichEmojiAtlas";
        CustomEmojiAsset.spriteSheet = atlas;
        CustomEmojiAsset.m_Version = "1.1.0"; // if we don't set this, TMP gets angry and tries to upgrade it

        var defaultSprite = TMP_Settings.defaultSpriteAsset;

        CustomEmojiAsset.material = new Material(defaultSprite.material)
        {
            mainTexture = atlas
        };
        CustomEmojiAsset.spriteCharacterTable = [];
        CustomEmojiAsset.spriteGlyphTable = [];

        for (var i = 0; i < total; i++)
        {
            var (texture, emojiName, unicodeSequence) = PendingEmojis[i];

            var r = rects[i];

            var x = r.x * atlas.width;
            var y = r.y * atlas.height;
            var w = r.width * atlas.width;
            var h = r.height * atlas.height;

            var glyph = new TMP_SpriteGlyph
            {
                index = (uint)i,
                metrics = new GlyphMetrics(
                    w,
                    h,
                    0.0f,
                    h,
                    w
                ),
                glyphRect = new GlyphRect(
                    (int)x,
                    (int)y,
                    (int)w,
                    (int)h
                ),
                scale = 1f
            };
            CustomEmojiAsset.spriteGlyphTable.Add(glyph);

            var fakeUnicode = (uint)(0xE000 + i);
            var fakeChar = char.ConvertFromUtf32((int)fakeUnicode);

            // TMP doesn't support multiple codepoints... but we want them!
            // since we know the real sequence, we can create a mapping for real:fake and control the glyph displayed.
            if (!string.IsNullOrWhiteSpace(unicodeSequence))
            {
                StringBuilder sequence = new();
                var codepoints = unicodeSequence.Split('-');
                var valid = true;

                for (var cp = 0; i < codepoints.Length; i++)
                    if (uint.TryParse(codepoints[cp], NumberStyles.HexNumber, null, out var parsed))
                    {
                        sequence.Append(char.ConvertFromUtf32((int)parsed));
                    }
                    else
                    {
                        valid = false;
                        break;
                    }

                if (valid && sequence.Length > 0)
                    EmojiUnicodeLookup[sequence.ToString()] = fakeChar;
            }

            var character = new TMP_SpriteCharacter(fakeUnicode, glyph) { name = emojiName };

            CustomEmojiAsset.spriteCharacterTable.Add(character);

            EmojiNameLookup[emojiName] = fakeChar;
            EmojiFakeUnicodeLookup[fakeChar[0]] = $":{emojiName}:";
        }

        CustomEmojiAsset.UpdateLookupTables();
        defaultSprite.fallbackSpriteAssets ??= [];
        defaultSprite.fallbackSpriteAssets.Add(CustomEmojiAsset);

        Log.LogInfo($"Loaded {PendingEmojis.Count} emojis.");

        PendingEmojis.Clear();
    }

    private static Texture2D ResizeTexture(Texture2D source, int maxWidth, int maxHeight)
    {
        var targetWidth = source.width;
        var targetHeight = source.height;

        // preserve aspect
        if (targetWidth > maxWidth || targetHeight > maxHeight)
        {
            var aspect = (float)targetWidth / targetHeight;
            if (targetWidth > targetHeight)
            {
                targetWidth = maxWidth;
                targetHeight = Mathf.RoundToInt(targetWidth / aspect);
            }
            else
            {
                targetHeight = maxHeight;
                targetWidth = Mathf.RoundToInt(targetHeight * aspect);
            }
        }
        else
        {
            return source;
        }

        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}