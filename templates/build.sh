VERSION=${VERSION-'0.0.0'}
OUTPUT=${OUTPUT-'./artifacts/'}
cd "$(dirname "$0")"
# uninstall the old version if it exists...
dotnet new -u Beamable.Templates

# clean and prepare the template projects
rm -rf ./templates/Data
mkdir ./templates/Data
cp -R ./BeamService/ ./templates/Data/BeamService
cp -R ./CommonLibrary/ ./templates/Data/CommonLibrary
cp -R ./BeamStorage/ ./templates/Data/BeamStorage

# TODO: modify the version number of the copied data

# produce a nuget package...
# dotnet pack ./templates/templates.csproj -o ./artifacts/ --no-build --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
dotnet pack ./templates/templates.csproj -o ${OUTPUT} -p:PackageVersion=$VERSION
# clean the folder again...
rm -rf ./templates/Data

# install the latest version...
dotnet new -i ${OUTPUT}Beamable.Templates.${VERSION}.nupkg