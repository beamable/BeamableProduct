#!/usr/bin/env bash
#
# Builds the BeamableNotifications Swift core into a static XCFramework that Unity,
# Unreal, and React Native all link against. Static linking is required so Unity's
# `[DllImport("__Internal")]` resolves the @_cdecl symbols inside the app binary.
#
# Output: build/BeamableNotifications.xcframework
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CORE="$ROOT/core"
BUILD="$ROOT/build"
SCHEME="BeamableNotifications"
HEADER="$CORE/include/BeamableNotifications.h"

rm -rf "$BUILD"
mkdir -p "$BUILD"

# Archive one platform into its OWN derivedData (sharing it clobbers the other slice),
# then convert the emitted object file into a static library and stage the Swift module.
# Args: <destination> <slug> <release-subdir>
build_slice() {
  local destination="$1" slug="$2" releaseDir="$3"
  local dd="$BUILD/dd-$slug"
  local archive="$BUILD/$slug.xcarchive"

  # All progress goes to stderr so the captured stdout is ONLY the headers path.
  ( cd "$CORE" && xcodebuild archive \
      -scheme "$SCHEME" \
      -destination "$destination" \
      -archivePath "$archive" \
      -derivedDataPath "$dd" \
      SKIP_INSTALL=NO \
      BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
      | tail -2 ) 1>&2

  # SPM archives a single merged object file; wrap it into a .a static library.
  local obj
  obj="$(find "$archive/Products" -name '*.o' | head -1)"
  local headers="$BUILD/headers-$slug"
  mkdir -p "$headers"
  libtool -static -o "$headers/lib${SCHEME}.a" "$obj" 1>&2

  # Stage the C ABI header and the generated Swift module interface alongside the lib.
  cp "$HEADER" "$headers/"
  local swiftmodule
  swiftmodule="$(find "$dd" -path "*$releaseDir*" -name "$SCHEME.swiftmodule" -type d | head -1)"
  cp -R "$swiftmodule" "$headers/"

  printf '%s' "$headers"
}

echo "==> Building device slice"
IOS_HEADERS="$(build_slice 'generic/platform=iOS' 'ios' 'Release-iphoneos')"

echo "==> Building simulator slice"
SIM_HEADERS="$(build_slice 'generic/platform=iOS Simulator' 'sim' 'Release-iphonesimulator')"

echo "==> Creating XCFramework"
xcodebuild -create-xcframework \
  -library "$IOS_HEADERS/lib${SCHEME}.a" -headers "$IOS_HEADERS" \
  -library "$SIM_HEADERS/lib${SCHEME}.a" -headers "$SIM_HEADERS" \
  -output "$BUILD/BeamableNotifications.xcframework"

# Remove build intermediates, leaving only the xcframework.
rm -rf "$BUILD"/dd-* "$BUILD"/*.xcarchive "$BUILD"/headers-*

echo ""
echo "==> Done: $BUILD/BeamableNotifications.xcframework"
echo "    Unity : copy into unity/Plugins/iOS/"
echo "    Unreal: zip it -> unreal/ThirdParty/BeamableNotifications.embeddedframework.zip"
echo "    RN    : copy into reactnative/ios/Frameworks/"
