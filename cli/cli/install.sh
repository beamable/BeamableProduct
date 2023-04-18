#bin/sh

dotnet pack /p:Version=0.0.0
dotnet tool uninstall beamable.tools -g || true
dotnet tool install --global --version 0.0.0 --add-source ./nupkg/ beamable.tools