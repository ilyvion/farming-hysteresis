[![RimWorld 1.4](https://img.shields.io/badge/RimWorld-1.4-brightgreen.svg)](http://rimworldgame.com/) [![Build](https://github.com/alexschrod/farming-hysteresis/actions/workflows/build.yml/badge.svg)](https://github.com/alexschrod/farming-hysteresis/actions/workflows/build.yml)

Farming Hysteresis lets you automate enabling and disabling growing zones based on the amount of product already in storage.

I got tired of keeping track of these numbers myself, so I made a little mod to do it for me. Hopefully it can be of use to you too.

## Operation

The mod adds an "Enable farming hysteresis" to all supported growing zones. Only growing zones that produce a harvestable product are supported. When you enable this setting, the "Enable sowing" button is removed, and sowing is instead controlled by a set of hysteresis bounds.

When the amount of stored product goes below the lower bound, sowing is enabled. Sowing remains enabled until the amount of stored product goes above the upper bound, at which point sowing is disabled. Sowing remains disabled until the amount of stored product once again goes below the lower bound.

## Contributors

* Proxyer: Japanese translation

## License

Licensed under either of

* Apache License, Version 2.0, ([LICENSE.Apache-2.0](LICENSE.Apache-2.0) or http://www.apache.org/licenses/LICENSE-2.0)
* MIT license ([LICENSE.MIT](LICENSE.MIT) or http://opensource.org/licenses/MIT)

at your option.

`SPDX-License-Identifier: Apache-2.0 OR MIT`

### Contribution

Unless you explicitly state otherwise, any contribution intentionally submitted
for inclusion in the work by you, as defined in the Apache-2.0 license, shall be
dual licensed as above, without any additional terms or conditions.
