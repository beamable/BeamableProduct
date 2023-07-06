#!/bin/bash

default_version='0.0.0'
version=${1:-$default_version}

dotnet pack -p:PackageVersion=$version
dotnet tool uninstall beamable.tools -g || true
dotnet tool install --global --version $version --add-source ./nupkg/ beamable.tools
