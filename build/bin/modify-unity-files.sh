# review inputs
echo "ENVIRONMENT = ${ENVIRONMENT}"
echo "VERSION = ${VERSION}"

# select the right .env file for the package and write it as env-defaults
SRC_FILE="client/Packages/com.beamable/Runtime/Environment/Resources/env-$ENVIRONMENT.json"
DST_FILE="client/Packages/com.beamable/Runtime/Environment/Resources/env-default.json"
echo "Moving $SRC_FILE to $DST_FILE"
cp -f $SRC_FILE $DST_FILE

# update the version number in env-defaults
sed -i "s/BUILD__SDK__VERSION__STRING/${VERSION}/" client/Packages/com.beamable/Runtime/Environment/Resources/env-default.json
sed -i "s/UNITY__VSP__UID/false/" client/Packages/com.beamable/Runtime/Environment/Resources/env-default.json

# update the com.beamable.server dependency on com.beamable
sed -i "s/BUILD__SDK__VERSION__STRING/${VERSION}/" client/Packages/com.beamable.server/package.json
    
# remove old env template files
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-dev.json
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-dev.json.meta
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-staging.json
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-staging.json.meta
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-prod.json
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-prod.json.meta

# map samples
# TODO

