# RichEmoji

![Valheim](https://img.shields.io/badge/Valheim-bog--witch--update-teal?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-393?style=flat-square)
![Discord](https://img.shields.io/badge/Discord-r__on-discord?logo=discord&color=%235865F2&style=flat-square)
[![Github](https://img.shields.io/badge/github-repo-blue?logo=github&style=flat-square)](https://github.com/Ron4242/RichEmoji)

### ❗ Disclaimer ❗

This is an initial client-side release and may contain a few bugs, like some emojis with multiple codepoints not showing
up properly.
Please report any broken or malformed Unicode emojis (provided they're valid) to `ro_n` on Discord and I'll get them
fixed. The mod also adds a few seconds to startup with the current emoji pack.

## About

All the emojis you ~~never asked for~~ could ever ask for, now in Valheim!

- Custom emoji support - add your own PNG files and use them in Valheim as emojis (see below)
- Discord-style shortcodes - type `:wave:` and it becomes 👋
- Paste support - optionally copy and paste Unicode emojis from the web / emoji picker
- Twemoji - Includes Twitter emojis (you can add your own, too) so you'll have plenty to start (~1900 emojis)

You can use these emojis almost everywhere, like in your favorite chat mods or on signs.

## Usage

### Shortcodes

Type a shortcode matching your emoji's filename and it'll be replaced with the matching sprite:

```
:wave: -> wave.png
:thumbsup: -> thumbsup.png
```

You can also directly paste the emoji if it has a Unicode mapped to it.

### File naming

| Emoji Type                              | Filename format                |
|-----------------------------------------|--------------------------------|
| Basic :custom_emoji:                    | `wave.png`                     |
| With Unicode mapping                    | `wave__1F44B.png`              |
| Multi-codepoint (e.g. flags, skin tone) | `wave_medium__1F44B-1F3FD.png` |

The `__` (double underscore) separator followed by hex codepoints lets RichEmoji map pasted Unicode to your custom
glyphs. Codepoints are separated by `-` (hyphen). Check the included emojis for more examples.

---

## Installation

1. Make sure you have the [BepInEx Valheim Pack](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
   dependency
2. Install
   via [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager), [r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/) (
   recommended) or manually
   - For manual install, extract the archive to `BepInEx/plugins/`
3. Drop your emoji PNGs into the `emojis/` folder inside the plugin directory:
   ```
   BepInEx/plugins/RichEmoji/emojis/yourEmojiHere.png
   ```

You can nest your emojis inside folders to keep them organized and easy to share.

## Notes

Should be compatible with everything.

- Multiplayer: This mod is currently client-side only. Players without the mod will see raw text (unrecognized
  characters) or shortcodes.
- Atlas size: The current maximum atlas size is 8192px. Weird things will probably happen if you have too many emojis.

## Planned Features

- Autocomplete dropdown for shortcode input (!!!)
- Multiplayer emoji syncing (?)
- GIF support (!?!?)
- Probably some configuration (??)

---

## Contact

[![Discord](https://img.shields.io/badge/Discord-Valheim_Modding-discord?logo=discord&color=%235865F2&style=flat-square)](https://discord.gg/ktFvZ8ET)
[![Discord](https://img.shields.io/badge/Discord-Jotunn_Valheim-discord?logo=discord&color=%235865F2&style=flat-square)](https://discord.gg/kWyQcMwd)

If you have a bug, suggestion, or even something nice to say, shoot me a DM or mention on Discord! You can find me on
the above Discord servers (**they are not mine!**)

---

I know there's not enough emojis on this page, but I don't really like them... 😔