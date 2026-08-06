#!/bin/bash

# Reverses the changes made by setup-web.sh:
#   1. Restores the global npm/pnpm registry to the default
#   2. Stops and wipes the local Docker stack

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCALDEV_DIR="$SCRIPT_DIR/portal-localdev"

echo "=== Beamable Web Local Dev Teardown ==="

# ---------------------------------------------------------------------------
# Restore global npm/pnpm registry
# ---------------------------------------------------------------------------
echo ""
echo "Removing @beamable/* registry override..."
npm config delete @beamable:registry
npm config delete //localhost:4873/:_authToken
echo "  @beamable/* packages will now resolve from the default npm registry."

# ---------------------------------------------------------------------------
# Stop local stack
# ---------------------------------------------------------------------------
echo ""
echo "Stopping local registry and CDN server..."
docker compose -f "$LOCALDEV_DIR/docker-compose.yml" down -v

echo ""
echo "Teardown complete."
