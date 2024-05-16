IF "%1"=="" ( SET "VERSION=0.0.123" ) ELSE ( SET "VERSION=%1" )

dotnet pack -p:PackageVersion=%VERSION%
taskkill /IM "beam.exe" /F || true
dotnet tool uninstall beamable.tools -g || true
dotnet tool install --global --version %VERSION% --add-source ./nupkg/ beamable.tools
