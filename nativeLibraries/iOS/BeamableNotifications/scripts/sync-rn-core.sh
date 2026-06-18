#!/usr/bin/env bash
#
# Mirrors the Swift core sources into the React Native package.
#
# RN compiles the core FROM SOURCE (the bridge + core share one Swift module), but
# CocoaPods sandboxes a pod's source_files to the pod root — so the core can't be
# globbed from ../core. This script copies core/Sources/BeamableNotifications into
# reactnative/ios/core/ so the pod's "ios/**/*.swift" glob picks it up.
#
# Re-run after changing anything under core/Sources/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC="$ROOT/core/Sources/BeamableNotifications"
DST="$ROOT/reactnative/ios/core"

rm -rf "$DST"
mkdir -p "$DST/Plugins"
cp "$SRC"/*.swift "$DST/"
cp "$SRC"/Plugins/*.swift "$DST/Plugins/"

echo "==> Mirrored core → reactnative/ios/core/"
find "$DST" -name '*.swift' | sed "s#$ROOT/##" | sort
