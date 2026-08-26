#!/usr/bin/env bash
set -euo pipefail

VERSION="${VERSION:?VERSION environment variable is required (e.g. 1.0.0)}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CLI_CSPROJ="$SCRIPT_DIR/../cli/cli.csproj"
STAGING_DIR="$SCRIPT_DIR/staging"
OUTPUT_DIR="$SCRIPT_DIR/output"

RIDS=("win-x64" "osx-arm64" "osx-x64" "linux-x64")

if ! command -v mcpb &>/dev/null; then
  echo "Error: mcpb CLI not found. Install it with: npm install -g @anthropic-ai/mcpb"
  exit 1
fi

rm -rf "$STAGING_DIR" "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

for RID in "${RIDS[@]}"; do
  echo "=== Building $RID ==="
  STAGE="$STAGING_DIR/$RID"
  mkdir -p "$STAGE/server"

  dotnet publish "$CLI_CSPROJ" \
    -f net8.0 \
    -r "$RID" \
    -c Release \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:SKIP_GENERATION=true \
    -p:BeamBuild=true \
    -p:DebugType=none \
    -o "$STAGE/server/"

  # dotnet publish uses AssemblyName (Beamable.Tools), but mcpb entry_point expects "beam"
  if [[ "$RID" == win-* ]]; then
    mv "$STAGE/server/Beamable.Tools.exe" "$STAGE/server/beam.exe"
    BINARY_NAME="beam.exe"
  else
    mv "$STAGE/server/Beamable.Tools" "$STAGE/server/beam"
    chmod +x "$STAGE/server/beam"
    BINARY_NAME="beam"
  fi

  sed -e "s/VERSION_PLACEHOLDER/$VERSION/g" -e "s/BINARY_PLACEHOLDER/$BINARY_NAME/g" "$SCRIPT_DIR/manifest.json" > "$STAGE/manifest.json"
  cp "$SCRIPT_DIR/.mcpbignore" "$STAGE/.mcpbignore"
  [ -f "$SCRIPT_DIR/icon.png" ] && cp "$SCRIPT_DIR/icon.png" "$STAGE/icon.png"

  echo "=== Packing $RID ==="
  mcpb pack "$STAGE" "$OUTPUT_DIR/beamable-$RID.mcpb"
done

rm -rf "$STAGING_DIR"

echo ""
echo "=== Done ==="
ls -lh "$OUTPUT_DIR"
