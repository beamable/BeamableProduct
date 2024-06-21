#!/bin/bash

# Path where we'll put the BeamableSource folder and make it into a nuget source feed
HOME_FOR_BUILD=${1:-$HOME_OVERRIDE}
HOME_FOR_BUILD=${HOME_FOR_BUILD:-$HOME}
# The name of the source in the format UNITY_|UNREAL_|WHATEVER_
SOURCE_NAME=${2-"BeamableNugetSource"}
# Look at PKG_ variables and separated by '|'
PACKAGES_TO_SET=${3:-''}
# Either "", "Local" or "Global"
INSTALL_CLI=${4:-""}
# When not "", this should be the absolute path we'll call dotnet restore in after the script has run
PATH_TO_DOTNET_RESTORE=${5:-""}

# Overridable via EnvVars only
PROJECTS_DIR=${PROJECT_DIR_OVERRIDE:-"ProjectsSource"}
PROJECTS_SOURCE="$HOME_FOR_BUILD/BeamableSource/"
VERSION="0.0.123"

PKG_COMMON="./cli/beamable.common/"
PKG_SERVER_COMMON="./cli/beamable.server.common/"
PKG_MICROSERVICE_TOOLING_COMMON="./microservice/beamable.tooling.common/"
PKG_MICROSERVICE_UNITY_ENGINE_STUBS="./microservice/unityEngineStubs/"
PKG_MICROSERVICE_UNITY_ENGINE_STUBS_ADDRESSABLES="./microservice/unityEngineStubs.addressables/"
PKG_CLI="./cli/cli"
PKG_MICROSERVICE="./microservice/microservice"
PKG_TEMPLATES="./templates"

PACKAGES_TO_SET_ARR[0]="$PKG_COMMON"
PACKAGES_TO_SET_ARR[1]="$PKG_SERVER_COMMON"
PACKAGES_TO_SET_ARR[2]="$PKG_MICROSERVICE_TOOLING_COMMON"
PACKAGES_TO_SET_ARR[3]="$PKG_MICROSERVICE_UNITY_ENGINE_STUBS"
PACKAGES_TO_SET_ARR[4]="$PKG_MICROSERVICE_UNITY_ENGINE_STUBS_ADDRESSABLES"
PACKAGES_TO_SET_ARR[5]="$PKG_CLI"
PACKAGES_TO_SET_ARR[6]="$PKG_MICROSERVICE"
PACKAGES_TO_SET_ARR[7]="$PKG_TEMPLATES"

if [[ "$PACKAGES_TO_SET" != '' ]]; then
    echo "Filtering packages to set..."
    PACKAGES_TO_SET_ARR=("${PACKAGES_TO_SET//|/ }")
    printf '%s, ' "${PACKAGES_TO_SET_ARR[@]}"
    echo ""
fi

if [ ! -d "$PROJECTS_DIR" ]; then
    echo "Creating projects source folder!"
    mkdir "$PROJECTS_DIR"
else
    echo "Projects source folder ($PROJECTS_DIR) already exists!"
fi
ls $PROJECTS_DIR

if [ ! -d "$PROJECTS_SOURCE" ]; then
    echo "Creating source feed folder!"
    mkdir "$PROJECTS_SOURCE"
else
    echo "Projects source feed ($PROJECTS_SOURCE) already exists!"
fi

# Clean up the path so that it works on all OSs
if [[ $PROJECTS_SOURCE == *:* ]];then
    case "$OSTYPE" in
    solaris*) PROJECTS_SOURCE=$PROJECTS_SOURCE ;;
    darwin*)  PROJECTS_SOURCE=$PROJECTS_SOURCE ;; 
    linux*)   PROJECTS_SOURCE=$PROJECTS_SOURCE ;;
    bsd*)     PROJECTS_SOURCE=$PROJECTS_SOURCE ;;
    msys*)    PROJECTS_SOURCE=${PROJECTS_SOURCE/\//\/\/} ;;
    cygwin*)  PROJECTS_SOURCE=${PROJECTS_SOURCE/\//\/\/} ;;
    *)        echo "Should never see this!!" ;;
    esac
fi

# Clean up the path so that it works on all OSs
if [[ $PATH_TO_DOTNET_RESTORE == *:* ]];then
    case "$OSTYPE" in
    solaris*) PATH_TO_DOTNET_RESTORE=$PATH_TO_DOTNET_RESTORE ;;
    darwin*)  PATH_TO_DOTNET_RESTORE=$PATH_TO_DOTNET_RESTORE ;; 
    linux*)   PATH_TO_DOTNET_RESTORE=$PATH_TO_DOTNET_RESTORE ;;
    bsd*)     PATH_TO_DOTNET_RESTORE=$PATH_TO_DOTNET_RESTORE ;;
    msys*)    PATH_TO_DOTNET_RESTORE=${PATH_TO_DOTNET_RESTORE/\//\/\/} ;;
    cygwin*)  PATH_TO_DOTNET_RESTORE=${PATH_TO_DOTNET_RESTORE/\//\/\/} ;;
    *)        echo "Should never see this!!" ;;
    esac
