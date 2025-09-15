#!/bin/bash

echo Branch is $BRANCH
echo Should Copy Unity SDK=$COPY_UNITY_SDK
echo Should Copy CLI=$COPY_CLI
echo Should Copy Web SDK=$COPY_WEB_SDK

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

if [ "$COPY_UNITY_SDK" = "true" ]
then 
cp -f ../BeamableProduct/BeamableProduct/client/Packages/com.beamable/CHANGELOG.md ./com-beamable-changelog.md
cp -f ../BeamableProduct/BeamableProduct/client/Packages/com.beamable.server/CHANGELOG.md ./com-beamable-server-changelog.md
fi

if [ "$COPY_CLI" = "true" ]
then 
cp -f ../BeamableProduct/BeamableProduct/cli/cli/CHANGELOG.md ./beamable-tools-changelog.md || true
cp -f ../BeamableProduct/BeamableProduct/microservice/microservice/CHANGELOG.md ./beamable-server-changelog.md || true
fi 

if [ "$COPY_WEB_SDK" = "true" ]
then 
cp -f ../../BeamableProduct/BeamableProduct/web/CHANGELOG.md ./beamable-web-sdk-changelog.md || true
fi

git add .
git status
git diff-index --quiet HEAD || git commit -m "updating changelogs for $VERSION"
git push --set-upstream --force origin $BRANCH