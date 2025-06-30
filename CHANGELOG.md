# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.9.0] - 2025-06-30

### Added

- Rimworld 1.6 support.

## [0.8.2] - 2024-05-25

### Added

- Added a dialog to get people to add a future dependency. Since Rimworld doesn't have a good mechanism to automatically add new dependencies, we need to be a little in-your-face about it.

## [0.8.1] - 2024-05-22

### Fixed

- Type checking code was a bit too narrow, causing exceptions where they weren't appropriate.

## [0.8.0] - 2024-05-22

### Added

- Added compatibility with Vanilla Plants Expanded - More Plants' custom aquatic and sandy growing zones.
- Added filtering to hysteresis tab.
- Added a setting for counting all items on a map instead of only items in storage.

## [0.7.0] - 2024-05-21

### Added

- Added a setting to hide the Hysteresis tab from the game.

### Changed

- Removed farming hysteresis text from inspect strings as well as the "configure hysteresis" gizmo and related dialog. Both concepts were unified into an inspect tab that is now added instead.

## [0.6.1] - 2024-04-24

### Added

- Added support for versions 1.3 and 1.5.

### Fixed

- Harvest control logic wasn't working properly for growing zones since I had misunderstood the logic of "allow cut" on growing zones.

## [0.6.0] - 2023-01-04

### Added

- Added support for choosing which aspect of farming gets controlled. The default setting is sowing, the behavior that was hardcoded in the past, but now it also allows controlling harvesting instead, or even both.
- Moved the configuration of hysteresis parameters from command buttons to a dialog. This gives better control over the bounds, including the ability to type the numbers in directly. A setting was also added to bring back the command buttons, if you prefer those.

### Fixed

- The checkbox "Growing zones and hydroponic basins enable hysteresis upon placement" was incorrectly wired to the "Use global hysteresis values by default" value, so it wasn't possible to disable hysteresis being on by default.
- Some texts had accidental newlines that have been removed.
- The mod now properly supports interacting with multiple selected growing zones.

## [0.5.1] - 2023-01-02

### Fixed

- Updated About.xml appropriately

## [0.5.0] - 2023-01-02

### Added

- Norwegian translations added.
- Main tab for hysteresis configuration added.
- Support for hydroponics added.

## [0.4.1] - 2022-10-24

### Fixed

- Only support for 1.4. Fixes bug when selecting growing zones.

## [0.4.0] - 2022-10-23

### Added

- Mod settings to let the default hysteresis values be configurable by the player.
- Mod settings for enabling hysteresis by default and for using global hysteresis values by default.

### Changed

- Declare support for 1.4.
- Using hysteresis is now enabled by default.
- Using global hysteresis values is now enabled by default.

## [0.3.2] - 2021-07-23

### Changed

- Declare support for 1.3.
- Changelog file modified to adhere to the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) pattern.
- Modified build files so the project folder must be outside the RimWorld folder, and files are copied in instead.

## [0.3.1] - 2021-04-03

### Added

- Manifest.xml for use with ModManager now included.

### Changed

- 'Growing' changed to 'sowing' in description of what the mod does.

### Fixed

- Whether or not to allow sowing only got updated on a latch mode change, which meant that if the zone was set to not allow sowing manually, and then was set to use hysteresis, sowing would not be enabled until there was a change in hysteresis state.

## [0.3.0] - 20201-03-26

### Added

- There is now a Japanese translation of the mod, courtesy of Proxyer.
- You can now set map-global hysteresis bounds, which lets you make it so that all zones that grow the same product share the same values.

### Changed

- Show main multiplier modifier keys in the increment/decrement button descriptions

## [0.2.0] - 2021-03-21

### Changed

- Now uses the same adjustment multiplier as production bills to allow more fine-grained bound configuration. (These modifier keys are the Ctrl and Shift buttons by default.)
- Only show GUI buttons when selecting a single zone. Otherwise, you'd get a set of buttons for every zone selected, which was messy.

### Fixed

- Latch mode didn't get calculated on enable, so the growing zone description could show the current state as "Unknown." This value is now calculated on enable (in addition to being regularly calculated every in-game hour), to avoid this.

## [0.1.0] 2021-03-21

### Added

- First implementation of the mod.

[Unreleased]: https://github.com/ilyvion/farming-hysteresis/compare/v0.9.0...HEAD
[0.9.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.8.2...v0.9.0
[0.8.2]: https://github.com/ilyvion/farming-hysteresis/compare/v0.8.1...v0.8.2
[0.8.1]: https://github.com/ilyvion/farming-hysteresis/compare/v0.8.0...v0.8.1
[0.8.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.6.1...v0.7.0
[0.6.1]: https://github.com/ilyvion/farming-hysteresis/compare/v0.6.0...v0.6.1
[0.6.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.5.1...v0.6.0
[0.5.1]: https://github.com/ilyvion/farming-hysteresis/compare/v0.5.0...v0.5.1
[0.5.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.4.1...v0.5.0
[0.4.1]: https://github.com/ilyvion/farming-hysteresis/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.3.2...v0.4.0
[0.3.2]: https://github.com/ilyvion/farming-hysteresis/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/ilyvion/farming-hysteresis/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/ilyvion/farming-hysteresis/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/ilyvion/farming-hysteresis/releases/tag/v0.1.0
