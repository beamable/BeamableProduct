PROJECTS_DIR=${PROJECT_DIR_OVERRIDE:-"../ProjectsSource/"}
HOME_FOR_BUILD=${1:-$HOME}
PROJECTS_SOURCE="$HOME_FOR_BUILD/BeamableSource/"
SOURCE_NAME="BeamableSource"
SOURCE_PATH="$PROJECTS_SOURCE"
VERSION="0.0.123"
VERSION_INFO="$VERSION"

if [ ! -d "$PROJECTS_DIR" ]; then
    echo "Creating projects source folder!"
    mkdir $PROJECTS_DIR
else
    echo "Projects source folder already exists!"
    rm -r $PROJECTS_DIR/*
fi
ls $PROJECTS_DIR

if [ ! -d "$PROJECTS_SOURCE" ]; then
    echo "Creating source feed folder!"
    mkdir $PROJECTS_SOURCE
else
    echo "Projects source feed  already exists!"
    rm -r $PROJECTS_SOURCE/*
fi

echo "Creating nupckg files for our packages"
dotnet pack ../cli/beamable.common/ --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION_INFO
dotnet pack ../cli/beamable.server.common/ --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION_INFO
dotnet pack ../microservice/beamable.tooling.common/  --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION_INFO
dotnet pack ../microservice/unityEngineStubs/  --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION_INFO
dotnet pack ../microservice/unityEngineStubs.addressables/  --configuration Release --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:InformationalVersion=$VERSION_INFO

dotnet build ../microservice/microservice/  --configuration Release -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION_INFO
dotnet pack ../microservice/microservice/  --configuration Release --no-build --include-source  -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION -p:InformationalVersion=$VERSION_INFO


OUTPUT=$PROJECTS_DIR VERSION=$VERSION ../templates/build.sh

if dotnet nuget list source | grep -q $SOURCE_NAME; then
    echo "Source already exists!"
else
    echo "Source does not exists!!"
    echo "Setting the projects folder as a source folder for nuget"
    echo $PROJECTS_SOURCE
    echo $SOURCE_NAME
    dotnet nuget add source "$PROJECTS_SOURCE" -n $SOURCE_NAME
fi

echo "Removing cached nuget entries"
rm -rf ~/.nuget/packages/beamable.common/$VERSION
rm -rf ~/.nuget/packages/beamable.tooling.common/$VERSION
rm -rf ~/.nuget/packages/beamable.server/$VERSION
rm -rf ~/.nuget/packages/beamable.unityengine/$VERSION
rm -rf ~/.nuget/packages/beamable.unity.addressables/$VERSION
rm -rf ~/.nuget/packages/beamable.microservice.runtime/$VERSION


echo "Pushing"
echo $PROJECTS_DIR
echo $PROJECTS_SOURCE
ls $PROJECTS_DIR
ls $PROJECTS_SOURCE

# Nuget push seems to need the actual files on windows (it doesn't automatically get all the files if you just pass in a directory) 
PROJECTS_DIR_FILES=$PROJECTS_DIR${PACKAGE_SOURCE_SUFFIX_BLOB:-"*.nupkg"}
dotnet nuget push "$PROJECTS_DIR_FILES" -s $PROJECTS_SOURCE