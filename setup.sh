#!/bin/bash

# This script should be run ONCE after 
# you check out the repository for the first time. 
#
# It will create a local Nuget package feed/source 
# in your Home directory. That feed will be used to 
# store local packages as you develop. 

echo "Setting up the Beamables!"

# the .dev.env file hosts some common variables
source ./.dev.env

echo "Checking OS=[$OSTYPE] type for root value"
# Get the current directory in the right format...
case "$OSTYPE" in
    solaris*) SOURCE_FOLDER=$(pwd)/$SOURCE_FOLDER ;;
    darwin*)  SOURCE_FOLDER=$(pwd)/$SOURCE_FOLDER ;;
    linux*)   SOURCE_FOLDER=$(pwd)/$SOURCE_FOLDER ;;
    bsd*)     SOURCE_FOLDER=$(pwd)/$SOURCE_FOLDER ;;
    msys*)    SOURCE_FOLDER=$(pwd -W)/$SOURCE_FOLDER ;;
    cygwin*)  SOURCE_FOLDER=$(pwd -W)/$SOURCE_FOLDER ;;
    *)        echo "Should never see this!!" ;;
esac

echo "Setting up $FEED_NAME at $SOURCE_FOLDER"



# delete contents of old folder, and then re-create it. 
#  this essentially removes all old packages as well. 
rm -rf $SOURCE_FOLDER
mkdir -p $SOURCE_FOLDER

echo "Removing old source (if none exists, you'll see an error 'Unable to find any package', but that is okay)"
dotnet nuget remove source $FEED_NAME || true


echo "Adding new source!"
dotnet nuget add source $SOURCE_FOLDER --name $FEED_NAME

# reset the build number back to zero.
echo "Creating build number file"
echo 0 > build-number.txt


# TODO: Consider running this as part of a post-pull git action
# TODO: Should this run the `sync-rider-run-settings.sh` script? (probably)