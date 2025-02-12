#!/usr/bin/env bash


UNITY_VERSION=6000.0.37f1
GAME_CI_VERSION=3.1.0
MY_USERNAME=beamable

# don't hesitate to remove unused components from this list
declare -a components=("android" "ios" "mac-mono" "webgl" "windows-mono")

for component in "${components[@]}"
do
  GAME_CI_UNITY_EDITOR_IMAGE=unityci/editor:ubuntu-${UNITY_VERSION}-${component}-${GAME_CI_VERSION}
  IMAGE_TO_PUBLISH=${CUSTOM_REGISTRY}${MY_USERNAME}/editor:ubuntu-${UNITY_VERSION}-${component}-${GAME_CI_VERSION}
  #editor-ubuntu-6000.0.37f1-android-3.1.0
  #echo $GAME_CI_UNITY_EDITOR_IMAGE
  docker build --build-arg GAME_CI_UNITY_EDITOR_IMAGE=${GAME_CI_UNITY_EDITOR_IMAGE} . -t ${IMAGE_TO_PUBLISH} --platform linux/amd64
  docker push ${IMAGE_TO_PUBLISH}
done