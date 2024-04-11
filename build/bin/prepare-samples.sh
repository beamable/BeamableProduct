#!/bin/bash

mkdir -p Samples~
echo "Converting samples into UPM samples"
ls -a
echo "--- listing samples ---"
ls Samples
echo "---- showing package.json ----"
cat ./package.json

# for the package, we need to identify the files listed in the ./package.json's Samples section
for row in $(cat ./package.json | jq -r '.samples[] | @base64'); do

    _jq() {
     echo ${row} | base64 -d | jq -r ${1}
    }
    path=$(_jq '.path')
    echo "Converting samples - found path ${path}"

    sourcePath="${path/"SAMPLES_PATH"/"Samples"}"
    echo "Converting samples - found source ${sourcePath}"

    dstPath="${path/"SAMPLES_PATH"/"Samples~"}"
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