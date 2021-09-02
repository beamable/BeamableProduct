cd /D "%~dp0"
Rem  -------------------
Rem  Windows
Rem  -------------------

Rem  Build latest copy of shared library.
Rem  On windows, this won't work. We need a separate script for windows.
dotnet publish ..\\client\\Packages\\com.beamable\\common\\ -c release -o .\\microservice\\lib
dotnet publish ..\\client\\Packages\\com.beamable.server\\SharedRuntime -c release -o .\\microservice\\lib
dotnet publish .\\unityEngineStubs -c release -o .\\microservice\\lib

Rem  Build image
docker build -t beamservice .\\microservice

Rem  Clean up lib folder
@RD /S /Q ".\microservice\lib"
