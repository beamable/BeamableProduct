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

rm -rf ./templates/Data
mkdir ./templates/Data
cp -R ./BeamService/ ./templates/Data/BeamService
cp -R ./CommonLibrary/ ./templates/Data/CommonLibrary
cp -R ./BeamStorage/ ./templates/Data/BeamStorage


cd $CURRENT_DIRECTORY

echo "running dotnet packs"

if [ -z "$SUFFIX" ]
then
    dotnet pack ./cli/cli --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./cli/beamable.common --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./cli/beamable.server.common --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/beamable.tooling.common --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/unityEngineStubs --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/unityenginestubs.addressables --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    
    dotnet build ./templates/MicroserviceSourceGen/MicroserviceSourceGen --configuration Release /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./templates/MicroserviceSourceGen/MicroserviceSourceGen --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    
    dotnet build ./microservice/microservice --configuration Release /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/microservice --configuration Release --no-build --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION /p:InformationalVersion=$VERSION
else
    dotnet pack ./cli/cli --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./cli/beamable.common --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./cli/beamable.server.common --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/beamable.tooling.common --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/unityEngineStubs --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/unityenginestubs.addressables --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    
    dotnet build ./templates/MicroserviceSourceGen/MicroserviceSourceGen --configuration Release --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    dotnet pack ./templates/MicroserviceSourceGen/MicroserviceSourceGen --configuration Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:InformationalVersion=$VERSION
    
    dotnet build ./microservice/microservice -c Release --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION /p:InformationalVersion=$VERSION
    dotnet pack ./microservice/microservice -c Release --no-build --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION /p:InformationalVersion=$VERSION
fi

echo "Installing built package..."
export PATH="$PATH:/root/.dotnet/tools"
dotnet tool install --global --prerelease --add-source ./cli/cli/nupkg beamable.tools

echo "Checking built version..."
beam --version #todo: is it possible to assert that the output must match the $VERSION string?

echo "Checking for publish"
echo $DRY_RUN
if [ "$DRY_RUN" = "true" ]
then 
    echo "Not running due to dry run."
    exit $?
else
    dotnet nuget push ./cli/cli/nupkg/Beamable.Tools.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    dotnet nuget push ./cli/beamable.common/nupkg/Beamable.Common.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    dotnet nuget push ./cli/beamable.server.common/nupkg/Beamable.Server.Common.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    dotnet nuget push ./microservice/beamable.tooling.common/nupkg/Beamable.Tooling.Common.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    dotnet nuget push ./microservice/unityEngineStubs/nupkg/Beamable.UnityEngine.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    dotnet nuget push ./microservice/unityenginestubs.addressables/nupkg/Beamable.UnityEngine.Addressables.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    dotnet nuget push ./microservice/microservice/nupkg/Beamable.Microservice.Runtime.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    echo "source-gen-debug"
    ls ./templates/MicroserviceSourceGen/MicroserviceSourceGen/nupkg/
    dotnet nuget push ./templates/MicroserviceSourceGen/MicroserviceSourceGen/nupkg/Beamable.Microservice.SourceGen.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    
    echo "Upgrading template reference"

    dotnet add ./templates/BeamService/BeamService.csproj package Beamable.Microservice.Runtime --version=$VERSION
    dotnet add ./templates/BeamStorage/BeamStorage.csproj package Beamable.Microservice.Runtime --version=$VERSION

    echo "Packing templates..."
    if [ -z "$SUFFIX" ]
    then
        dotnet pack ./templates/templates/templates.csproj -o ./templates/artifacts/ /p:VersionPrefix=$VERSION_PREFIX
    else
        dotnet pack ./templates/templates/templates.csproj -o ./templates/artifacts/ --version-suffix=${SUFFIX-""} /p:VersionPrefix=$VERSION_PREFIX
    fi

    echo "Pushing template project."
    dotnet nuget push ./templates/artifacts/Beamable.Templates.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
fi
