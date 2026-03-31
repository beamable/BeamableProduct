#!/bin/bash

# This script should be run ONCE before starting a local web-dev session,
# or any time you want a clean slate (wipes all previously published packages).
#
# It will:
#   1. Start the local npm registry (Verdaccio) and CDN file server (local-unpkg)
#      via Docker Compose (see portal-localdev/).
#   2. Reset the web build number back to 0.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCALDEV_DIR="$SCRIPT_DIR/portal-localdev"
WEB_BUILD_NUMBER_FILE="$SCRIPT_DIR/web-build-number.txt"

echo "=== Beamable Web Local Dev Setup ==="

# ---------------------------------------------------------------------------
# Start / restart the local stack
# ---------------------------------------------------------------------------
echo ""
echo "Starting local registry and CDN server..."
docker compose -f "$LOCALDEV_DIR/docker-compose.yml" down -v  # wipe old packages
docker compose -f "$LOCALDEV_DIR/docker-compose.yml" up -d

echo "Verdaccio   → http://localhost:4873"
echo "local-unpkg → http://localhost:4874"

# ---------------------------------------------------------------------------
# Reset web build number
# ---------------------------------------------------------------------------
echo ""
echo "Resetting web build number to 0"
echo 0 > "$WEB_BUILD_NUMBER_FILE"

# ---------------------------------------------------------------------------
# Configure global npm/pnpm registry
# ---------------------------------------------------------------------------
echo ""
echo "Pointing @beamable/* packages to local Verdaccio..."
npm config set @beamable:registry http://localhost:4873
npm config set //localhost:4873/:_authToken local
echo "  Projects resolving '@beamable/*' packages will use local Verdaccio."
echo "  All other packages continue to use the default npm registry."
echo "  Run ./teardown-web.sh to remove this configuration."

echo ""
echo "Setup complete. Run ./dev-web.sh to build and publish packages."
