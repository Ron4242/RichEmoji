# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Planned

- Automatically add additional emojis based on item stack icons (?)
- Multiplayer emoji pack syncing (?)
- GIF support (?)

<details open>
<summary><b>[1.0.0] - 2026-10-05</b></summary>

### ❗ Breaking changes

- All emojis are now in `BepInEx/config/RichEmoji/emojis`. Bundled emojis are copied there on first launch. Delete the
  folder if you want to restore the default emojis (back up existing ones!)
- Emojis are no longer loaded from `BepInEx/plugins/RichEmoji/emojis`. Custom emojis still in there on first launch are
  copied over too, but mod manager updates usually wipe that folder, so **back up your emojis before moving them to the
  new config.** Sorry for the inconvenience!
- Signs, tame names, portal tags and map pins saved with emojis by older versions may show the wrong emoji and need to
  be
  re-entered once.

### Added

- Autocomplete popup when typing a shortcode (`:wa`...). Use Up/Down to pick, Tab or Enter to insert, Esc to close
- Config option to turn autocomplete off (`BepInEx/config/Ron4242.github.RichEmoji.cfg`, or in-game with
  ConfigurationManager)
- The log now lists how many emojis were loaded from each folder, and warns about duplicate or unreadable images

### Changed

- Lowered texture size to 64x64 to use a bit less memory (should still look fine)
- Emojis are loaded in a consistent (alphabetical) order

### Fixed

- Emojis on signs, tame names, portal tags and map pins could change to different emojis after adding new ones
- Some Unicode emojis being skipped or mapped to the wrong image
- Caret jumping to the wrong place after a shortcode is converted
- Small memory leak when resizing large emoji images

</details>

<details>
<summary><b>[0.3.1] - 2026-05-17</b></summary>

### Fixed

- Fixed flag shortcodes not converting properly

</details>

<details>
<summary><b>[0.3.0] - 2026-05-17</b></summary>

### Added

- Emojis are now resized to a maximum of 96x96 (respects aspect ratio)
- 6400 emoji limit

### Changes

- Emojis are now loaded in parallel, improving their load time by an order of magnitude

### Fixes

- Emojis should no longer clash with other Unicode sequences

</details>

<details>
<summary><b>[0.2.1] - 2026-05-17</b></summary>

### Added

- Thunderstore "Usage" wiki page

### Changed

- Shortened package README

</details>

<details>
<summary><b>[0.2.0] - 2026-05-17</b></summary>

### Added

- Other players with the mod and same emojis should now see the correct emojis

### Changed

- Players without the mod will now see the shortcode instead of broken Unicode
- Input text now gets converted to shortcodes when submitted
- Reserved Unicode characters now only used when editing text input

</details>

<details>
<summary><b>[0.1.1] - 2026-05-16</b></summary>

### Added

- Thunderstore package metadata

</details>

<details>
<summary><b>[0.1.0] - 2026-05-16</b></summary>

### Added

- Custom emoji support via PNG files in the `emojis/` folder
- Emoji replacement using Discord-style shortcodes (`:emoji_name:`)
- Emoji replacement for pasted Unicode emoji sequences
- Multi-codepoint emoji support
- Automatic atlas packing for emoji textures

</details>
