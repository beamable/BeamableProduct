#!/bin/bash

echo "Packing..."
echo "printing version"
echo $VERSION
cd ./cli/cli
dotnet pack --configuration Release /p:Version=$VERSION

echo "Verifying..."
export PATH="$PATH:/root/.dotnet/tools"
dotnet tool install --global --add-source ./nupkg/ beamcli
beam --version #todo: is it possible to assert that the output must match the $VERSION string?


if [ "$DRY_RUN" == "true" ]
then
	echo "Not running due to dry run."
else 
    echo "Publishing..."
    dotnet nuget push ./bin/release/BeamCli.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key test
fi