#!/bin/bash

echo "--PWD---"
pwd
ls

echo "moving up"
cd ../..
pwd
ls

WEB_SDK_SAMPLE_BUILD_DIR="web-sdk-sample-build"
echo "creating new dir"
mkdir -p $WEB_SDK_SAMPLE_BUILD_DIR

echo "listing"
ls

cd $WEB_SDK_SAMPLE_BUILD_DIR
echo "moving into dir"
pwd

git --version
git init
git config user.email "chris@beamable.com"
git config user.name "gh-actions"
git remote add --fetch origin https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/web-sdk-sample.git
git remote set-url origin https://$GITHUB_USERNAME:$GITHUB_PASSWORD@github.com/beamable/web-sdk-sample.git
git checkout main
git status

cp -f ../../BeamableProduct/BeamableProduct/web/samples/WordWiz/dist ./dist || true

git add .
git status
git diff-index --quiet HEAD || git commit -m "updating web sdk sample build"
git push --set-upstream --force origin main