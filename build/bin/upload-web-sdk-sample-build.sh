#!/bin/bash

# Exit immediately if any command fails
set -e

# Use GitHub workspace if available, otherwise use current directory
workspace="${GITHUB_WORKSPACE:-.}"

# Set Up Paths
build_directory="$workspace/web-sdk-sample-build"
github_repo_url="https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/web-sdk-sample.git"
source_dist_folder="$workspace/$SAMPLE_DIR/dist"

# Validate Source Files Exist
if [[ ! -d "$source_dist_folder" ]]; then
  echo "Error: Expected dist folder not found at $source_dist_folder" >&2
  exit 1
fi

echo "Preparing build directory..."

# Create fresh build directory
mkdir -p "$build_directory"

# Move into build directory
cd "$build_directory"

echo "Setting up git repository..."

git init
git config user.email "github-actions[bot]@users.noreply.github.com"
git config user.name "github-actions[bot]"
git remote add origin "$github_repo_url"

# Fetch existing main branch or create new one
echo "Checking for existing repository..."
if git ls-remote --exit-code origin main &>/dev/null; then
  echo "Found existing main branch, fetching..."
  git fetch origin main --depth=1
  git checkout -B main origin/main
else
  echo "No existing main branch found, creating new one..."
  git checkout -B main
fi

echo "Copying new files..."

cp -a "$source_dist_folder"/. .

echo "Checking for changes..."

# Stage all changes
git add --all
git status

# Check if there are any changes to commit
if git diff --cached --quiet; then
  echo "No changes detected - nothing to commit"
else
  echo "Changes detected, committing and pushing..."
  git commit -m "Update web sdk sample build"
  git push --set-upstream --force origin main
  echo "Successfully deployed changes"
fi

echo "Done!"