#bin/sh

dotnet pack
dotnet tool update --global --add-source ./nupkg/ BeamCli