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



GO_VERSION="1.24"
CURRENT_OS="windows"

GO_INSTALLED=0

if command -v go &> /dev/null; then
    INSTALLED_GO_VERSION=$(go version | awk '{print $3}' | sed 's/go//')
    echo "Go is installed with version: $INSTALLED_GO_VERSION"
    if [[ "$INSTALLED_GO_VERSION" == *"$GO_VERSION"* ]]; then
        GO_INSTALLED=1
        echo "Found GO installation with correct version!"
    else
        echo "This script needs GO with version $GO_VERSION, instead found $INSTALLED_GO_VERSION"
        exit 1
    fi
fi


#TODO This could be improved by actually running through a list of dependencies and check for all of them, better done once we actually have more deps
# if the dependency is not installed by default, then we will need either Chocolatey or Homebrew
if [[ "$GO_INSTALLED" == "0" ]]; then
    # check for os type, if windows then we need chocolatey to be installed, otherwise we need homebrew
    case "$OSTYPE" in
      darwin*)  
          CURRENT_OS="mac"
          if command -v brew &> /dev/null; then
            echo "Homebrew is installed"
            echo "Version: $(brew --version | head -n 1)"
          else
              echo "Homebrew is not installed, please install it before running this script!"
              exit 1
          fi
          ;;
      msys*|cygwin*|win32)  
          CURRENT_OS="windows"
          if command -v choco &> /dev/null; then
            echo "Chocolatey is installed with version: $(choco --version)"
          else
            echo "Chocolatey is not installed, please install it before running this script!"
            exit 1
          fi
          ;;
      *)        
          echo "This message should never be printed, os: $OSTYPE"
          exit 1
          ;;
    esac

    if [[ "$CURRENT_OS" == "windows" ]]; then
        if choco install golang --version=$GO_VERSION -y; then
            echo "GO with version $GO_VERSION was successfully installed!"
            export PATH="$PATH:/c/Go/bin" #This is the default case for chocolatey, it might not work depending on the user's setup
        else
            echo "Failed to install GO using Chocolatey!"
            exit 1
        fi
    else
        if brew install go@$GO_VERSION 2>/dev/null; then
            echo "GO with version $GO_VERSION was successfully installed!"
            export PATH="/opt/homebrew/bin:/usr/local/bin:$PATH" #This is the default case for homebrew, it might not work depending on the user's setup
        else
            echo "Failed to install GO using Homebrew!"
        fi
    fi
fi


# Builds all the otel collector binaries
cd ./otel-collector/
./build.sh --version 0.0.123
cd ..



# TODO: Consider running this as part of a post-pull git action
# TODO: Should this run the `sync-rider-run-settings.sh` script? (probably)