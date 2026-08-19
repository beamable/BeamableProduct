#!/bin/bash

# PREREQ:
#   Run ./setup-web.sh at least once before running this script (or have the registry running via
#   `beam local up`, when initialized with --with-web-registry).
#
# Run this many times as you develop the web packages locally. Each run:
#   1. Builds @beamable/sdk and @beamable/portal-toolkit (tsdown; the toolkit also regenerates its
#      component bindings first). Pass --skip-build to publish as-is.
#   2. Publishes BOTH as version 0.0.123 to the local Verdaccio registry (http://localhost:4873),
#      pointing the toolkit's @beamable/sdk peer dependency at it too — the Portal reads that peer dep
#      to decide which SDK to load, so they have to agree. 0.0.123 is the shared "developer build"
#      sentinel; dev.sh uses the same base for the .NET packages.
#   3. Flushes the local CDN's file cache, since the version it already cached hasn't changed name.
#   4. Refreshes the projects that consume it (`beam web use`) — which force-reinstalls, because npm
#      would otherwise see 0.0.123 already installed and do nothing.
#
# Thin wrapper around `beam web publish` + `beam web use` — see web/LOCAL_DEV.md for the full guide.
#
# ⚠️  Step 4 pins 0.0.123 in every extension it finds, which edits package.json and lock files. Because
#     the version never changes this is a ONE-TIME edit, not per run — but it is tracked files, so it
#     must not be committed. At the end of a session:
#
#       git restore '**/package.json' '**/package-lock.json'
#
# BUILD FLAGS:
#   --build        Full build: runs 'pnpm install' in both packages before building. Use after the
#                  packages' own dependencies changed (a normal run skips the install, since it only
#                  matters when node_modules is missing or out of date). Slower.
#   --skip-build   Publish whatever is already in dist/ without rebuilding.
#
# BEAM_WORKSPACE (optional) — the repo holding your extensions, where step 4 runs. Defaults to this
# repo, which is only right if you're developing against its own extensions:
#
#   BEAM_WORKSPACE=/path/to/agentic-portal ./dev-web.sh
#
# Extra arguments pass straight through to `beam web publish`, e.g.:
#   ./dev-web.sh --only sdk
#   ./dev-web.sh --build --only toolkit
#   ./dev-web.sh --version 1.2.3-mine          # publish under a different version entirely
#
# Set BEAM_SKIP_UPDATE=1 to publish without repointing any extension.
# Set BEAM_FULL_BUILD=1 as an alternative to passing --build.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/scripts/beam-cli.sh"

PUBLISH_ARGS=(web publish --product-dir "$SCRIPT_DIR")

# Translate the friendly --build alias (and BEAM_FULL_BUILD=1) into the CLI's --force-install, and
# pass everything else through untouched.
PASSTHROUGH=()
FULL_BUILD="$BEAM_FULL_BUILD"
for arg in "$@"; do
  if [ "$arg" = "--build" ]; then
    FULL_BUILD=1
  else
    PASSTHROUGH+=("$arg")
  fi
done

if [ -n "$FULL_BUILD" ]; then
  PUBLISH_ARGS+=(--force-install)
  echo "Full build requested — dependencies will be reinstalled before building."
fi

echo ""
echo "=== Beamable Web Local Dev ==="
echo ""

beam_cli "$SCRIPT_DIR" "${PUBLISH_ARGS[@]}" "${PASSTHROUGH[@]}"

if [ -n "$BEAM_SKIP_UPDATE" ]; then
  echo ""
  echo "BEAM_SKIP_UPDATE set — your extensions still have the PREVIOUS build installed."
  echo "Run 'beam web use' in the repo holding them to pick this one up."
else
  WORKSPACE="${BEAM_WORKSPACE:-$SCRIPT_DIR}"
  echo ""
  echo "--- Refreshing extensions in [$WORKSPACE] ---"
  # `beam web use` force-reinstalls: the pin is already 0.0.123 after the first run, so a plain install
  # would consider the tree satisfied and never fetch what we just published.
  #
  # Deliberately not `beam portal extension update-toolkit --local`, which does the same pin rewrite but
  # discovers extensions via the Beamo manifest — that makes it authenticate against the configured host,
  # so it fails when the backend is down even though this is a purely local file operation.
  beam_cli "$SCRIPT_DIR" web use --workspace "$WORKSPACE"
fi

echo ""
echo "Done. The Portal needs no configuration — it recognises the 0.0.123 version and loads it from the"
echo "local CDN automatically. Hard-reload it to drop its in-memory module cache."
echo "At the end of your session:  git restore '**/package.json' '**/package-lock.json'"
