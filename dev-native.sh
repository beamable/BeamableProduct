#!/bin/bash

# PREREQ:
#   Run ./setup-native.sh at least once before running this script.
#
# This script will be run many times as you develop the native libraries.
# Each run:
#   1. Builds the unified AAR (beamable-notifications-release.aar — push + deep links)
#      using the JDK 17 + Android SDK resolved by setup-native.sh.
#   2. Copies it into the shared Unity package at
#      nativeLibraries/EnginePlugins/Unity/Plugins/Android/ (the package ships the binary;
#      the Unity client consumes it via its local UPM reference), into the unified React
#      Native package, and into the Unreal plugin's ThirdParty/Android/.
#   3. On macOS (and only if setup-native.sh found Xcode), builds the iOS
#      BeamableNotifications.xcframework via the inner build-xcframework.sh script
#      and replaces the copy under nativeLibraries/EnginePlugins/Unity/Plugins/iOS/ and the
#      React Native package; then builds the dynamic-framework variant via
#      build-xcframework-dynamic.sh and stages its BeamableNotifications.embeddedframework.zip
#      into the Unreal plugin's ThirdParty/.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ANDROID_DIR="$SCRIPT_DIR/nativeLibraries/Android"
ENV_FILE="$ANDROID_DIR/.native-build-env"
PACKAGE_ANDROID_DIR="$SCRIPT_DIR/nativeLibraries/EnginePlugins/Unity/Plugins/Android"
PACKAGE_IOS_DIR="$SCRIPT_DIR/nativeLibraries/EnginePlugins/Unity/Plugins/iOS"
PACKAGE_RN_ANDROID_DIR="$SCRIPT_DIR/nativeLibraries/EnginePlugins/ReactNative/android/libs"
PACKAGE_RN_IOS_DIR="$SCRIPT_DIR/nativeLibraries/EnginePlugins/ReactNative/ios"

# Unreal plugin: ships its native binaries under ThirdParty/ (iOS embeddedframework.zip
# directly under ThirdParty/, Android .aar under ThirdParty/Android/).
PACKAGE_UNREAL_DIR="$SCRIPT_DIR/nativeLibraries/EnginePlugins/Unreal"
PACKAGE_UNREAL_TP="$PACKAGE_UNREAL_DIR/ThirdParty"
PACKAGE_UNREAL_ANDROID="$PACKAGE_UNREAL_TP/Android"

NOTIF_PROJ="$ANDROID_DIR/BeamableNotifications"
NOTIF_AAR="$NOTIF_PROJ/notifications/build/outputs/aar/notifications-release.aar"

IOS_PROJ="$SCRIPT_DIR/nativeLibraries/iOS/BeamableNotifications"
IOS_BUILD_SCRIPT="$IOS_PROJ/scripts/build-xcframework.sh"
IOS_XCFRAMEWORK="$IOS_PROJ/build/BeamableNotifications.xcframework"
IOS_DYNAMIC_BUILD_SCRIPT="$IOS_PROJ/scripts/build-xcframework-dynamic.sh"
IOS_EMBEDDED_ZIP="$IOS_PROJ/build/BeamableNotifications.embeddedframework.zip"

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

echo "=== Beamable Native Libraries — Build ==="
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

echo ""
echo "--- Copying AAR into the unified React Native package ---"
mkdir -p "$PACKAGE_RN_ANDROID_DIR"
cp "$NOTIF_AAR" "$PACKAGE_RN_ANDROID_DIR/beamable-notifications-release.aar"
echo "  beamable-notifications-release.aar → $PACKAGE_RN_ANDROID_DIR"

echo ""
echo "--- Staging AAR into the Unreal plugin (ThirdParty/Android) ---"
# Flat .aar consumed by the APL's <AARImports> via a Gradle flatDir repo.
mkdir -p "$PACKAGE_UNREAL_ANDROID"
cp "$NOTIF_AAR" "$PACKAGE_UNREAL_ANDROID/beamable-notifications-release.aar"
echo "  beamable-notifications-release.aar → $PACKAGE_UNREAL_ANDROID"

