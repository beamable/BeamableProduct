#!/bin/bash

# PREREQ:
#   Run ./setup-web.sh at least once before running this script.
#
# This script will be run many times as you develop web packages locally.
# Each run:
#   1. Increments the build number (stored in web-build-number.txt)
#   2. Builds and publishes packages as version 0.0.123-local<build_number>
#      to the local Verdaccio registry (http://localhost:4873)
#   3. Restarts local-unpkg to bust its in-memory file cache
#
# By default both the webSDK (@beamable/sdk) and toolkit (@beamable/portal-toolkit)
# are built and published. Use --skip-sdk to publish the toolkit only.
#
# When --skip-sdk is used, the toolkit's package.json is left untouched —
# the developer is responsible for the declared @beamable/sdk version.
# When the SDK is also published, both peer and dev dependency are updated to
# the local version so the Portal loads both from localhost.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_SDK_DIR="$SCRIPT_DIR/web"
TOOLKIT_DIR="$SCRIPT_DIR/beam-portal-toolkit"
LOCALDEV_DIR="$SCRIPT_DIR/portal-localdev"
WEB_BUILD_NUMBER_FILE="$SCRIPT_DIR/web-build-number.txt"
REGISTRY="http://localhost:4873"

# ---------------------------------------------------------------------------
# Cleanup trap — restores package.json files even if the script exits early
# ---------------------------------------------------------------------------
SDK_BACKUP=false
TOOLKIT_BACKUP=false

cleanup() {
  local exit_code=$?
  if [ "$SDK_BACKUP" = true ]; then
    echo "  Restoring web/package.json..."
    cp "$WEB_SDK_DIR/package.json.devbak" "$WEB_SDK_DIR/package.json" 2>/dev/null || true
    rm -f "$WEB_SDK_DIR/package.json.devbak"
  fi
  if [ "$TOOLKIT_BACKUP" = true ]; then
    echo "  Restoring beam-portal-toolkit/package.json..."
    cp "$TOOLKIT_DIR/package.json.devbak" "$TOOLKIT_DIR/package.json" 2>/dev/null || true
    rm -f "$TOOLKIT_DIR/package.json.devbak"
  fi
  exit $exit_code
}
trap cleanup EXIT

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

VERSION="0.0.123-local$NEXT_BUILD_NUMBER"
PREVIOUS_VERSION="0.0.123-local$PREVIOUS_BUILD_NUMBER"

echo ""
echo "=== Beamable Web Local Dev ==="
echo "Publishing version: $VERSION"
echo "Registry:           $REGISTRY"
echo ""

# ---------------------------------------------------------------------------
# Publish webSDK
# ---------------------------------------------------------------------------
if [ "$SKIP_SDK" = false ]; then
  echo "--- Building @beamable/sdk ---"
  cd "$WEB_SDK_DIR"

  cp package.json package.json.devbak
  SDK_BACKUP=true

  echo "  [cmd] pnpm install"
  pnpm install
  echo "  [cmd] pnpm version $VERSION --no-git-tag-version"
  pnpm version "$VERSION" --no-git-tag-version
  echo "  [cmd] pnpm build"
  pnpm build
  echo "  [cmd] pnpm publish --registry $REGISTRY --no-git-checks"
  pnpm publish --registry "$REGISTRY" --no-git-checks

  cp package.json.devbak package.json && rm package.json.devbak
  SDK_BACKUP=false

  echo "Published @beamable/sdk@$VERSION"
  cd "$SCRIPT_DIR"
fi

# ---------------------------------------------------------------------------
# Publish toolkit
# ---------------------------------------------------------------------------
echo ""
echo "--- Building @beamable/portal-toolkit ---"
cd "$TOOLKIT_DIR"

cp package.json package.json.devbak
TOOLKIT_BACKUP=true

# When the SDK is also published this run, update both devDependencies and
# peerDependencies to the local version BEFORE pnpm install, so pnpm resolves
# @beamable/sdk from Verdaccio correctly.
# When --skip-sdk is passed, package.json is left untouched — the developer
# is responsible for ensuring the declared version is available.
if [ "$SKIP_SDK" = false ]; then
  node -e "
    const fs = require('fs');
    const pkg = JSON.parse(fs.readFileSync('package.json', 'utf8'));
    pkg.peerDependencies['@beamable/sdk'] = '$VERSION';
    pkg.devDependencies['@beamable/sdk'] = '$VERSION';
    fs.writeFileSync('package.json', JSON.stringify(pkg, null, 2) + '\n');
  "
  echo "  Updated @beamable/sdk → $VERSION"
fi

# Evict any cached @beamable/sdk tarball from the pnpm content-addressable store.
# After a Verdaccio wipe the same version is republished with a different hash,
# so the cached tarball causes ERR_PNPM_TARBALL_INTEGRITY on the next install.
rm -f pnpm-lock.yaml
pnpm store delete @beamable/sdk 2>/dev/null || true
echo "  [cmd] pnpm install"
pnpm install
echo "  [cmd] pnpm version $VERSION --no-git-tag-version"
pnpm version "$VERSION" --no-git-tag-version

echo "  [cmd] pnpm build"
pnpm build
echo "  [cmd] pnpm publish --registry $REGISTRY --no-git-checks"
pnpm publish --registry "$REGISTRY" --no-git-checks

cp package.json.devbak package.json && rm package.json.devbak
TOOLKIT_BACKUP=false

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
