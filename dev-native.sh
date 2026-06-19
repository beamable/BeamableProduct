#!/bin/bash

# PREREQ:
#   Run ./setup-native.sh at least once before running this script.
#
# This script will be run many times as you develop the native Android library.
# Each run:
#   1. Builds the unified AAR (beamable-notifications-release.aar — push + deep links)
#      using the JDK 17 + Android SDK resolved by setup-native.sh.
#   2. Copies it into the shared Unity package at
#      nativeLibraries/EnginePlugins/Unity/Plugins/Android/ (the package ships the binary;
#      the Unity client consumes it via its local UPM reference).

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ANDROID_DIR="$SCRIPT_DIR/nativeLibraries/Android"
ENV_FILE="$ANDROID_DIR/.native-build-env"
PACKAGE_ANDROID_DIR="$SCRIPT_DIR/nativeLibraries/EnginePlugins/Unity/Plugins/Android"

NOTIF_PROJ="$ANDROID_DIR/BeamableNotifications"
NOTIF_AAR="$NOTIF_PROJ/notifications/build/outputs/aar/notifications-release.aar"

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

echo "=== Beamable Native Android Library — Build ==="
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
# 1. Build the unified AAR
# ---------------------------------------------------------------------------
echo ""
echo "--- Building com.beamable.notifications (push + deeplink) ---"
run_gradlew "$NOTIF_PROJ" :notifications:assembleRelease

[ -f "$NOTIF_AAR" ] || { echo "ERROR: expected artifact not produced: $NOTIF_AAR"; exit 1; }

# ---------------------------------------------------------------------------
# 2. Copy the AAR into the shared Unity package
#    (Only the binary is overwritten; the committed .aar.meta persists.)
# ---------------------------------------------------------------------------
echo ""
echo "--- Copying AAR into the shared Unity package ---"
mkdir -p "$PACKAGE_ANDROID_DIR"
cp "$NOTIF_AAR" "$PACKAGE_ANDROID_DIR/beamable-notifications-release.aar"
echo "  beamable-notifications-release.aar → $PACKAGE_ANDROID_DIR"

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
echo ""
echo "Done. Next: open the client in Unity 2021.3.45f2 and run"
echo "Tools/Beamable/Android/Setup & Validation to verify the setup."
