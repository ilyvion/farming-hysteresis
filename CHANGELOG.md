# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/olivierlacan/keep-a-changelog/compare/v0.4.1...HEAD
[0.4.1]: https://github.com/alexschrod/farming-hysteresis/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/alexschrod/farming-hysteresis/compare/v0.3.2...v0.4.0
[0.3.2]: https://github.com/alexschrod/farming-hysteresis/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/alexschrod/farming-hysteresis/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/alexschrod/farming-hysteresis/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/alexschrod/farming-hysteresis/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/alexschrod/farming-hysteresis/releases/tag/v0.1.0
