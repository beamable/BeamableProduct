#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
WORKSPACE_DIR="${GITHUB_WORKSPACE:-$REPO_ROOT}"

: "${SAMPLE_DIR:?SAMPLE_DIR environment variable is required}"
: "${GITHUB_USERNAME:?GITHUB_USERNAME environment variable is required}"
: "${GITHUB_PASSWORD:?GITHUB_PASSWORD environment variable is required}"

WEB_SDK_SAMPLE_BUILD_DIR="$WORKSPACE_DIR/web-sdk-sample-build"
REMOTE_URL="https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/web-sdk-sample.git"
DIST_SOURCE="$WORKSPACE_DIR/$SAMPLE_DIR/dist"

if [[ ! -d "$DIST_SOURCE" ]]; then
  echo "Expected dist folder not found at $DIST_SOURCE" >&2
  exit 1
fi

rm -rf "$WEB_SDK_SAMPLE_BUILD_DIR"
mkdir -p "$WEB_SDK_SAMPLE_BUILD_DIR"
cd "$WEB_SDK_SAMPLE_BUILD_DIR"

git init
git config user.email "github-actions[bot]@users.noreply.github.com"
git config user.name "github-actions[bot]"
git remote add origin "$REMOTE_URL"

if git ls-remote --exit-code origin main >/dev/null 2>&1; then
  git fetch origin main --depth=1
  git checkout -B main origin/main
else
  git checkout -B main
fi

find . -mindepth 1 -maxdepth 1 ! -name '.git' -exec rm -rf {} +

cp -a "$DIST_SOURCE"/. "$WEB_SDK_SAMPLE_BUILD_DIR"

git add --all

if git diff --cached --quiet; then
  echo "No changes to commit"
else
  git commit -m "Update web sdk sample build"
  git push --force-with-lease origin main
fi
