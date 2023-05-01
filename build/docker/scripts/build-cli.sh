#!/bin/bash

echo "Packing..."

echo "printing version"
echo $VERSION_PREFIX
echo $VERSION_SUFFIX
export SUFFIX=$(echo $VERSION_SUFFIX | tr . -)
echo $SUFFIX
echo $VERSION

echo "preparing template build"
export CURRENT_DIRECTORY=$(pwd)
echo $(pwd)
cd ./templates
./build.sh
cd $CURRENT_DIRECTORY

echo "running dotnet packs"

if [ -z "$SUFFIX" ]
then
    dotnet pack ./templates/templates/templates.csproj -o ./templates/artifacts/ --no-build /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./cli/cli --configuration Release /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./client/Packages/com.beamable/Common --configuration Release --include-source --include-symbols /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./microservice/unityEngineStubs --configuration Release --include-source --include-symbols /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./microservice/microservice --configuration Release --include-source --include-symbols /p:NuspecFile=Microservice.nuspec /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION
else
    dotnet pack ./templates/templates/templates.csproj -o ./templates/artifacts/ --no-build --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./cli/cli --configuration Release --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./client/Packages/com.beamable/Common --configuration Release --include-source --include-symbols --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./microservice/unityEngineStubs --configuration Release --include-source --include-symbols --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
    dotnet pack ./microservice/microservice -c Release --include-source --include-symbols  --version-suffix=${SUFFIX-""} /p:NuspecFile=Microservice.nuspec /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION
fi

echo "Installing built package..."
export PATH="$PATH:/root/.dotnet/tools"
dotnet tool install --global --prerelease --add-source ./nupkg/ beamable.tools

echo "Checking built version..."
beam --version #todo: is it possible to assert that the output must match the $VERSION string?

echo "Checking for publish"
echo $DRY_RUN
if [ "$DRY_RUN" = "true" ]
then 
    echo "Not running due to dry run."
    exit $?
else
    dotnet nuget push ./templates/templates/artifacts/Beamable.Templates.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    dotnet nuget push ./cli/cli/nupkg/Beamable.Tools.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    dotnet nuget push ./client/Packages/com.beamable/Common/nupkg/Beamable.Common.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    dotnet nuget push ./microservice/unityEngineStubs/nupkg/Beamable.UnityEngine.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    dotnet nuget push ./microservice/microservice/bin/Release/Beamable.Microservice.Runtime.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
fi
