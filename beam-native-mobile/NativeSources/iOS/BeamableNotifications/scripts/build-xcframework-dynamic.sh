#!/usr/bin/env bash
#
# Builds a DYNAMIC-framework XCFramework of the Swift core, for Unreal Engine.
#
# Why dynamic (vs. the static build used by Unity/RN): UE's PublicAdditionalFrameworks
# links with `-framework <Name>` and embeds a real `.framework` bundle. A static `.a`
# can't satisfy that, and statically linking a Swift library into UE means resolving the
# Swift runtime by hand. A dynamic framework is self-contained (iOS ships the Swift
# runtime), so UE just embeds and links it.
#
# Output: build/BeamableNotifications.xcframework  (dynamic; device + simulator)
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CORE="$ROOT/core"
BUILD="$ROOT/build"
SCHEME="BeamableNotifications"
MANIFEST="$CORE/Package.swift"

# Flip the product to dynamic for the duration of this build, then always restore it so
# the default (static, for Unity/RN) manifest is untouched.
cp "$MANIFEST" "$MANIFEST.bak"
restore() { mv "$MANIFEST.bak" "$MANIFEST"; }
trap restore EXIT
sed -i '' 's/type: .static,/type: .dynamic,/' "$MANIFEST"

rm -rf "$BUILD"
mkdir -p "$BUILD"

archive() {
  local destination="$1" archive_path="$2"
  ( cd "$CORE" && xcodebuild archive \
      -scheme "$SCHEME" \
      -destination "$destination" \
      -archivePath "$archive_path" \
      -derivedDataPath "$BUILD/dd-$(basename "$archive_path" .xcarchive)" \
      SKIP_INSTALL=NO \
      BUILD_LIBRARY_FOR_DISTRIBUTION=YES \
      | tail -2 ) 1>&2
}

echo "==> Archiving dynamic framework (device + simulator)"
archive "generic/platform=iOS" "$BUILD/ios.xcarchive"
archive "generic/platform=iOS Simulator" "$BUILD/sim.xcarchive"

FW_REL="Products/usr/local/lib/$SCHEME.framework"

echo "==> Creating dynamic XCFramework (reference; device + simulator)"
xcodebuild -create-xcframework \
  -framework "$BUILD/ios.xcarchive/$FW_REL" \
  -framework "$BUILD/sim.xcarchive/$FW_REL" \
  -output "$BUILD/BeamableNotifications.xcframework"

# UE's PublicAdditionalFrameworks does NOT consume an .xcframework. It expects a zip that
# unzips to <Name>.embeddedframework/<Name>.framework (a single dynamic framework). iOS
# device builds are arm64, so we package the device slice in that exact layout.
echo "==> Packaging UE embeddedframework zip (device arm64)"
EMB="$BUILD/BeamableNotifications.embeddedframework"
rm -rf "$EMB"; mkdir -p "$EMB"
cp -R "$BUILD/ios.xcarchive/$FW_REL" "$EMB/"
( cd "$BUILD" && rm -f BeamableNotifications.embeddedframework.zip \
    && zip -rq BeamableNotifications.embeddedframework.zip BeamableNotifications.embeddedframework )

rm -rf "$BUILD"/dd-* "$BUILD"/*.xcarchive "$EMB"

echo ""
echo "==> Done."
echo "    UE artifact: $BUILD/BeamableNotifications.embeddedframework.zip  (device arm64)"
echo "      -> copy to <plugin>/ThirdParty/BeamableNotifications.embeddedframework.zip"
echo "    Reference  : $BUILD/BeamableNotifications.xcframework            (device + sim)"
echo "    Note: the UE zip is device-only. For an iOS Simulator build, repackage the"
echo "          ios-arm64_x86_64-simulator slice from the xcframework the same way."
