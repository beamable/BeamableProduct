#!/bin/bash

echo "Packing..."
pwd
echo "printing version"
echo $VERSION
cd ./cli/cli
echo "in cli"
pwd
dotnet pack --configuration Release /p:Version=$VERSION

echo "okay, doing an ls"
ls -a

echo "Verifying..."
dotnet tool install --global --add-source ./nupkg/ beamcli
beam --version #todo: is it possible to assert that the output must match the $VERSION string?

echo "Publishing..."
dotnet nuget push ./bin/release/BeamCli.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key test
