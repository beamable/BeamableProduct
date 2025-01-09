#!/bin/bash

# This script should be run ONCE after 
# you check out the repository for the first time. 
#
# It will create a local Nuget package feed/source 
# in your Home directory. That feed will be used to 
# store local packages as you develop. 


echo "Setting up the Beamables!"

FEED_NAME=BeamableNugetSource
SOURCE_FOLDER=$HOME/BeamableNugetSource

# create the folder if it doesn't exist yet...
mkdir -p $SOURCE_FOLDER

echo "Removing old source (if none exists, you'll see an error 'Unable to find any package', but that is okay)"
dotnet nuget remove source $FEED_NAME || true

echo "Adding new source!"
dotnet nuget add source $SOURCE_FOLDER --name $FEED_NAME