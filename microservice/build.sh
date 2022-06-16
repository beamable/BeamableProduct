cd "${0%/*}"
export lib_path="./microservice/lib"

# build latest copy of shared library.
# on windows, this won't work. We need a separate script for windows.
/usr/local/share/dotnet/dotnet publish ../client/Packages/com.beamable/Common -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ../client/Packages/com.beamable.server/SharedRuntime -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ../client/Packages/com.beamable.server/Runtime/Common -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ./unityEngineStubs -c release -o $lib_path
/usr/local/share/dotnet/dotnet publish ./beamable.tooling.common -c release -o $lib_path

# optionally uncomment to run tests on build
# /usr/local/share/dotnet/dotnet test ./microserviceTests/

# build image
docker build -t beamservice ./microservice

# clean up lib folder
rm -rf $lib_path
