[![RimWorld 1.3](https://img.shields.io/badge/RimWorld-1.3-brightgreen.svg)](http://rimworldgame.com/) [![RimWorld 1.4](https://img.shields.io/badge/RimWorld-1.4-brightgreen.svg)](http://rimworldgame.com/) [![RimWorld 1.5](https://img.shields.io/badge/RimWorld-1.5-brightgreen.svg)](http://rimworldgame.com/) [![RimWorld 1.6](https://img.shields.io/badge/RimWorld-1.6-brightgreen.svg)](http://rimworldgame.com/) [![Build](https://github.com/ilyvion/farming-hysteresis/actions/workflows/ci.yml/badge.svg)](https://github.com/ilyvion/farming-hysteresis/actions/workflows/ci.yml)

Farming Hysteresis lets you automate enabling and disabling growing zones and hydroponics basins based on the amount of product already in storage.

## Operation

Farming Hysteresis automates enabling and disabling sowing and/or harvesting based on the amount of product already in storage: when the amount of stored product goes below a lower bound, sowing and/or harvesting is enabled and remains enabled until the amount of stored product goes above an upper bound, at which point it is disabled and remains disabled until the amount of stored product once again goes below the lower bound.

Here's a graph illustrating the hysteresis process:
![graph showing a hysteresis process](https://user-images.githubusercontent.com/767490/210202973-3642db8c-9558-40e4-8c25-82e34a7821dd.png)

<!-- From: https://www.canva.com/design/DAFWfzZWia4/xychZn8doj014b2Hc99_dQ/edit?category=tADWs7A4Dr8 -->

In this example, we assume the lower bound is set to 500, and the upper bound is set to 1000. As we can see by the three regions:

-   0-500 is the region below the "lower bound" in which sowing gets enabled if it were disabled,
-   500-1000 is the region between the "lower bound" and the "upper bound" in which no change to whether sowing is enabled or disabled is made,
-   1000+ is the region above the "upper bound" in which sowing gets disabled if it were enabled.

What this gives us is a way to build up a buffer of product, then hold off on production while we're consuming some of it, and then turning production back on when we're running low, but before we're running out.

## Colony Manager Redux integration (recommended)

The recommended way to use Farming Hysteresis is alongside [Colony Manager Redux](https://github.com/ilyvion/colony-manager-redux) (CMR). With CMR installed, hysteresis is managed through a CMR manager job instead of being configured grower-by-grower: a job covers a chosen area/zone selection and shows up right in CMR's own manager tab, like any other job. This is where ongoing development is focused, and it unlocks some extra features over the legacy controls below:

- **Crop rotation**: set up an ordered list of crops for a job, and once the current crop's stock threshold is met, growers automatically move on to the next crop in the list, cycling back around at the end.
- **Tracking a different item than what's grown**: for example, sow hops based on how much beer you have in stock, rather than how many hops.
- **History graphing**: a graph tracking stock against the lower and upper bounds over time, same as CMR's other manager jobs.
- **Dual-crop support**: plants that yield a bonus secondary product alongside their main harvest (e.g. via Vanilla Expanded Framework) can have that secondary product tracked instead of, or alongside, the primary one.

CMR is not required, however — everything below continues to work exactly as it always has if you don't install CMR, or don't turn this on for a given save.

## Legacy per-grower controls

Without CMR (or with CMR-managed hysteresis turned off), the mod adds an "Enable hysteresis" gizmo to all supported growing zones and hydroponics basins. Only plants that produce a harvestable product are supported. When you enable this setting, the "Enable sowing" button is removed, and sowing and/or harvesting is instead controlled by the hysteresis bounds described above, editable per-grower. Hydroponics basins typically don't have this setting, but with this mod and hysteresis enabled, sowing and/or harvesting also stops on those when appropriate.

This mode still works and isn't going away, but it's no longer where new features are added — see the section above.

## License

Licensed under either of

-   Apache License, Version 2.0, ([LICENSE.Apache-2.0](LICENSE.Apache-2.0) or http://www.apache.org/licenses/LICENSE-2.0)
-   MIT license ([LICENSE.MIT](LICENSE.MIT) or http://opensource.org/licenses/MIT)

at your option.

`SPDX-License-Identifier: Apache-2.0 OR MIT`

### Contribution

Unless you explicitly state otherwise, any contribution intentionally submitted
for inclusion in the work by you, as defined in the Apache-2.0 license, shall be
dual licensed as above, without any additional terms or conditions.

### Attribution

Parts of this mod were created by, or derived from works created by;

- freepngimg.com: icon used for the manager tab in the Colony Manager mod integration ([BY-NC](https://freepngimg.com/png/9425-agriculture-transparent))
