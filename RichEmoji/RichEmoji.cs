using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
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
        if (files.Length == 0) return; // got no mojis

        List<Texture2D> individualTextures = [];
        List<string> fileNames = [];

        // collect images
        foreach (string file in files)
        {
            byte[] fileData = File.ReadAllBytes(file);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            LoadImageMethod.Invoke(null, [tex, fileData]);
            individualTextures.Add(tex);
            fileNames.Add(Path.GetFileNameWithoutExtension(file));
        }

        // TODO: this max atlas size probably isn't enough
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

        // theoretically supports variable length emojis
        for (int i = 0; i < rects.Length; i++)
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
            uint unicode = (uint)(0xE000 + i); // TODO: we need to avoid clashing with real unicodes.

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
}