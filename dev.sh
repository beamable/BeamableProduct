#!/bin/bash

# PREREQ: 
#  Ensure you've run `setup.sh` at least once before running this

# This script will be run many times as you develop.
# Anytime you need to test a change with new code you've written
# locally, you should run this script.
#
# It will...
#  1. build all the projects that result in Nuget packages, 
#  2. publish them locally to the local package feed
#  3. update the local templates
#  4. install the latest CLI globally 
#  5. invalidate the nuget cache for local beamable dev packages, which
#     means that downstream projects will need to run a `dotnet restore`.

echo "Hello, you stalwart Beamable"

VERSION=0.0.123
FEED_NAME=BeamableNugetSource
# the build solution only has references to the projects that we actually want to publish
SOLUTION=./build/LocalBuild/LocalBuild.sln
TMP_BUILD_OUTPUT="TempBuild"

RESTORE_ARGS="-p:Warn=0"
BUILD_ARGS="--configuration Release -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION --no-dependencies -p:Warn=0" #-
PACK_ARGS="--configuration Release --no-build -o $TMP_BUILD_OUTPUT -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION"
PUSH_ARGS="--source $FEED_NAME"

dotnet restore $SOLUTION $RESTORE_ARGS
dotnet build $SOLUTION $BUILD_ARGS
dotnet pack $SOLUTION $PACK_ARGS
dotnet nuget push $TMP_BUILD_OUTPUT/*.$VERSION.nupkg $PUSH_ARGS

# install the latest templates.
#  frustratingly, it seems like the version of the install command
#  that accepts a nuget feed does not work for local package feeds. 
dotnet new install $TMP_BUILD_OUTPUT/Beamable.Templates.$VERSION.nupkg --force

# clean up the build artificats... 
rm -rf $TMP_BUILD_OUTPUT

# remove the cache keys for beamable projects. 
rm -rf $HOME/.nuget/packages/beamable.*/$VERSION

# install the latest CLI globally.
dotnet tool install Beamable.Tools --version $VERSION --global --allow-downgrade --no-cache