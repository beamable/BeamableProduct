#bin/sh

dotnet pack
dotnet tool uninstall cli -g
dotnet tool install --global --add-source ./nupkg/ cli