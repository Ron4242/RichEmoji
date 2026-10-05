# RichEmoji

![Valheim](https://img.shields.io/badge/Valheim-deep--north--update-blue?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-393?style=flat-square)
[![Github](https://img.shields.io/badge/github-repo-black?logo=github&style=flat-square)](https://github.com/Ron4242/RichEmoji)

**All the emojis you ~~never asked for~~ could ever ask for, now in Valheim!**

## Features
- Discord-style shortcodes - type `:wave:` and it becomes 👋
- Autocomplete - start typing a shortcode and pick from a list of matching emojis
- Custom emoji support - add your own PNG files and use them in Valheim as emojis (see below)
- Multiplayer compatible - other players with the mod can still see your emojis, everyone else sees shortcodes
- Paste support - optionally copy and paste Unicode emojis from the web / emoji picker
- Twemoji - Includes Twitter emojis (you can add your own, too) so you'll have plenty to start (~1900 emojis)

You can use these emojis almost everywhere: chat, signs, tame names, portal tags, map pins, and your favorite chat mods.

## Usage

<details>
<summary><b>Shortcodes</b></summary>

Type a shortcode matching your emoji's filename and it'll be replaced with the matching sprite:

```
:wave: -> wave.png
:thumbsup: -> thumbsup.png
```

You can also directly paste the emoji if it has a Unicode mapped to it.

</details>

<details>
<summary><b>Autocomplete</b></summary>

Type `:` followed by at least two letters (e.g. `:th`) and a list of matching emojis pops up above the text box.

| Key         | Action                    |
|-------------|---------------------------|
| Up / Down   | Choose an emoji           |
| Tab / Enter | Insert the selected emoji |
| Esc         | Close the list            |

If you don't want to see it, set `Autocomplete = false` in `BepInEx/config/Ron4242.github.RichEmoji.cfg`, or toggle it
in-game with
[ConfigurationManager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/).

</details>

<details>
<summary><b>Adding your own emojis</b></summary>

All emojis are loaded from the RichEmoji config folder found at `BepInEx/config/RichEmoji/emojis/`

- The file name is the shortcode, so `party_parrot.png` becomes `:party_parrot:`
- You can nest folders to keep things organized and easy to share
- On first launch, the bundled emojis are copied into this folder. After that it's all yours. Feel free to remove packs
  you don't want
- To restore the default emojis, delete the `emojis` folder and restart the game

For more info, like how to support Unicode mapping, check
the [usage wiki](https://thunderstore.io/c/valheim/p/Ron4242/RichEmoji/wiki/5229-usage/)!

</details>

## Installation

1. Make sure you have the [BepInEx Valheim Pack](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
   dependency
2. Install
   via [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager), [r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/), [Gale](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/)
   or manually
    - For manual install, extract the archive to `BepInEx/plugins/`
3. Launch the game once to load the bundled emojis
4. Put any custom emoji PNGs into the emojis/ folder inside the config directory: `BepInEx/config/RichEmoji/emojis/`

## Notes

Should be compatible with everything.

Please report any broken or malformed Unicode emojis (provided they're valid) to `ro_n` on Discord, and I'll get them
fixed.

- Multiplayer: This mod is client-sided and does not need to be installed on the server. Emojis are converted to their
  shortcodes, so what
  other players see depends on their own emoji pack. If they have a matching shortcode, they'll see their own version of
  it (e.g. :joy: can show up differently based on what that image is in their files).

See the changelog for planned features.

## Contact

[![Discord](https://img.shields.io/badge/Discord_User-r__on-discord?logo=discord&color=%235865F2&style=flat-square)](https://discord.com/users/222464786376425473)
[![Discord](https://img.shields.io/badge/Discord_Server-Valheim_Modding-discord?logo=discord&color=%235865F2&style=flat-square)](https://discord.gg/ktFvZ8ET)
[![Discord](https://img.shields.io/badge/Discord_Server-Jotunn_Valheim-discord?logo=discord&color=%235865F2&style=flat-square)](https://discord.gg/kWyQcMwd)

If you have a bug, suggestion, issues using or installing the mod, or even something nice to say, shoot me a DM or
mention on Discord! I'm also in the above servers (they are not mine!)

---

I know there's not enough emojis on this page, but I don't really like them... 😔
