#!/bin/bash

echo Branch is $BRANCH

echo "--PWD---"
pwd
ls

echo "moving up"
cd ../..
pwd
ls

CHANGELOG_DIR=changlog-upload-dir
echo "creating new dir"
mkdir -p $CHANGELOG_DIR

echo "listing"
ls

cd $CHANGELOG_DIR
echo "moving into dir"
pwd

git --version
git init
git config user.email "chris@beamable.com"
git config user.name "gh-actions"
git remote add --fetch origin https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/Changelogs.git
git remote set-url origin https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/Changelogs.git
git checkout $BRANCH
git status
cp -f ../BeamableProduct/BeamableProduct/client/Packages/com.beamable/CHANGELOG.md ./com-beamable-changelog.md
cp -f ../BeamableProduct/BeamableProduct/client/Packages/com.beamable.server/CHANGELOG.md ./com-beamable-server-changelog.md
cp -f ../BeamableProduct/BeamableProduct/cli/cli/CHANGELOG.md ./beamable-tools-changelog.md || true
git add .
git status
git diff-index --quiet HEAD || git commit -m "updating changelogs for ' + $VERSION + '"
git push --set-upstream --force origin $BRANCH