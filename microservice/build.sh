cd "${0%/*}"
export lib_path="./microservice/lib"

export docker_platform=${BEAMABLE_MICROSERVICE_ARCH-"linux/amd64"}

# build latest copy of shared library.
# on windows, this won't work. We need a separate script for windows.
/usr/local/share/dotnet/dotnet publish ../client/Packages/com.beamable/Common --framework net6.0 -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ../client/Packages/com.beamable.server/SharedRuntime --framework net6.0 -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ../client/Packages/com.beamable.server/Runtime/Common --framework net6.0 -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ./unityEngineStubs --framework net6.0  -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ./unityenginestubs.addressables  --framework net6.0 -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ./beamable.tooling.common --framework net6.0 -c release -o $lib_path


# optionally uncomment to run tests on build
# /usr/local/share/dotnet/dotnet test ./microserviceTests/

# build image
# As of July 21, 2022, being able to load multi-arch builds isn't supported. -> https://github.com/docker/buildx/issues/59
# which means that the following command won't work. Maybe someday it will, which would be nice...
# docker --context=desktop-linux buildx build --load --pull --platform linux/arm64,linux/amd64 -t beamservice ./microservice

# instad, we need to build only one variant, that is controlled via a developer string...
/usr/local/bin/docker buildx build --load --platform $docker_platform -t beamservice ./microservice

# and then build the arm variant...
# docker buildx build --load --platform linux/arm64 -t beamservice ./microservice

# clean up lib folder
rm -rf $lib_path
