#!/bin/bash
export ARGSV

cd $PACKAGE_DIR
pwd
ls

npm config fix

if [ "$NPM_COMMAND" == "deprecate" ]
then
  npm deprecate $VERSION "This package has been deprecated by Beamable."
  exit $?
fi

if [ "$DRY_RUN" == "true" ]
then
	ARGSV="$ARGSV --dry-run"
else 
	echo "Running $NPM_COMMAND"
fi

if [ "$UPDATE_NPM_VERSION" == "true" ]
then
	npm version $VERSION --allow-same-version --loglevel $NPM_LOGLEVEL
else 
	echo "Not updating the version."
fi

#check the previous command error state before continuing.
if [ $? -eq 0 ]
then
  npm cache clean --force
  npm $NPM_COMMAND $ARGSV --loglevel $NPM_LOGLEVEL
else
	exit $?
fi

if [ $? -eq 0 ]
then
  echo "Completed $NPM_COMMAND Successfully."
  exit 0
else
  echo "$NPM_COMMAND failed."
  exit 1
fi