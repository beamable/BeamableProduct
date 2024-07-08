#!/bin/bash

# ATM, this only works on Mac because path strings are annoying across OSs.
cd ../..
bash ./set-packages.sh "" "BeamableNugetSource" "./cli/cli" "Global" ""
