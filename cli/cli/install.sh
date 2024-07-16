#!/bin/bash

cd ../..
# ATM, this only works on Mac because path strings are annoying across OSs.
bash ./set-packages.sh "" "BeamableNugetSource" "./cli/cli" "Global" ""
