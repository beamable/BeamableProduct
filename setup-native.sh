#!/bin/bash

# This script should be run ONCE before building the native libraries,
# or any time you want to re-discover/re-install the toolchain.
#
# It will:
#   1. Install Android Studio + JDK 17 + Gradle via the OS package manager
#      (winget/choco on Windows, Homebrew on macOS).
#   2. Locate a usable JDK 17 and an Android SDK (reusing Unity's bundled SDK or
#      an existing Android Studio SDK when present, instead of re-downloading);
#      accept SDK licenses idempotently. On macOS, also verify Xcode + the iOS SDK
#      so dev-native.sh can build the BeamableNotifications xcframework.
#   3. Persist the resolved JAVA_HOME / ANDROID_SDK_ROOT / IOS_SUPPORTED_NATIVE
#      to nativeLibraries/Android/.native-build-env and write a local.properties
#      (sdk.dir) into the Gradle project.
#   4. Bootstrap the Gradle wrapper (the wrapper .jar is not committed).
#
# Why JDK 17 specifically: the library pins AGP 8.1.4 (requires JDK 17 to run)
# and Gradle 8.2 (which cannot run on JDK 21 — Android Studio's bundled JBR).
# Why Xcode (macOS): the iOS xcframework is built via xcodebuild + SPM — no Pods,
# no XcodeGen, just a full Xcode install with the iOS SDK available.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ANDROID_DIR="$SCRIPT_DIR/nativeLibraries/Android"
ENV_FILE="$ANDROID_DIR/.native-build-env"

NOTIF_PROJ="$ANDROID_DIR/BeamableNotifications"
IOS_PROJ="$SCRIPT_DIR/nativeLibraries/iOS/BeamableNotifications"

GRADLE_VERSION=8.2

echo "=== Beamable Native Libraries — Setup ==="

# ---------------------------------------------------------------------------
# Detect OS
# ---------------------------------------------------------------------------
case "$(uname -s)" in
  Darwin)               OS=macos ;;
  MINGW*|MSYS*|CYGWIN*) OS=windows ;;
  *) echo "Unsupported OS: $(uname -s). This script supports Windows (Git Bash) and macOS."; exit 1 ;;
esac
echo "OS: $OS"

# Convert a path to the form native tools expect (mixed C:/... on Windows).
to_native_path() {
  if [ "$OS" = windows ]; then cygpath -m "$1"; else echo "$1"; fi
}

# ---------------------------------------------------------------------------
# 1. Install Android Studio + JDK 17 + Gradle
# ---------------------------------------------------------------------------
echo ""
echo "--- Installing toolchain (Android Studio, JDK 17, Gradle) ---"

if [ "$OS" = windows ]; then
  # Note: winget has no official Gradle package — Gradle is provided by
  # ensure_gradle() below (direct download), which is also what bootstraps the wrapper.
  if command -v winget >/dev/null 2>&1; then
    echo "  Using winget..."
    winget install --id EclipseAdoptium.Temurin.17.JDK --silent --accept-package-agreements --accept-source-agreements --disable-interactivity || true
    winget install --id Google.AndroidStudio           --silent --accept-package-agreements --accept-source-agreements --disable-interactivity || true
  elif command -v choco >/dev/null 2>&1; then
    echo "  Using choco..."
    choco install -y temurin17 androidstudio || true
  else
    echo "  Neither winget nor choco found. Install Android Studio and Temurin JDK 17 manually."
  fi
else # macos
  if command -v brew >/dev/null 2>&1; then
    echo "  Using Homebrew..."
    brew install --cask temurin@17 || true
    brew install --cask android-studio || true
  else
    echo "  Homebrew not found. Install it from https://brew.sh then re-run, or install the tools manually."
  fi
fi

# ---------------------------------------------------------------------------
# 2a. Locate JDK 17
# ---------------------------------------------------------------------------
echo ""
echo "--- Locating JDK 17 ---"

find_jdk17() {
  local c
  if [ "$OS" = windows ]; then
    for c in \
      "/c/Program Files/Eclipse Adoptium/jdk-17"* \
      "/c/Program Files/Microsoft/jdk-17"* \
      "/c/Program Files/Java/jdk-17"* \
      "/c/Program Files/Amazon Corretto/jdk17"* ; do
      [ -x "$c/bin/java" ] && { echo "$c"; return 0; }
    done
  else
    # Prefer the macOS java_home selector when available.
    if /usr/libexec/java_home -v 17 >/dev/null 2>&1; then
      /usr/libexec/java_home -v 17; return 0
    fi
    for c in /Library/Java/JavaVirtualMachines/*17*/Contents/Home ; do
      [ -x "$c/bin/java" ] && { echo "$c"; return 0; }
    done
  fi
  return 1
}

