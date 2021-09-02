#!/bin/bash
set -e

echo "Starting com.beamable package build"
docker-compose --no-ansi -f docker/package.com.beamable/docker-compose.yml up --build --exit-code-from beamable
docker-compose --no-ansi -f docker/package.com.beamable/docker-compose.yml down # TODO: Ensure that this down command executes

echo "Starting com.beamable.server package build"
docker-compose --no-ansi -f docker/package.com.beamable.server/docker-compose.yml up --build --exit-code-from beamable_server
docker-compose --no-ansi -f docker/package.com.beamable.server/docker-compose.yml down # TODO: Ensure that this down command executes

echo "Starting Microservice dependencies..."
docker-compose --no-ansi -f docker/image.microservice/docker-compose.yml up --build --exit-code-from microservice
docker-compose --no-ansi -f docker/image.microservice/docker-compose.yml down # TODO: Ensure that this down command executes

export BEAMSERVICE_TAG=${ENVIRONMENT}_${VERSION:-0.0.0}
export LOCAL_REPO_TAG=beamservice:${BEAMSERVICE_TAG}
export REMOTE_REPO_TAG=beamableinc/${LOCAL_REPO_TAG}

echo "Building Microservice base image..."
docker build -t ${LOCAL_REPO_TAG} ../microservice/microservice --build-arg BEAMABLE_SDK_VERSION=${VERSION:-0.0.0}

echo "Pushing Microservice base image..."
docker login -u ${DOCKER_HUB_USER} -p ${DOCKER_HUB_PASSWORD}
docker tag ${LOCAL_REPO_TAG} ${REMOTE_REPO_TAG}
docker push ${REMOTE_REPO_TAG}

exit