#!/bin/bash

# Reverses setup-web.sh: stops the local registry and CDN, and deletes the packages published to it.
#
# Thin wrapper around `beam web stop --wipe` — see WEB_LOCAL_DEV.md for the full guide.
#
# There is no global npm config to restore: this flow never wrote one. What DOES need undoing is the
# toolkit pin that `dev-web.sh` wrote into your extensions — that lives in tracked files, so revert it
# with git in the repo holding them:
#
#   git restore '**/package.json' '**/package-lock.json'
#   npm install     # in any extension you actually ran, to restore the published toolkit
#
# Pass --keep-packages to stop the containers without deleting what was published:
#   ./teardown-web.sh --keep-packages

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/scripts/beam-cli.sh"

STOP_ARGS=(web stop --product-dir "$SCRIPT_DIR")
KEEP_PACKAGES=""
PASSTHROUGH=()

for arg in "$@"; do
  if [ "$arg" = "--keep-packages" ]; then
    KEEP_PACKAGES=1
  else
    PASSTHROUGH+=("$arg")
  fi
done

if [ -z "$KEEP_PACKAGES" ]; then
  STOP_ARGS+=(--wipe)
fi

echo ""
echo "=== Beamable Web Local Dev Teardown ==="
echo ""

beam_cli "$SCRIPT_DIR" "${STOP_ARGS[@]}" "${PASSTHROUGH[@]}"

echo ""
echo "Teardown complete."
echo "If a project still has a locally-linked package in node_modules, 'npm install' there restores the published one."
