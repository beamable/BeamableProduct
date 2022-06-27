#!/bin/bash

mkdir -p Samples~
# for the package, we need to identify the files listed in the ./package.json's Samples section
for row in $(jq -r '.samples[] | @base64' ./package.json); do
    _jq() {
     echo ${row} | base64 --decode | jq -r ${1}
    }

    path=$(_jq '.path')
    sourcePath="${path/SAMPLES_PATH/Samples}"
    dstPath="${path/SAMPLES_PATH/Samples~}"

    # move the source path to the dst path
    mv $sourcePath $dstPath

    # update the package json file
    sed -i 's,'$path','$dstPath',' package.json
done

# delete the /Samples directory
rm -rf Samples/