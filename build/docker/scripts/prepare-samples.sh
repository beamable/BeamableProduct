#!/bin/bash

mkdir -p Samples~
echo "Converting samples into UPM samples"
ls
echo "---"
ls Samples
echo "---"
ls Samples/LightBeamSamples/AccountManager
# for the package, we need to identify the files listed in the ./package.json's Samples section
for row in $(cat ./package.json | jq -r '.samples[] | @base64'); do

    _jq() {
     echo ${row} | base64 -d | jq -r ${1}
    }
    path=$(_jq '.path')
    sourcePath="${path/SAMPLES_PATH/Samples}"
    dstPath="${path/SAMPLES_PATH/Samples~}"

    echo "Converting samples - found path ${path}"
    echo "Converting samples - found source ${sourcePath}"
    echo "Converting samples - found dst ${dstPath}"

    # # update the package json file
    sed -i 's,'$path','$dstPath',' package.json
done

# move all samples into samples~
ls
echo "moving"
mv Samples/* Samples~

# delete the /Samples directory
rm -rf Samples/
rm Samples.meta