fi

echo "I'm a fake nupkg that exist here so that our sample microservices' Dockerfile-BeamableDev can work properly." >> "$PROJECTS_SOURCE"/DONT_MIND_ME.nupkg


echo "Creating nupkg files for our packages"

## Check if we should be building each of the possible packages and then correctly build them (this grep is just a contain
## The "rm -rf" invalidates the nuget cache for the previous cached version of this package version  
if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_COMMON"; then
    dotnet pack "$PKG_COMMON" --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION
    rm -rf ~/.nuget/packages/beamable.common/$VERSION
fi

if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_SERVER_COMMON"; then
    dotnet pack "$PKG_SERVER_COMMON" --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION
    rm -rf ~/.nuget/packages/beamable.tooling.common/$VERSION
fi

if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_MICROSERVICE_TOOLING_COMMON"; then
    dotnet pack "$PKG_MICROSERVICE_TOOLING_COMMON"  --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION
    rm -rf ~/.nuget/packages/beamable.server/$VERSION
fi

if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_MICROSERVICE_UNITY_ENGINE_STUBS"; then
    dotnet pack "$PKG_MICROSERVICE_UNITY_ENGINE_STUBS" --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION
    rm -rf ~/.nuget/packages/beamable.unityengine/$VERSION
fi

if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_MICROSERVICE_UNITY_ENGINE_STUBS_ADDRESSABLES"; then
    dotnet pack "$PKG_MICROSERVICE_UNITY_ENGINE_STUBS_ADDRESSABLES"  --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION
    rm -rf ~/.nuget/packages/beamable.unity.addressables/$VERSION
fi

if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_CLI"; then
    dotnet build "$PKG_CLI" --configuration Release -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION
    dotnet pack "$PKG_CLI"  --configuration Release --no-build --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION
    rm -rf ~/.nuget/packages/beamable.microservice.runtime/$VERSION
fi

if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_MICROSERVICE"; then
    dotnet build "$PKG_MICROSERVICE"  --configuration Release -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION
    dotnet pack "$PKG_MICROSERVICE"  --configuration Release --no-build --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION
    rm -rf ~/.nuget/packages/beamable.tools/$VERSION
fi

if printf '%s\0' "${PACKAGES_TO_SET_ARR[@]}" | grep -qwz "$PKG_TEMPLATES"; then
  OUTPUT=$PROJECTS_DIR VERSION=$VERSION "$PKG_TEMPLATES"/build.sh
fi

# Print out the source list
echo "List of Sources"
dotnet nuget list source

if dotnet nuget list source | grep -q $SOURCE_NAME; then
    echo "Source already exists!"
else
    echo "Source does not exists!!"
    echo "Setting the projects folder as a source folder for nuget"
    echo "$PROJECTS_SOURCE"
    echo "$SOURCE_NAME"  
    dotnet nuget add source "$PROJECTS_SOURCE" -n "$SOURCE_NAME"
    echo "Added source $SOURCE_NAME bound to path $PROJECTS_SOURCE"
    dotnet nuget list source
fi

echo "Pushing"
echo $PROJECTS_DIR
echo $PROJECTS_SOURCE
ls $PROJECTS_DIR
ls $PROJECTS_SOURCE


# Nuget push seems to need the actual files on windows (it doesn't automatically get all the files if you just pass in a directory) 
# The $PACKAGE_SOURCE_SUFFIX_BLOB is set from the buildPR.yml file.
for i in `find $PROJECTS_DIR -name "*.nupkg" -type f`; do    
    dotnet nuget push "$i" -s "$SOURCE_NAME"
done
dotnet nuget update source "$SOURCE_NAME"

## If we also want to install the CLI in our Engine Integration SDK Repo (this will install the CLI in the first found .config/dotnet-tools.json while walking up the hierarchy)
if [[ "$INSTALL_CLI" == "Local" ]];then
  cd "$PROJECTS_SOURCE/.." || dotnet tool uninstall beamable.tools || true
  cd "$PROJECTS_SOURCE/.." || dotnet tool install beamable.tools --version "$VERSION"
fi

## If we also want to install the CLI
if [[ "$INSTALL_CLI" == "Global" ]];then
  dotnet tool uninstall beamable.tools -g || true
  dotnet tool install beamable.tools --global --version "$VERSION"
fi

## If we want to restore the project in a particular path...
if [[ "$PATH_TO_DOTNET_RESTORE" != "" ]];then
  echo "Restoring project at $PATH_TO_DOTNET_RESTORE" 
  cd "$PROJECTS_SOURCE/.."
  dotnet tool restore || true
  cd "$PATH_TO_DOTNET_RESTORE"
  dotnet restore || true
fi
