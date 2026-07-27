#!/bin/bash

# Run this ONCE before starting a local web-dev session, or any time you want a clean slate.
#
# Starts the local npm registry (Verdaccio) and CDN file server (local-unpkg) from
# portal-localdev/, wiping any previously published packages.
#
# Thin wrapper around `beam web reset` — see WEB_LOCAL_DEV.md for the full guide.
#
# Note: unlike older versions, this does NOT write an @beamable registry override into your global
# npm config, so nothing here changes machine-wide npm behaviour. A project opts in by pinning a
# 0.0.123 version (which `beam web use` writes), and the CLI routes those installs at the local
# registry on its own.
#
# Extra arguments pass straight through, e.g.:
#   ./setup-web.sh --keep-caches

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/scripts/beam-cli.sh"

echo ""
echo "=== Beamable Web Local Dev Setup ==="
echo ""

beam_cli "$SCRIPT_DIR" web reset --product-dir "$SCRIPT_DIR" "$@"

echo ""
echo "Setup complete. Run ./dev-web.sh to build and publish the local web packages."
