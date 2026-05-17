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
    public const int MaxEmojis = 6400;
    public static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(ThisPluginInfo.PLUGIN_NAME);
    public static TMP_SpriteAsset CustomEmojiAsset;

    public static readonly Dictionary<string, string> EmojiNameLookup = new();
    public static readonly Dictionary<char, string> EmojiFakeUnicodeLookup = new();
    public static readonly Dictionary<string, string> EmojiUnicodeLookup = new();

    // LoadImage fix from Jotunn AssetUtils
    private static MethodInfo LoadImageMethod { get; } = AccessTools.Method(typeof(ImageConversion),
        nameof(ImageConversion.LoadImage), [typeof(Texture2D), typeof(byte[])]);

    void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(RichEmoji).Assembly);
        LoadEmojis();
    }

    void LoadEmojis()
    {
        string pluginFolder = Path.GetDirectoryName(Info.Location);
        string emojisFolder = Path.Combine(pluginFolder, "emojis");

        if (!Directory.Exists(emojisFolder))
        {
            Log.LogWarning($"Emojis folder not found at {emojisFolder}");
            return;
        }

        string[] files = Directory.GetFiles(emojisFolder, "*.png", SearchOption.AllDirectories);
        int totalFiles = files.Length;
        if (files.Length == 0)
            return; // got no mojis

        // loading hundreds of individual pngs is very, very IO bound, so we'll parallelize it
        byte[][] bytes = new byte[totalFiles][];
        Parallel.For(0, totalFiles, i => { bytes[i] = File.ReadAllBytes(files[i]); });

        List<Texture2D> individualTextures = new List<Texture2D>(totalFiles);
        ;
        List<string> fileNames = new List<string>(totalFiles);

        // collect textures (resize to maximum of 96x96 so we never overflow our atlas with the max emoji count)
        for (int i = 0; i < totalFiles; i++)
        {
            Texture2D rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            LoadImageMethod.Invoke(null, [rawTex, bytes[i]]);
            bytes[i] = null;

            Texture2D resizedTex = ResizeTexture(rawTex, 96, 96);
            individualTextures.Add(resizedTex);
            fileNames.Add(Path.GetFileNameWithoutExtension(files[i]));

            if (resizedTex != rawTex)
                Destroy(rawTex);
        }

        Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Rect[] rects = atlas.PackTextures(individualTextures.ToArray(), 2, 8192);

        // don't need these anymore
        foreach (var tex in individualTextures)
        {
            Destroy(tex);
        }

        individualTextures.Clear();

        CustomEmojiAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        CustomEmojiAsset.name = "RichEmojiAtlas";
        CustomEmojiAsset.spriteSheet = atlas;
        CustomEmojiAsset.m_Version = "1.1.0"; // if we don't set this, TMP gets angry and tries to upgrade it

        TMP_SpriteAsset defaultAsset = TMP_Settings.defaultSpriteAsset;
        CustomEmojiAsset.material = new Material(defaultAsset.material)
        {
            mainTexture = atlas
        };

        CustomEmojiAsset.spriteCharacterTable = [];
        CustomEmojiAsset.spriteGlyphTable = [];

        int limit = Math.Min(rects.Length, MaxEmojis);
        if (rects.Length > MaxEmojis)
        {
            Log.LogWarning(
                $"Emoji limit exceeded! Found {rects.Length}, but only up to {MaxEmojis} emojis are supported. Not all emojis will be loaded.");
        }

        // theoretically supports size emojis
        for (int i = 0; i < limit; i++)
        {
            Rect r = rects[i];
            string fileName = fileNames[i];

            // convert to pixels
            float x = r.x * atlas.width;
            float y = r.y * atlas.height;
            float w = r.width * atlas.width;
            float h = r.height * atlas.height;

            TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
            {
                index = (uint)i,
                metrics = new GlyphMetrics(w, h, 0, h, w), // width, height, bearingX, bearingY, advance
                glyphRect = new GlyphRect((int)x, (int)y, (int)w, (int)h),
                scale = 1.0f
            };
            CustomEmojiAsset.spriteGlyphTable.Add(glyph);

            string[] parts = fileName.Split(["__"], StringSplitOptions.None);
            string shortName = parts[0];
            StringBuilder sequence = new();
            uint unicode = (uint)(0xE000 + i);

            // TMP doesn't support multiple codepoints... but we want them!
            // since we know the real sequence, we can create a mapping for real:fake and control the glyph displayed.
            if (parts.Length > 1)
            {
                string[] codepoints = parts[1].Split('-');
                bool validSequence = true;

                foreach (string cp in codepoints)
                {
                    if (uint.TryParse(cp, NumberStyles.HexNumber, null, out uint cpUnicode))
                        sequence.Append(char.ConvertFromUtf32((int)cpUnicode));
                    else
                    {
                        validSequence = false;
                        break;
                    }
                }

                if (validSequence && sequence.Length > 0)
                    EmojiUnicodeLookup[sequence.ToString()] = char.ConvertFromUtf32((int)unicode);
            }

            TMP_SpriteCharacter character = new TMP_SpriteCharacter(unicode, glyph)
            {
                name = shortName
            };
            CustomEmojiAsset.spriteCharacterTable.Add(character);

            EmojiNameLookup[shortName] = char.ConvertFromUtf32((int)unicode);
            EmojiFakeUnicodeLookup[char.ConvertFromUtf32((int)unicode)[0]] = $":{shortName}:";
        }

        CustomEmojiAsset.UpdateLookupTables();

        defaultAsset.fallbackSpriteAssets ??= [];
        defaultAsset.fallbackSpriteAssets.Add(CustomEmojiAsset);

        Log.LogInfo($"Loaded {files.Length} emojis!");
    }

    private static Texture2D ResizeTexture(Texture2D source, int maxWidth, int maxHeight)
    {
        int targetWidth = source.width;
        int targetHeight = source.height;

        // preserve aspect
        if (targetWidth > maxWidth || targetHeight > maxHeight)
        {
            float aspect = (float)targetWidth / targetHeight;
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

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }
}