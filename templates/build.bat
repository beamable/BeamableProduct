:: uninstall the old version if it exists...
dotnet "new" "-u" "Beamable.Templates"

:: clean and prepare the template projects
DEL /S /F /Q "%CD%/templates/Data"
mkdir "%CD%/templates/Data"

robocopy  "%CD%/BeamService/" "%CD%/templates/Data/BeamService" /s /e
robocopy  "%CD%/CommonLibrary/" "%CD%/templates/Data/CommonLibrary" /s /e
robocopy  "%CD%/BeamStorage/" "%CD%/templates/Data/BeamStorage" /s /e

:: TODO: modify the version number of the copied data
dotnet restore

:: produce a nuget package...
:: dotnet pack ./templates/templates.csproj -o ./artifacts/ --no-build --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
dotnet "pack" "%CD%/templates/templates.csproj" "-o" "%CD%/artifacts/" "--no-build" "/p:VersionPrefix=0.0.0"

:: clean the folder again...
DEL /S /F /Q "%CD%/templates/Data"

:: install the latest version...
dotnet "new" "install" "%CD%/artifacts/Beamable.Templates.0.0.0.nupkg"

