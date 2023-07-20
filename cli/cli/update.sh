#bin/sh

default_version='0.0.0'
version=${1:-$default_version}

dotnet pack -p:PackageVersion=$version
dotnet tool update --global --version $version --add-source ./nupkg/ beamable.tools
