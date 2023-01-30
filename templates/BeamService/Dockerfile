# use the dotnet SDK as a build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env

# copy in required files
#WORKDIR /subsrc
#COPY BeamService/BeamService.csproj ./BeamService
#COPY BeamService/. ./BeamService
#
## copy in any other common libraries
#COPY CommonLibrary/. ./CommonLibrary

WORKDIR /subsrc/BeamServiceCommon
COPY BeamServiceCommon/. .

# copy in required files
WORKDIR /subsrc/BeamService
COPY BeamService/BeamService.csproj .
COPY BeamService/. .

# build the dotnet program
RUN dotnet publish -c release -o /subapp

# use the dotnet runtime as the final stage
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine
WORKDIR /subapp

# expose the health port
EXPOSE 6565 

# copy the built program
COPY --from=build-env /subapp .

# when starting the container, run dotnet with the built dll
ENTRYPOINT ["dotnet", "/subapp/BeamService.dll"]
