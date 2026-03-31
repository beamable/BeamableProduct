#!/bin/bash

# PREREQ:
#   Run ./setup-web.sh at least once before running this script.
#
# This script will be run many times as you develop web packages locally.
# Each run:
#   1. Increments the build number (stored in web-build-number.txt)
#   2. Builds and publishes packages as version 0.0.123.<build_number>
#      to the local Verdaccio registry (http://localhost:4873)
#   3. Restarts local-unpkg to bust its in-memory file cache
#
# By default both the webSDK (beamable-sdk) and toolkit (@beamable/portal-toolkit)
# are built and published. Use --skip-sdk to publish the toolkit only.
#
# When --skip-sdk is used the toolkit's peerDependencies.beamable-sdk is left
# unchanged, so the Portal will load that SDK version from the real CDN.
# When the SDK is also published the toolkit's peer dependency is updated to the
# local version so the Portal loads both from localhost.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_SDK_DIR="$SCRIPT_DIR/web"
TOOLKIT_DIR="$SCRIPT_DIR/beam-portal-toolkit"
LOCALDEV_DIR="$SCRIPT_DIR/portal-localdev"
WEB_BUILD_NUMBER_FILE="$SCRIPT_DIR/web-build-number.txt"
REGISTRY="http://localhost:4873"

# ---------------------------------------------------------------------------
# Flags
# ---------------------------------------------------------------------------
SKIP_SDK=false

while test $# -gt 0; do
  case "$1" in
    --skip-sdk)
      SKIP_SDK=true
      echo "Skipping webSDK build — toolkit will reference its current peerDependency version"
      ;;
    *)
      echo "Unknown argument: $1"
      ;;
  esac
  shift
done

# ---------------------------------------------------------------------------
# Build number
# ---------------------------------------------------------------------------
if [ ! -f "$WEB_BUILD_NUMBER_FILE" ]; then
  echo "web-build-number.txt not found. Run ./setup-web.sh first."
  exit 1
fi

NEXT_BUILD_NUMBER=$(cat "$WEB_BUILD_NUMBER_FILE")
PREVIOUS_BUILD_NUMBER=$NEXT_BUILD_NUMBER
((NEXT_BUILD_NUMBER += 1))
echo $NEXT_BUILD_NUMBER > "$WEB_BUILD_NUMBER_FILE"

VERSION="0.0.123.$NEXT_BUILD_NUMBER"
PREVIOUS_VERSION="0.0.123.$PREVIOUS_BUILD_NUMBER"

echo ""
echo "=== Beamable Web Local Dev ==="
echo "Publishing version: $VERSION"
echo "Registry:           $REGISTRY"
echo ""

# ---------------------------------------------------------------------------
# Publish webSDK
# ---------------------------------------------------------------------------
if [ "$SKIP_SDK" = false ]; then
  echo "--- Building beamable-sdk ---"
  cd "$WEB_SDK_DIR"

  # Temporarily set the version, build, publish, then restore
  SAVED_SDK_VERSION=$(node -p "require('./package.json').version")
  pnpm version "$VERSION" --no-git-tag-version
  pnpm build
  pnpm publish --registry "$REGISTRY" --no-git-checks

  # Restore the original version
  pnpm version "$SAVED_SDK_VERSION" --no-git-tag-version

  echo "Published beamable-sdk@$VERSION"
  cd "$SCRIPT_DIR"
fi

# ---------------------------------------------------------------------------
# Publish toolkit
# ---------------------------------------------------------------------------
echo ""
echo "--- Building @beamable/portal-toolkit ---"
cd "$TOOLKIT_DIR"

# Temporarily set the toolkit version (and optionally peerDep), then restore
SAVED_TOOLKIT_VERSION=$(node -p "require('./package.json').version")
SAVED_TOOLKIT_PEER=$(node -p "require('./package.json').peerDependencies['beamable-sdk']")
pnpm version "$VERSION" --no-git-tag-version

# If the SDK was also published locally, update the peer dependency to match
# so the Portal knows to load both from localhost.
if [ "$SKIP_SDK" = false ]; then
  node -e "
    const fs = require('fs');
    const pkg = JSON.parse(fs.readFileSync('package.json', 'utf8'));
    pkg.peerDependencies['beamable-sdk'] = '$VERSION';
    fs.writeFileSync('package.json', JSON.stringify(pkg, null, 2) + '\n');
  "
  echo "Updated peerDependencies.beamable-sdk → $VERSION"
fi

pnpm build
pnpm publish --registry "$REGISTRY" --no-git-checks

# Restore version and peerDep to their original values
pnpm version "$SAVED_TOOLKIT_VERSION" --no-git-tag-version
node -e "
  const fs = require('fs');
  const pkg = JSON.parse(fs.readFileSync('package.json', 'utf8'));
  pkg.peerDependencies['beamable-sdk'] = '$SAVED_TOOLKIT_PEER';
  fs.writeFileSync('package.json', JSON.stringify(pkg, null, 2) + '\n');
"

echo "Published @beamable/portal-toolkit@$VERSION"
cd "$SCRIPT_DIR"

# ---------------------------------------------------------------------------
# Restart local-unpkg to clear the in-memory file cache
# ---------------------------------------------------------------------------
echo ""
echo "--- Restarting local-unpkg (clearing file cache) ---"
docker compose -f "$LOCALDEV_DIR/docker-compose.yml" restart local-unpkg

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
echo ""
echo "Done. Set ToolkitVersion: \"$VERSION\" in your extension manifest and run the Portal."
