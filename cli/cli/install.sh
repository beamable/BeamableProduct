#bin/sh

dotnet pack /p:Version=0.0.0
dotnet tool uninstall beamcli -g || true
dotnet tool uninstall cli -g || true
dotnet tool install --global --add-source ./nupkg/ BeamCli