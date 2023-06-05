dotnet pack /p:Version=1.15.2
taskkill /IM "beam.exe" /F || true
dotnet tool uninstall beamable.tools -g || true
dotnet tool install --global --version 1.15.2 --add-source ./nupkg/ beamable.tools