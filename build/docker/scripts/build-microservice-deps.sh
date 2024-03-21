#!/bin/bash

export lib_path="../microservice/microservice/lib"

echo "Building microserivce dependencies..."
cd client
echo "Building Common Library..."
dotnet publish ../client/Packages/com.beamable/Common -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Server Library..."
dotnet publish ../client/Packages/com.beamable.server/SharedRuntime -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Stubs..."
dotnet publish ../microservice/unityEngineStubs -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Addressable Stubs..."
dotnet publish ../microservice/unityenginestubs.addressables -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Tools..."
dotnet publish ../microservice/beamable.tooling.common -c release -o $lib_path /p:InformationalVersion=$VERSION
