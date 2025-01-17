#!/bin/bash

echo "Packing..."

echo "printing version"
echo $VERSION_PREFIX
echo $VERSION_SUFFIX
export SUFFIX=$(echo $VERSION_SUFFIX | tr . -)
echo $SUFFIX
echo $VERSION
BUILD_OUTPUT="./build-output"
mkdir -p $BUILD_OUTPUT

echo "running dotnet packs"

if [ -z "$SUFFIX" ]
then
    BUILD_ARGS="--configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION"
else
    BUILD_ARGS="--configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION"
fi

#TODO: need to update the template references somehow!

dotnet pack ./build/LocalBuild/LocalBuild.sln --output $BUILD_OUTPUT $BUILD_ARGS

echo "Checking for publish"
echo $DRY_RUN
if [ "$DRY_RUN" = "true" ]
then 
    echo "Not running due to dry run."
    exit $?
else
    dotnet nuget push $BUILD_OUTPUT/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
fi
