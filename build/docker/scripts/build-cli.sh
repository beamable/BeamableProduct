#!/bin/bash

echo "Packing..."
echo "printing version"
echo $VERSION
cd ./cli/cli
dotnet pack --configuration Release /p:Version=$VERSION

echo "Installing built package..."
export PATH="$PATH:/root/.dotnet/tools"
dotnet tool install --global --add-source ./nupkg/ beamable.tools

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
