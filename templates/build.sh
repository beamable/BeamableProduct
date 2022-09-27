# uninstall the old version if it exists...
dotnet new -u Beamable.Templates

# clean and prepare the template projects
rm -rf ./templates/Data
mkdir ./templates/Data
cp -R ./BeamService/ ./templates/Data/BeamService

# produce a nuget package...
dotnet pack ./templates/templates.csproj -o ./artifacts/ --no-build

# clean the folder again...
rm -rf ./templates/Data

# install the latest version...
dotnet new -i ./artifacts/Beamable.Templates.1.0.0.nupkg