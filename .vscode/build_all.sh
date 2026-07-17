#!/usr/bin/env bash

# Builds the mod for all supported RimWorld versions by calling build.sh for each.

set -euo pipefail

SCRIPT_DIR="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

for version in 1.3 1.4 1.5 1.6; do
    echo "=== Building for RimWorld $version ==="
    "$SCRIPT_DIR/build.sh" "$version"
done