# ---------------------------------------------------------------------------
# 3. iOS xcframework (macOS only). Builds BeamableNotifications.xcframework via the
#    existing nativeLibraries/iOS script and replaces the copy under Plugins/iOS so
#    Unity picks up the fresh slices. We only swap the binary directory; any
#    committed .xcframework.meta files persist.
# ---------------------------------------------------------------------------
if [ "$OS" = macos ] && [ "${IOS_SUPPORTED_NATIVE:-false}" = true ]; then
  echo ""
  echo "--- Building com.beamable.notifications iOS xcframework ---"
  bash "$IOS_BUILD_SCRIPT"

  [ -d "$IOS_XCFRAMEWORK" ] || { echo "ERROR: expected artifact not produced: $IOS_XCFRAMEWORK"; exit 1; }

  echo ""
  echo "--- Copying xcframework into the shared Unity package ---"
  mkdir -p "$PACKAGE_IOS_DIR"
  rm -rf "$PACKAGE_IOS_DIR/BeamableNotifications.xcframework"
  cp -R "$IOS_XCFRAMEWORK" "$PACKAGE_IOS_DIR/BeamableNotifications.xcframework"
  echo "  BeamableNotifications.xcframework → $PACKAGE_IOS_DIR"

  echo ""
  echo "--- Copying xcframework into the unified React Native package ---"
  mkdir -p "$PACKAGE_RN_IOS_DIR"
  rm -rf "$PACKAGE_RN_IOS_DIR/BeamableNotifications.xcframework"
  cp -R "$IOS_XCFRAMEWORK" "$PACKAGE_RN_IOS_DIR/BeamableNotifications.xcframework"
  echo "  BeamableNotifications.xcframework → $PACKAGE_RN_IOS_DIR"

  # Unreal needs a DYNAMIC framework packaged as BeamableNotifications.embeddedframework.zip
  # (UE's PublicAdditionalFrameworks can't consume the static .xcframework used above). Run the
  # dynamic build only after the static slices are already copied out: it does `rm -rf build/`
  # and reuses the same xcframework name, so order matters.
  echo ""
  echo "--- Building dynamic embeddedframework for the Unreal plugin ---"
  bash "$IOS_DYNAMIC_BUILD_SCRIPT"

  [ -f "$IOS_EMBEDDED_ZIP" ] || { echo "ERROR: expected artifact not produced: $IOS_EMBEDDED_ZIP"; exit 1; }

  echo ""
  echo "--- Staging embeddedframework.zip into the Unreal plugin (ThirdParty) ---"
  mkdir -p "$PACKAGE_UNREAL_TP"
  cp "$IOS_EMBEDDED_ZIP" "$PACKAGE_UNREAL_TP/BeamableNotifications.embeddedframework.zip"
  echo "  BeamableNotifications.embeddedframework.zip → $PACKAGE_UNREAL_TP"
elif [ "$OS" = macos ]; then
  echo ""
  echo "  Skipping iOS build (IOS_SUPPORTED_NATIVE=false in $ENV_FILE)."
  echo "  Re-run ./setup-native.sh after installing Xcode to enable iOS."
fi

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
echo ""
echo "Done. Next: open the client in Unity 2021.3.45f2 and run"
echo "Tools/Beamable/Android/Setup & Validation to verify the Android setup."
echo "The Unreal plugin's native binaries are staged under"
echo "nativeLibraries/EnginePlugins/Unreal/ThirdParty/ (Android .aar always; iOS"
echo "embeddedframework.zip on macOS with Xcode)."
if [ "$OS" = macos ] && [ "${IOS_SUPPORTED_NATIVE:-false}" = true ]; then
  echo "The iOS xcframework is installed under nativeLibraries/EnginePlugins/Unity/Plugins/iOS/"
  echo "and will be picked up next time Unity builds for iOS."
fi
