#!/bin/bash
echo "Packing Base Nuget Package..."

cd ./microservice/microservice
echo "printing version"
echo $VERSION_PREFIX
echo $VERSION_SUFFIX
export SUFFIX=$(echo $VERSION_SUFFIX | tr . -)
echo $SUFFIX
if [ -z "$SUFFIX" ]
then 
    dotnet pack -c Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg /p:NuspecFile=Microservice.nuspec /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION
else
    dotnet pack -c Release --include-source -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg  --version-suffix=${SUFFIX-""} /p:NuspecFile=Microservice.nuspec /p:VersionPrefix=$VERSION_PREFIX /p:CombinedVersion=$VERSION
fi

echo "Checking for publish"
echo $DRY_RUN
if [ "$DRY_RUN" = "true" ] 
then 
    echo "Not running due to dry run."
    exit $?
else
    dotnet nuget push ./bin/Release/Beamable.Microservice.Runtime.${VERSION}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
    dotnet nuget push ./bin/Release/Beamable.Microservice.Runtime.${VERSION}.snupkg --source https://api.nuget.org/v3/index.json --api-key ${NUGET_TOOLS_KEY}
fi
