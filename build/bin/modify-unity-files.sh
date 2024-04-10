# select the right .env file for the package and write it as env-defaults
cp -f client/Packages/com.beamable/Runtime/Environment/Resources/env-${ENVIRONMENT}.json client/Packages/com.beamable/Runtime/Environment/Resources/env-default.json || :

# update the version number in env-defaults
sed -i "s/BUILD__SDK__VERSION__STRING/'${VERISON}'/" "client/Packages/com.beamable/Runtime/Environment/Resources/env-default.json" || :

# update the com.beamable.server dependency on com.beamable
sed -i "s/BUILD__SDK__VERSION__STRING/'${VERISON}'/" "client/Packages/com.beamable.server/package.json" || :
    
# remove old env template files
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-dev.json
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-dev.json.meta
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-staging.json
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-staging.json.meta
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-prod.json
rm -f client/Packages/com.beamable/Runtime/Environment/Resources/env-prod.json.meta

# map samples
# TODO

