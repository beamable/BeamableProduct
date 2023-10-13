#!/bin/bash
set -e

export BEAMSERVICE_TAG=${ENVIRONMENT}_${VERSION:-0.0.0}
export LOCAL_REPO_TAG=beamservice:${BEAMSERVICE_TAG}

echo "Starting Microservice dependencies..."
docker-compose --no-ansi -f docker/image.microservice/docker-compose.yml build --pull --no-cache # Fresh pull, no cache, builds
docker-compose --no-ansi -f docker/image.microservice/docker-compose.yml down --exit-code-from microservice # Runs containers and checks the exit code
docker-compose --no-ansi -f docker/image.microservice/docker-compose.yml up --exit-code-from microservice # Runs containers and checks the exit code
docker-compose --no-ansi -f docker/image.microservice/docker-compose.yml down # TODO: Ensure that this down command executes

echo "Starting nuget builds"
docker-compose --no-ansi -f docker/cli/docker-compose.yml down # TODO: Ensure that this down command executes
docker-compose --no-ansi -f docker/cli/docker-compose.yml up --build --exit-code-from cli
docker-compose --no-ansi -f docker/cli/docker-compose.yml down # TODO: Ensure that this down command executes

echo "Logging into dockerhub"
docker login -u ${DOCKER_HUB_USER} -p ${DOCKER_HUB_PASSWORD}

echo "Building Microservice base image..."
/usr/libexec/docker/cli-plugins/docker-buildx build --builder beamable-builder --platform linux/arm64,linux/amd64 --push -t beamableinc/${LOCAL_REPO_TAG} ../microservice/microservice --build-arg BEAMABLE_SDK_VERSION=${VERSION:-0.0.0}

echo "Starting com.beamable package build"
docker-compose --no-ansi -f docker/package.com.beamable/docker-compose.yml down # TODO: Ensure that this down command executes
docker-compose --no-ansi -f docker/package.com.beamable/docker-compose.yml up --build --exit-code-from beamable
docker-compose --no-ansi -f docker/package.com.beamable/docker-compose.yml down # TODO: Ensure that this down command executes

echo "Starting com.beamable.server package build"
docker-compose --no-ansi -f docker/package.com.beamable.server/docker-compose.yml down # TODO: Ensure that this down command executes
docker-compose --no-ansi -f docker/package.com.beamable.server/docker-compose.yml up --build --exit-code-from beamable_server
docker-compose --no-ansi -f docker/package.com.beamable.server/docker-compose.yml down # TODO: Ensure that this down command executes

exit
