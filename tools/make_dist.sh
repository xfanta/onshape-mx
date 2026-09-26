#!/bin/sh
# Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
# Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.
#
# Builds the two release files into dist/:
#   Onshape.lplug4          the Options+ plugin (Release build, packed and verified)
#   onshape-mx-bridge.zip   the Chrome extension, to unzip and load unpacked
#
# Note: the Release build recreates OnshapePlugin.link in the Plugins folder, like
# every build. Delete it before testing an installed .lplug4.

set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
DIST="$ROOT/dist"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$HOME/.dotnet/tools:$PATH"
export DOTNET_ROLL_FORWARD=LatestMajor

mkdir -p "$DIST"
rm -f "$DIST/Onshape.lplug4" "$DIST/onshape-mx-bridge.zip"

(cd "$ROOT/OnshapePlugin" && dotnet build -c Release --nologo -v quiet)

# The symbols are for debugging here, not for shipping.
rm -f "$ROOT/OnshapePlugin/bin/Release/bin/OnshapePlugin.pdb"

logiplugintool pack "$ROOT/OnshapePlugin/bin/Release" "$DIST/Onshape.lplug4"
logiplugintool verify "$DIST/Onshape.lplug4"

(cd "$ROOT" && zip -q -r -X "$DIST/onshape-mx-bridge.zip" extension -x '*.DS_Store')

ls -l "$DIST"
