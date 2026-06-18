#!/bin/bash

# PREREQ:
#   Run ./setup-native.sh at least once before running this script.
#
# This script will be run many times as you develop the native Android libraries.
# Each run:
#   1. Builds both AARs (pushnotifications-release.aar, deeplink-release.aar)
#      using the JDK 17 + Android SDK resolved by setup-native.sh.
#   2. Copies them into the Unity client at client/Assets/Plugins/Android/.
#   3. Opens that folder so you can see the packages.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ANDROID_DIR="$SCRIPT_DIR/nativeLibraries/Android"
ENV_FILE="$ANDROID_DIR/.native-build-env"
CLIENT_ANDROID_DIR="$SCRIPT_DIR/client/Assets/Plugins/Android"

PUSH_PROJ="$ANDROID_DIR/PushNotifications"
DEEPLINK_PROJ="$ANDROID_DIR/Deeplink"
PUSH_AAR="$PUSH_PROJ/pushnotifications/build/outputs/aar/pushnotifications-release.aar"
DEEPLINK_AAR="$DEEPLINK_PROJ/deeplink/build/outputs/aar/deeplink-release.aar"

GRADLE_VERSION=8.2

# ---------------------------------------------------------------------------
# Detect OS + load the environment written by setup-native.sh
# ---------------------------------------------------------------------------
case "$(uname -s)" in
  Darwin)               OS=macos ;;
  MINGW*|MSYS*|CYGWIN*) OS=windows ;;
  *) echo "Unsupported OS: $(uname -s)."; exit 1 ;;
esac

if [ ! -f "$ENV_FILE" ]; then
  echo "$ENV_FILE not found. Run ./setup-native.sh first."
  exit 1
fi
# shellcheck disable=SC1090
source "$ENV_FILE"

export JAVA_HOME="$JAVA_HOME_NATIVE"
export ANDROID_SDK_ROOT="$ANDROID_SDK_ROOT_NATIVE"

echo "=== Beamable Native Android Libraries — Build ==="
echo "JAVA_HOME        = $JAVA_HOME"
echo "ANDROID_SDK_ROOT = $ANDROID_SDK_ROOT"

# ---------------------------------------------------------------------------
# Run a gradlew task in a project. Uses `sh ./gradlew` so it works even when
# the wrapper script lacks the +x bit (common on a Windows checkout), and
# disables MSYS path mangling so the ':module:task' argument survives.
# ---------------------------------------------------------------------------
run_gradlew() { # $1=project dir, remaining args = gradle tasks/flags
  local proj="$1"; shift
  if [ ! -f "$proj/gradlew" ]; then
    echo "  Wrapper missing — bootstrapping ($GRADLE_VERSION)..."
    if command -v gradle >/dev/null 2>&1; then
      ( cd "$proj" && MSYS_NO_PATHCONV=1 gradle wrapper --gradle-version "$GRADLE_VERSION" )
    else
      echo "  ERROR: no gradlew and no 'gradle' on PATH. Re-run ./setup-native.sh in a fresh shell."
      exit 1
    fi
  fi
  ( cd "$proj" && MSYS_NO_PATHCONV=1 sh ./gradlew "$@" )
}

# ---------------------------------------------------------------------------
# 1. Build both AARs
# ---------------------------------------------------------------------------
echo ""
echo "--- Building com.beamable.push ---"
run_gradlew "$PUSH_PROJ" :pushnotifications:assembleRelease

echo ""
echo "--- Building com.beamable.deeplink ---"
run_gradlew "$DEEPLINK_PROJ" :deeplink:assembleRelease

for aar in "$PUSH_AAR" "$DEEPLINK_AAR" ; do
  [ -f "$aar" ] || { echo "ERROR: expected artifact not produced: $aar"; exit 1; }
done

# ---------------------------------------------------------------------------
# 2. Copy AARs into the Unity client
# ---------------------------------------------------------------------------
echo ""
echo "--- Copying AARs into the client ---"
mkdir -p "$CLIENT_ANDROID_DIR"
cp "$PUSH_AAR"     "$CLIENT_ANDROID_DIR/pushnotifications-release.aar"
cp "$DEEPLINK_AAR" "$CLIENT_ANDROID_DIR/deeplink-release.aar"
echo "  pushnotifications-release.aar → $CLIENT_ANDROID_DIR"
echo "  deeplink-release.aar          → $CLIENT_ANDROID_DIR"

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
echo ""
echo "Done. Next: open the client in Unity 2021.3.45f2 and run"
echo "Tools/Beamable/Android/Setup & Validation to verify the AARs."
