#!/bin/bash
set -e

export lib_path="./microservice/microservice/lib"

echo "Building microserivce dependencies..."
cd client
echo "Building Common Library..."
dotnet publish ./client/Packages/com.beamable/Common -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Server Library..."
dotnet publish ./client/Packages/com.beamable.server/SharedRuntime -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Stubs..."
dotnet publish ./microservice/unityEngineStubs -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Addressable Stubs..."
dotnet publish ./microservice/unityenginestubs.addressables -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Tools..."
dotnet publish ./microservice/beamable.tooling.common -c release -o $lib_path /p:InformationalVersion=$VERSION

echo "Building Microservice base image..."
# docker build --platform linux/arm64,linux/amd64 --push -t beamableinc/${LOCAL_REPO_TAG} ../microservice/microservice --build-arg BEAMABLE_SDK_VERSION=${VERSION:-0.0.0}
