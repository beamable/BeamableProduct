cd /D "%~dp0"
Rem  -------------------
Rem  Windows
Rem  -------------------
set dotnetpath="C:\Program Files\dotnet\dotnet.exe"
set docker_platform="C:\Program Files\dotnet\dotnet.exe"
IF DEFINED BEAMABLE_MICROSERVICE_ARCH (
    set docker_platform=BEAMABLE_MICROSERVICE_ARCH
) ELSE (
    set docker_platform="linux/amd64"
)
Rem  Build latest copy of shared library.
Rem  On windows, this won't work. We need a separate script for windows.
%dotnetpath% publish ..\\client\\Packages\\com.beamable\\common\\ -c release -o .\\microservice\\lib
%dotnetpath% publish ..\\client\\Packages\\com.beamable.server\\SharedRuntime -c release -o .\\microservice\\lib
%dotnetpath% publish ..\\client\\Packages\\com.beamable.server\\Runtime\\Common -c release -o .\\microservice\\lib
%dotnetpath% publish .\\unityEngineStubs -c release -o .\\microservice\\lib
%dotnetpath% publish .\\unityEngineStubs.addressables -c release -o .\\microservice\\lib
%dotnetpath% publish .\\beamable.tooling.common -c release -o .\\microservice\\lib

Rem  Build image
REM docker build -t beamservice .\\microservice

docker buildx build --load --platform %docker_platform% -t beamservice .\\microservice


Rem  Clean up lib folder
@RD /S /Q ".\microservice\lib"
