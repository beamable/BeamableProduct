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
    solaris*) ROOT=$(pwd) ;;
    darwin*)  ROOT=$(pwd) ;;
    linux*)   ROOT=$(pwd) ;;
    bsd*)     ROOT=$(pwd) ;;
    msys*)    
        ROOT=$(pwd -W) 
        ROOT="${ROOT:0:2}/${ROOT:2}"
        ;;
    cygwin*) 
        ROOT=$(pwd -W) 
        ROOT="${ROOT:0:2}/${ROOT:2}"
        ;;
    *)        echo "Should never see this!!" ;;
esac

SOURCE_FOLDER=$ROOT/$SOURCE_FOLDER
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

# restore cli tools
dotnet tool restore --tool-manifest ./cli/cli/.config/dotnet-tools.json

# reset the template projects to reference the base version number
TEMPLATE_DOTNET_CONFIG_PATH="./cli/beamable.templates/.config/dotnet-tools.json"
ls -a "./cli/beamable.templates/"
mkdir -p "./cli/beamable.templates/.config"
cat > $TEMPLATE_DOTNET_CONFIG_PATH << EOF
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "beamable.tools": {
      "version": "0.0.123.0",
      "commands": [
        "beam"
      ],
      "rollForward": false
    }
  }
}
EOF


# TODO: Consider running this as part of a post-pull git action
# TODO: Should this run the `sync-rider-run-settings.sh` script? (probably)