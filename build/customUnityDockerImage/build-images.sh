#!/usr/bin/env bash

GAME_CI_VERSION=3.1.0
MY_USERNAME=beamable


# BUILD THE PLAYMODE VERSIONS
declare -a components=("base")
declare -a versions=("2021.3.29f1" "2022.3.7f1" "6000.0.37f1")

for version in "${versions[@]}"
do
    for component in "${components[@]}"
    do
        # a valid image resembles something like this,
        #  editor:ubuntu-6000.0.37f1-android-3.1.0

        GAME_CI_UNITY_EDITOR_IMAGE=unityci/player:ubuntu-${version}-${component}-${GAME_CI_VERSION}
        IMAGE_TO_PUBLISH=${CUSTOM_REGISTRY}${MY_USERNAME}/player:ubuntu-${version}-${component}-${GAME_CI_VERSION}

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


# BUILD THE EDITOR VERSIONS
declare -a components=("android" "ios" "mac-mono" "webgl" "windows-mono" "base")
declare -a versions=("2021.3.29f1" "2022.3.7f1" "6000.0.37f1")

for version in "${versions[@]}"
do
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