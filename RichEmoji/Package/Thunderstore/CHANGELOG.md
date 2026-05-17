# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Planned

- Autocomplete dropdown for shortcode input (!)
- Configuration
- Multiplayer emoji pack syncing (?)
- GIF support (?)

## [0.2.1] - 2026-05-17

### Added

- Thunderstore "Usage" wiki page

### Changed

- Shortened package README

## [0.2.0] - 2026-05-17

### Added

- Other players with the mod and same emojis should now see the correct emojis

### Changed

- Players without the mod will now see the shortcode instead of broken Unicode
- Input text now gets converted to shortcodes when submitted
- Reserved Unicode characters now only used when editing text input

## [0.1.1] - 2026-05-16

### Added

- Thunderstore package metadata

## [0.1.0] - 2026-05-16

### Added

- Custom emoji support via PNG files in the `emojis/` folder
- Emoji replacement using Discord-style shortcodes (`:emoji_name:`)
- Emoji replacement for pasted Unicode emoji sequences
- Multi-codepoint emoji support
- Automatic atlas packing for emoji textures

