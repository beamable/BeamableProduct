IF "%1"=="" ( SET "FOLDER=client/Packages" ) ELSE ( SET "FOLDER=%1" )

dotnet tool restore
dotnet-format -f %FOLDER%
