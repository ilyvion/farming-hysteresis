## 0.3.1 (April 3, 2021)

### Improvements 🙌
* Manifest.xml for use with ModManager now included.
* 'Growing' changed to 'sowing' in description of what the mod does.

### Bug Fixes 🐛
* Whether or not to allow sowing only got updated on a latch mode change, which meant that if the zone was set to not allow sowing manually, and then was set to use hysteresis, sowing would not be enabled until there was a change in hysteresis state.

## 0.3.0 (March 26, 2021)

### New ✨
* There is now a Japanese translation of the mod, courtesy of Proxyer.
* You can now set map-global hysteresis bounds, which lets you make it so that all zones that grow the same product share the same values.

### Improvements 🙌
* Show main multiplier modifier keys in the increment/decrement button descriptions

## 0.2.0 (March 21, 2021)

### New ✨
* Now uses the same adjustment multiplier as production bills to allow more fine-grained bound configuration. (These modifier keys are the Ctrl and Shift buttons by default.)

### Improvements 🙌
* Only show GUI buttons when selecting a single zone. Otherwise, you'd get a set of buttons for every zone selected, which was messy.

### Bug Fixes 🐛
* Latch mode didn't get calculated on enable, so the growing zone description could show the current state as "Unknown." This value is now calculated on enable (in addition to being regularly calculated every in-game hour), to avoid this.

## 0.1.0 (March 21, 2021)

First implementation of the mod.