JAVA_HOME_RESOLVED="$(find_jdk17 || true)"
if [ -z "$JAVA_HOME_RESOLVED" ]; then
  echo "  ERROR: JDK 17 not found after install."
  echo "  A fresh package-manager install may need a new shell for PATH to update,"
  echo "  or the install is still finishing. Re-run this script, or set JDK 17 manually."
  exit 1
fi
echo "  JAVA_HOME = $JAVA_HOME_RESOLVED"

# ---------------------------------------------------------------------------
# 2b. Locate an Android SDK (reuse existing before downloading)
# ---------------------------------------------------------------------------
echo ""
echo "--- Locating Android SDK ---"

has_sdk() { [ -d "$1/platforms" ]; }

find_android_sdk() {
  local c
  # 1) Respect an already-configured SDK.
  for c in "$ANDROID_SDK_ROOT" "$ANDROID_HOME" ; do
    [ -n "$c" ] && has_sdk "$c" && { echo "$c"; return 0; }
  done
  if [ "$OS" = windows ]; then
    # 2) Android Studio default location.
    has_sdk "$LOCALAPPDATA/Android/Sdk" && { echo "$LOCALAPPDATA/Android/Sdk"; return 0; }
    # 3) Unity's bundled SDK (has android-34 + build-tools + accepted licenses).
    for c in "/c/Program Files/Unity/Hub/Editor/"*/Editor/Data/PlaybackEngines/AndroidPlayer/SDK ; do
      has_sdk "$c" && { echo "$c"; return 0; }
    done
  else
    has_sdk "$HOME/Library/Android/sdk" && { echo "$HOME/Library/Android/sdk"; return 0; }
    for c in "/Applications/Unity/Hub/Editor/"*/PlaybackEngines/AndroidPlayer/SDK ; do
      has_sdk "$c" && { echo "$c"; return 0; }
    done
  fi
  return 1
}

ANDROID_SDK_RESOLVED="$(find_android_sdk || true)"
if [ -z "$ANDROID_SDK_RESOLVED" ]; then
  echo "  No Android SDK found."
  echo "  Open Android Studio once and let it install the SDK (it defaults to"
  if [ "$OS" = windows ]; then echo "  %LOCALAPPDATA%\\Android\\Sdk), then re-run this script."
  else echo "  ~/Library/Android/sdk), then re-run this script."; fi
  exit 1
fi
echo "  ANDROID_SDK_ROOT = $ANDROID_SDK_RESOLVED"

# Ensure compileSdk 34 + build-tools are present and licenses accepted, IF an
# sdkmanager is available in this SDK. Unity's bundled SDK already satisfies this.
SDKMGR=""
for c in "$ANDROID_SDK_RESOLVED/cmdline-tools/latest/bin/sdkmanager"* \
         "$ANDROID_SDK_RESOLVED/cmdline-tools/"*/bin/sdkmanager* \
         "$ANDROID_SDK_RESOLVED/tools/bin/sdkmanager"* ; do
  [ -x "$c" ] && { SDKMGR="$c"; break; }
done
if [ -n "$SDKMGR" ]; then
  # Always accept licenses — Unity's bundled SDK ships with an empty licenses/ dir,
  # so even if platforms/android-34 + build-tools/34.0.0 are already present, Gradle
  # will refuse to use them until the license files are written.
  echo "  Accepting SDK licenses (idempotent) via $(basename "$SDKMGR")..."
  JAVA_HOME="$JAVA_HOME_RESOLVED" yes | "$SDKMGR" --sdk_root="$ANDROID_SDK_RESOLVED" --licenses >/dev/null 2>&1 || true
  if ! [ -d "$ANDROID_SDK_RESOLVED/platforms/android-34" ] || ! [ -d "$ANDROID_SDK_RESOLVED/build-tools/34.0.0" ]; then
    echo "  Installing platform-tools, platforms;android-34, build-tools;34.0.0..."
    JAVA_HOME="$JAVA_HOME_RESOLVED" "$SDKMGR" --sdk_root="$ANDROID_SDK_RESOLVED" \
      "platform-tools" "platforms;android-34" "build-tools;34.0.0" || true
  fi
fi

