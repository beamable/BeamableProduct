cd /D "%~dp0"
Rem  -------------------
Rem  Windows
Rem  -------------------
set dotnetpath="C:\Program Files\dotnet\dotnet.exe"
Rem  Build latest copy of shared library.
Rem  On windows, this won't work. We need a separate script for windows.
%dotnetpath% publish ..\\client\\Packages\\com.beamable\\common\\ -c release -o .\\microservice\\lib
%dotnetpath% publish ..\\client\\Packages\\com.beamable.server\\SharedRuntime -c release -o .\\microservice\\lib
%dotnetpath% publish ..\\client\\Packages\\com.beamable.server\\Runtime\\Common -c release -o .\\microservice\\lib
%dotnetpath% publish .\\unityEngineStubs -c release -o .\\microservice\\lib
%dotnetpath% publish .\\beamable.tooling.common -c release -o .\\microservice\\lib

Rem  Build image
docker build -t beamservice .\\microservice

Rem  Clean up lib folder
@RD /S /Q ".\microservice\lib"
