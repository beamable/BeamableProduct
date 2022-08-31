#!/bin/bash

echo "Packing..."

cd ./cli/cli
echo "printing version"
echo $VERSION_PREFIX
echo $VERSION_SUFFIX
export SUFFIX=$(echo $VERSION_SUFFIX | tr . -)
echo $SUFFIX
echo $VERSION
if [ -z "$SUFFIX" ]
then 
    dotnet pack --configuration Release /p:VersionPrefix=$VERSION_PREFIX
else
    dotnet pack --configuration Release --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
fi

echo "Installing built package..."
export PATH="$PATH:/root/.dotnet/tools"
dotnet tool install --global --prerelease --add-source ./nupkg/ beamable.tools

echo "Checking built version..."
beam --version #todo: is it possible to assert that the output must match the $VERSION string?

echo "Checking for publish"
echo $DRY_RUN
if [ "$DRY_RUN" = "true" ]
then 
    echo "Not running due to dry run."
    exit $?
else
    dotnet nuget push ./nupkg/Beamable.Tools.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
fi
