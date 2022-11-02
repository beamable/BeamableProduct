#!/bin/bash

export lib_path="../microservice/microservice/lib"

echo "Building microserivce dependencies..."
cd client
echo "Building Common Library..."
dotnet publish ../client/Packages/com.beamable/Common -c release -o $lib_path

echo "Building Server Library..."
dotnet publish ../client/Packages/com.beamable.server/SharedRuntime -c release -o $lib_path

echo "Building Stubs..."
dotnet publish ../microservice/unityEngineStubs -c release -o $lib_path

echo "Building Tools..."
dotnet publish ../microservice/beamable.tooling.common -c release -o $lib_path
