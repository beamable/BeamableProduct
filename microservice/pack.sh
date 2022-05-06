dotnet pack ./microservice -c Release \
 -o /Users/chris/Documents/GitHub/EjectedMicroservice/MyService/LocalNuget \
 --include-source --include-symbols \
 -p:NuspecFile=Microservice.nuspec \
 -p:PackageVersion=0.0.2