#!/usr/bin/env bash

GAME_CI_VERSION=3.1.0
MY_USERNAME=beamable



# BUILD THE EDITOR VERSIONS
declare -a components=("android" "ios" "mac-mono" "webgl" "windows-mono" "base")
declare -a versions=("6000.3.0f1")

for version in "${versions[@]}"
do
    # Use GameCI 3.2.0 only for Unity 6000.2+
    if [[ "$version" == 6000.2.* ]]; then
        GAME_CI_VERSION=3.2.0
    elif [[ "$version" == 6000.3.* ]]; then
        GAME_CI_VERSION=3.2.1
    else
        GAME_CI_VERSION=3.1.0
    fi
    for component in "${components[@]}"
    do
        # a valid image resembles something like this,
        #  editor:ubuntu-6000.0.37f1-android-3.1.0

        GAME_CI_UNITY_EDITOR_IMAGE=unityci/editor:ubuntu-${version}-${component}-${GAME_CI_VERSION}
        IMAGE_TO_PUBLISH=${CUSTOM_REGISTRY}${MY_USERNAME}/editor:ubuntu-${version}-${component}-${GAME_CI_VERSION}

        echo "cleaning docker files"
        docker system prune -a --volumes --force
        docker rmi -f $(docker images -aq)

        echo "checking docker clean status"
        docker images -a  # Should be empty
        docker ps -a      # Should be empty
        docker volume ls  # Should be empty

        echo "building: ${IMAGE_TO_PUBLISH}"
        docker build --build-arg GAME_CI_UNITY_EDITOR_IMAGE=${GAME_CI_UNITY_EDITOR_IMAGE} . -t ${IMAGE_TO_PUBLISH} --platform linux/amd64
        
        echo "pushing: ${IMAGE_TO_PUBLISH}"
        docker push ${IMAGE_TO_PUBLISH}
    done
done