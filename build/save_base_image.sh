#!/bin/sh
echo "Running Docker save for base image"
# https://github.com/game-ci/unity-builder/blob/main/src/model/image-tag.ts

export TAG='base'
case "$TARGET_PLATFORM" in
   "WebGL") export TAG='webgl'
   ;;
   "Android") export TAG='android'
   ;;
   "StandaloneWindows") export TAG='windows-mono'
   ;;
   "iOS") export TAG='ios'
   ;;
   "StandaloneOSX") export TAG='mac-mono'
   ;;
esac

export MODE='editor'
case "$TEST_MODE" in 
    "editmode") export MODE='editor'
    ;;
    "playmode") export MODE='player'
    ;;
esac

# unityci/editor:2020.3.19f1-mac-mono-0
export IMAGE=unityci/${MODE}:${UNITY_VERSION}-${TAG}-0
export TAR="unityci_${MODE}_${UNITY_VERSION}_${TAG}_0.tar"
echo "Identified $IMAGE"
echo "Saving to $TAR"
mkdir ./docker-cache
docker save -o ./docker-cache/$TAR $IMAGE