# ---------------------------------------------------------------------------
# 2c. iOS toolchain (macOS only — Windows is a no-op)
# ---------------------------------------------------------------------------
IOS_SUPPORTED=false
if [ "$OS" = macos ]; then
  echo ""
  echo "--- Verifying iOS toolchain (Xcode) ---"

  XCODE_DEV_DIR="$(xcode-select -p 2>/dev/null || true)"
  if [ -z "$XCODE_DEV_DIR" ] || [ ! -d "$XCODE_DEV_DIR" ]; then
    echo "  WARNING: xcode-select did not return a developer dir."
    echo "  Install Xcode from the App Store, then run: sudo xcode-select -s /Applications/Xcode.app"
    echo "  Skipping iOS — Android setup will still complete."
  elif ! printf '%s\n' "$XCODE_DEV_DIR" | grep -q 'Xcode'; then
    echo "  WARNING: Command Line Tools are selected, not full Xcode (got: $XCODE_DEV_DIR)."
    echo "  Install Xcode from the App Store, then run: sudo xcode-select -s /Applications/Xcode.app"
    echo "  Skipping iOS — Android setup will still complete."
  elif ! xcodebuild -showsdks 2>/dev/null | grep -q 'iphoneos'; then
    echo "  WARNING: xcodebuild found but no iOS SDK is installed."
    echo "  Open Xcode once to finish first-launch setup, then re-run this script."
    echo "  Skipping iOS — Android setup will still complete."
  else
    XCODE_VERSION="$(xcodebuild -version 2>/dev/null | head -1 || true)"
    echo "  $XCODE_VERSION"
    echo "  Developer dir = $XCODE_DEV_DIR"
    # Ensure the iOS build scripts are executable (committed without +x on Windows checkouts).
    chmod +x "$IOS_PROJ/scripts/"*.sh 2>/dev/null || true
    IOS_SUPPORTED=true
  fi
fi

# ---------------------------------------------------------------------------
# 3. Persist env + write local.properties per project
# ---------------------------------------------------------------------------
echo ""
echo "--- Writing build environment ---"

cat > "$ENV_FILE" <<EOF
# Generated by setup-native.sh — sourced by dev-native.sh. Do not edit by hand.
JAVA_HOME_NATIVE="$JAVA_HOME_RESOLVED"
ANDROID_SDK_ROOT_NATIVE="$ANDROID_SDK_RESOLVED"
IOS_SUPPORTED_NATIVE="${IOS_SUPPORTED:-false}"
EOF
echo "  Wrote $ENV_FILE"

SDK_NATIVE_PATH="$(to_native_path "$ANDROID_SDK_RESOLVED")"
for proj in "$NOTIF_PROJ" ; do
  echo "sdk.dir=$SDK_NATIVE_PATH" > "$proj/local.properties"
  echo "  Wrote $proj/local.properties"
done

# ---------------------------------------------------------------------------
# 4. Bootstrap the Gradle wrapper (wrapper .jar is not committed)
# ---------------------------------------------------------------------------
echo ""
echo "--- Bootstrapping Gradle wrapper ($GRADLE_VERSION) ---"

# Resolve a Gradle launcher: use one on PATH if present, otherwise download the
# Gradle 8.2 distribution (the same zip the wrapper itself would fetch) into a
# user cache. No admin rights or package-manager Gradle package required.
ensure_gradle() {
  if command -v gradle >/dev/null 2>&1; then command -v gradle; return 0; fi
  local dist_dir="$HOME/.beam-gradle"
  local launcher="$dist_dir/gradle-$GRADLE_VERSION/bin/gradle"
  if [ ! -x "$launcher" ]; then
    mkdir -p "$dist_dir"
    local zip="$dist_dir/gradle-$GRADLE_VERSION-bin.zip"
    echo "  Downloading Gradle $GRADLE_VERSION..." >&2
    curl -fsSL "https://services.gradle.org/distributions/gradle-$GRADLE_VERSION-bin.zip" -o "$zip"
    unzip -q -o "$zip" -d "$dist_dir"
  fi
  echo "$launcher"
}

GRADLE_BIN="$(ensure_gradle)"
for proj in "$NOTIF_PROJ" ; do
  if [ -f "$proj/gradlew" ] && [ -f "$proj/gradle/wrapper/gradle-wrapper.jar" ]; then
    echo "  Wrapper already present in $(basename "$proj")"
  else
    echo "  Generating wrapper in $(basename "$proj")..."
    ( cd "$proj" && JAVA_HOME="$JAVA_HOME_RESOLVED" MSYS_NO_PATHCONV=1 sh "$GRADLE_BIN" wrapper --gradle-version "$GRADLE_VERSION" )
  fi
done

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
echo ""
echo "Setup complete. Run ./dev-native.sh to build native artifacts (Android AAR + iOS xcframework on macOS) and copy them into the client."
