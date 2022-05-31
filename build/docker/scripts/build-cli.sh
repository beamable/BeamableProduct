#!/bin/bash

echo "Packing..."
pwd
cd ./cli/cli
dotnet pack --configuration Release /p:Version=$VERSION

echo "Verifying..."
dotnet tool install --global --add-source ./nupkg/ beamcli
beam --version #todo: is it possible to assert that the output must match the $VERSION string?

echo "Publishing..."
dotnet nuget push ./bin/release/BeamCli.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key test
