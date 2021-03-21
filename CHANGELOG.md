## 0.2.0 (March 21, 2021)

### New ✨
* Now uses the same adjustment multiplier as production bills to allow more fine-grained bound configuration. (These modifier keys are the Ctrl and Shift buttons by default.)

### Improvements 🙌
* Only show GUI buttons when selecting a single zone. Otherwise, you'd get a set of buttons for every zone selected, which was messy.

### Bug Fixes 🐛
* Latch mode didn't get calculated on enable, so the growing zone description could show the current state as "Unknown." This value is now calculated on enable (in addition to being regularly calculated every in-game hour), to avoid this.

## 0.1.0 (March 21, 2021)

First implementation of the mod.
