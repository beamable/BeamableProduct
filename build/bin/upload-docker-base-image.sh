#!/bin/bash
set -e

export BEAMSERVICE_TAG=${ENVIRONMENT}_${VERSION:-0.0.0}
export LOCAL_REPO_TAG=beamservice:${BEAMSERVICE_TAG}

echo "Docker Version Info..."
docker version

echo "Tag Info...."
echo "beamserviceTag = $BEAMSERVICE_TAG" 
echo "localRepoTag = $LOCAL_REPO_TAG" 
echo "version = $VERSION"
echo "env = $ENVIRONMENT"

echo "Logging into dockerhub"
docker login -u ${DOCKER_HUB_USER} -p ${DOCKER_HUB_PASSWORD}


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

echo "Building Microservice base image..."
docker build --platform linux/arm64,linux/amd64 --push -t beamableinc/${LOCAL_REPO_TAG} ../microservice/microservice --build-arg BEAMABLE_SDK_VERSION=${VERSION:-0.0.0}
