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

# the .dev.env file hosts some common variables
source ./.dev.env

# construct a build number
NEXT_BUILD_NUMBER=`cat build-number.txt`
PREVIOUS_BUILD_NUMBER=$NEXT_BUILD_NUMBER
((NEXT_BUILD_NUMBER+=1))
echo $NEXT_BUILD_NUMBER > build-number.txt

# the version is the nuget package version that will be built
VERSION=0.0.123.$NEXT_BUILD_NUMBER
PREVIOUS_VERSION=0.0.123.$PREVIOUS_BUILD_NUMBER

# the build solution only has references to the projects that we actually want to publish
SOLUTION=./build/LocalBuild/LocalBuild.sln
TMP_BUILD_OUTPUT="TempBuild"

BUILD_ARGS="--configuration Release -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION -p:Warn=0" #-
PACK_ARGS="--configuration Release --no-build -o $TMP_BUILD_OUTPUT -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION -p:SKIP_GENERATION=true"
PUSH_ARGS="--source $FEED_NAME"

dotnet restore $SOLUTION
dotnet build $SOLUTION $BUILD_ARGS
dotnet build cli/beamable.common -f net8.0 -t:CopyCodeToUnity -p:BEAM_COPY_CODE_TO_UNITY=true
dotnet pack $SOLUTION $PACK_ARGS
dotnet nuget push $TMP_BUILD_OUTPUT/*.$VERSION.nupkg $PUSH_ARGS

# remove the old package from the nuget feed, so that we don't accumulate millions of packages over time. 
DELETE_ARGS="$PREVIOUS_VERSION --source $FEED_NAME --non-interactive"
echo "Deleting package! ------ $DELETE_ARGS"
dotnet nuget delete Beamable.Common $DELETE_ARGS
dotnet nuget delete Beamable.Server.Common $DELETE_ARGS
dotnet nuget delete Beamable.Microservice.Runtime $DELETE_ARGS
dotnet nuget delete Beamable.Microservice.SourceGen $DELETE_ARGS
dotnet nuget delete Beamable.Tooling.Common $DELETE_ARGS
dotnet nuget delete Beamable.UnityEngine $DELETE_ARGS
dotnet nuget delete Beamable.UnityEngine.Addressables $DELETE_ARGS
dotnet nuget delete Beamable.Tools $DELETE_ARGS


# install the latest templates.
#  frustratingly, it seems like the version of the install command
#  that accepts a nuget feed does not work for local package feeds. 
dotnet new uninstall Beamable.Templates || true
dotnet new install $TMP_BUILD_OUTPUT/Beamable.Templates.$VERSION.nupkg --force

# clean up the build artifacts... 
rm -rf $TMP_BUILD_OUTPUT

# remove the cache keys for beamable projects. This makes them break until a restore operation happens.
rm -rf $HOME/.nuget/packages/beamable.*/$PREVIOUS_VERSION

# install the latest CLI globally.
dotnet tool install Beamable.Tools --version $VERSION --global --allow-downgrade --no-cache

# generate unity CLI
beam generate-interface --engine unity --output=./client/Packages/com.beamable/Editor/BeamCli/Commands

# restore the nuget packages (and CLI) for a sample project, thus restoring the
# nuget-cache for all projects.
cd cli/beamable.templates/templates/BeamService
dotnet tool update Beamable.Tools --version $VERSION --allow-downgrade
dotnet restore BeamService.csproj  --no-cache --force

# Go back to the project root
cd ../../../..

# If the user has the Unreal repo as a sibling, we update the version number there too
if [[ -d "../UnrealSDK" ]]; then
  cd ../UnrealSDK
  dotnet tool update Beamable.Tools --version $VERSION --allow-downgrade
  cd Microservices
  for i in `find . -name "*.csproj" -type f`; do
    echo "Restoring Microservice Project: $i"
    dotnet restore "$i" --no-cache --force      
  done
  cd ../../BeamableProduct  
fi


