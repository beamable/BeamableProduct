#!/bin/bash

echo Branch is $BRANCH

echo "--PWD---"
pwd

echo "moving up"
cd ..
pwd

echo "creating new dir"
mkdir -p changlog-upload-dir
cd changelog-upload-dir

git --version
git init
git config user.email "chris@beamable.com"
git config user.name "gh-actions"
git remote add --fetch origin https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/Changelogs.git
git remote set-url origin https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/Changelogs.git
git checkout $BRANCH
git status
cp -f ../client/Packages/com.beamable/CHANGELOG.md ./com-beamable-changelog.md
cp -f ../client/Packages/com.beamable.server/CHANGELOG.md ./com-beamable-server-changelog.md
cp -f ../cli/cli/CHANGELOG.md ./beamable-tools-changelog.md || true
git add .
git status
git diff-index --quiet HEAD || git commit -m "updating changelogs for ' + $VERSION + '"
sh "git push --set-upstream --force origin $BRANCH"