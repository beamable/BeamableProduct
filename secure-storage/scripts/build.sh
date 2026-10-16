#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LIBRARY_PROJECT="$PROJECT_ROOT/src/Beamable.SecureStorage/Beamable.SecureStorage.csproj"
OUTPUT_ROOT="$PROJECT_ROOT/artifacts"
UNITY_PLUGINS="$OUTPUT_ROOT/unity/Plugins"
SDK_PATCH="$OUTPUT_ROOT/sdk-patch/Packages/com.beamable"
SDK_SOURCE="$PROJECT_ROOT/../client/Packages/com.beamable"

dotnet build "$LIBRARY_PROJECT" -c Release --nologo
dotnet build "$LIBRARY_PROJECT" -c Release --nologo -p:SecureStorageIOS=true -o "$OUTPUT_ROOT/managed-ios"

mkdir -p "$UNITY_PLUGINS/Desktop" "$UNITY_PLUGINS/iOS" "$OUTPUT_ROOT/unity/Editor"
mkdir -p "$OUTPUT_ROOT/unity/Analytics"
# Remove only the superseded generated adapters, which hide nonvirtual SDK methods.
rm -f "$OUTPUT_ROOT/unity/AnalyticsTracker.cs" "$OUTPUT_ROOT/unity/AnalyticsTracker.cs.meta" \
  "$OUTPUT_ROOT/unity/SecurePersistentBatchManager.cs" "$OUTPUT_ROOT/unity/SecurePersistentBatchManager.cs.meta"
cp "$PROJECT_ROOT/unity/Analytics/"*.cs "$PROJECT_ROOT/unity/Analytics/"*.meta "$OUTPUT_ROOT/unity/Analytics/"
cp "$PROJECT_ROOT/src/Beamable.SecureStorage/bin/Release/netstandard2.1/Beamable.SecureStorage.dll" "$UNITY_PLUGINS/Desktop/"
cp "$PROJECT_ROOT/src/Beamable.SecureStorage/bin/Release/netstandard2.1/Beamable.SecureStorage.xml" "$UNITY_PLUGINS/Desktop/"
cp "$OUTPUT_ROOT/managed-ios/Beamable.SecureStorage.dll" "$UNITY_PLUGINS/iOS/"
cp "$PROJECT_ROOT/unity/ManagedDesktop.dll.meta" "$UNITY_PLUGINS/Desktop/Beamable.SecureStorage.dll.meta"
cp "$PROJECT_ROOT/unity/ManagedIOS.dll.meta" "$UNITY_PLUGINS/iOS/Beamable.SecureStorage.dll.meta"
cp "$PROJECT_ROOT/native/apple/BeamableSecureStorage.c" "$UNITY_PLUGINS/iOS/"
cp "$PROJECT_ROOT/unity/SecureStorageIOSBuild.cs" "$OUTPUT_ROOT/unity/Editor/"

if [[ "$(uname -s)" == "Darwin" ]]; then
  mkdir -p "$OUTPUT_ROOT/native" "$UNITY_PLUGINS/macOS"
  xcrun clang -dynamiclib -arch arm64 -arch x86_64 -mmacosx-version-min=11.0 \
    -install_name @rpath/libBeamableSecureStorage.dylib \
    -Wall -Wextra -Werror -fvisibility=hidden -framework Security -framework CoreFoundation \
    "$PROJECT_ROOT/native/apple/BeamableSecureStorage.c" \
    -o "$OUTPUT_ROOT/native/libBeamableSecureStorage.dylib"
  cp "$OUTPUT_ROOT/native/libBeamableSecureStorage.dylib" "$UNITY_PLUGINS/macOS/"
  cp "$PROJECT_ROOT/unity/MacOS.dylib.meta" "$UNITY_PLUGINS/macOS/libBeamableSecureStorage.dylib.meta"
fi

# Consumers need these SDK changes before installing the opt-in secure backend.
SDK_FILES=(
  Runtime/Beam.cs
  Runtime/Core/Platform/SDK/Analytics/AnalyticsTracker.cs
  Runtime/Core/Platform/SDK/Analytics/AnalyticsDependencyRegistration.cs
  Runtime/Core/Platform/SDK/Analytics/Batch/BatchManager.cs
  Runtime/Core/Platform/SDK/Analytics/Batch/PersistentBatchManager.cs
  Runtime/Core/Platform/SDK/Analytics/Batch/IAnalyticsBatchStorage.cs
  Runtime/Core/Platform/SDK/Analytics/Batch/PlayerPrefsBatchStorage.cs
  Tests/Runtime/Beamable/AnalyticsBatchPersistenceTests.cs
)
for relative in "${SDK_FILES[@]}"; do
  mkdir -p "$SDK_PATCH/$(dirname "$relative")"
  cp "$SDK_SOURCE/$relative" "$SDK_SOURCE/$relative.meta" "$SDK_PATCH/$(dirname "$relative")/"
done
cp "$PROJECT_ROOT/README.md" "$OUTPUT_ROOT/README.md"
cp "$PROJECT_ROOT/REVIEW-FIXES.md" "$OUTPUT_ROOT/REVIEW-FIXES.md"

echo "Unity plug-ins: $OUTPUT_ROOT/unity"
echo "Required SDK source patch: $OUTPUT_ROOT/sdk-patch"
