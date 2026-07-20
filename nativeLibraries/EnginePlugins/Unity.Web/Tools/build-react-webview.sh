#!/usr/bin/env bash
# Exports a React (Expo) app as a web bundle and packages it into a Unity project's
# StreamingAssets, where StreamingAssetsServer (com.beamable.notifications.webview)
# serves it to the WebView at runtime — no external server needed.
#
# This script ships inside the package and does not assume any particular consuming
# project, so both source and destination are configurable:
#   RN_SAMPLE_DIR     Expo app to export      (default: the reference sample,
#                     nativeLibraries/Samples/ReactNative, resolved relative to this script)
#   UNITY_PROJECT_DIR Unity project to fill   (default: the current working directory)
#   CONTENT_FOLDER    StreamingAssets subdir  (default: react — must match
#                     StreamingAssetsServer.contentFolder)
#
# Usage (from your Unity project root):
#   /path/to/com.beamable.notifications.webview/Tools/build-react-webview.sh
# Or fully explicit:
#   RN_SAMPLE_DIR=/path/to/app UNITY_PROJECT_DIR=/path/to/UnityProject ./build-react-webview.sh
set -euo pipefail

PACKAGE_DIR="$(cd "$(dirname "$0")/.." && pwd)"
# Reference sample: nativeLibraries/Samples/ReactNative (…/EnginePlugins/Unity.WebView → ../../Samples/ReactNative)
RN_SAMPLE_DIR="${RN_SAMPLE_DIR:-$PACKAGE_DIR/../../Samples/ReactNative}"
UNITY_PROJECT_DIR="${UNITY_PROJECT_DIR:-$(pwd)}"
CONTENT_FOLDER="${CONTENT_FOLDER:-react}"
DEST="$UNITY_PROJECT_DIR/Assets/StreamingAssets/$CONTENT_FOLDER"

if [ ! -d "$RN_SAMPLE_DIR" ]; then
  echo "✗ RN_SAMPLE_DIR does not exist: $RN_SAMPLE_DIR" >&2
  exit 1
fi
if [ ! -d "$UNITY_PROJECT_DIR/Assets" ]; then
  echo "✗ UNITY_PROJECT_DIR is not a Unity project (no Assets/): $UNITY_PROJECT_DIR" >&2
  echo "  Run this from your Unity project root, or set UNITY_PROJECT_DIR." >&2
  exit 1
fi

echo "→ Exporting web bundle from $RN_SAMPLE_DIR"
(cd "$RN_SAMPLE_DIR" && npx expo export -p web)

echo "→ Copying dist → $DEST"
rm -rf "$DEST"
mkdir -p "$DEST"
cp -R "$RN_SAMPLE_DIR/dist/." "$DEST/"

# manifest.txt lists every file so StreamingAssetsServer can load them via
# UnityWebRequest (required on Android, where StreamingAssets live in the APK
# and cannot be enumerated at runtime). Unity's .meta files are excluded.
echo "→ Writing manifest.txt"
(cd "$DEST" && find . -type f ! -name '*.meta' ! -name 'manifest.txt' | sed 's|^\./||' | sort > manifest.txt)

COUNT=$(wc -l < "$DEST/manifest.txt" | tr -d ' ')
echo "✓ Packaged $COUNT files into Assets/StreamingAssets/$CONTENT_FOLDER"
