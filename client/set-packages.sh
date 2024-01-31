PROJECTS_DIR="../ProjectsSource/"
PROJECTS_SOURCE="$HOME/BeamableSource/"
SOURCE_NAME="BeamableSource"
SOURCE_PATH="$PROJECTS_SOURCE"
VERSION="0.0.0-local"

if [ ! -d "$PROJECTS_DIR" ]; then
    echo "Creating projects source folder!"
    mkdir $PROJECTS_DIR
else
    echo "Projects source folder already exists!"
    rm -r $PROJECTS_DIR/*
fi

if [ ! -d "$PROJECTS_SOURCE" ]; then
    echo "Creating source feed folder!"
    mkdir $PROJECTS_SOURCE
else
    echo "Projects source feed  already exists!"
    rm -r $PROJECTS_SOURCE/*
fi

echo "Creating nupckg files for our packages"
dotnet pack Packages/com.beamable/Common/ -c Release -o $PROJECTS_DIR -p:PackageVersion=$VERSION
dotnet pack Packages/com.beamable.server/Runtime/Common/ -c Release -o $PROJECTS_DIR -p:PackageVersion=$VERSION
dotnet pack Packages/com.beamable.server/SharedRuntime/ -c Release -o $PROJECTS_DIR -p:PackageVersion=$VERSION
dotnet pack ../microservice/beamable.tooling.common/  -c Release -o $PROJECTS_DIR -p:PackageVersion=$VERSION
dotnet pack ../microservice/  -c Release -o $PROJECTS_DIR -p:PackageVersion=$VERSION -p:CombinedVersion=$VERSION
dotnet pack ../microservice/unityEngineStubs/  -c Release -o $PROJECTS_DIR -p:PackageVersion=$VERSION


if dotnet nuget list source | grep -q $SOURCE_NAME; then
    echo "Source already exists!"
else
    echo "Source does not exists!!"
    echo "Setting the projects folder as a source folder for nuget"
    dotnet nuget add source $PROJECTS_SOURCE -n $SOURCE_NAME
fi


dotnet nuget push $PROJECTS_DIR -s $PROJECTS_SOURCE