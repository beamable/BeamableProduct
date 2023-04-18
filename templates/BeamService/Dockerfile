# use the dotnet SDK as a build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env

# copy in required files
WORKDIR /subsrc/BeamServiceCommon
COPY BeamServiceCommon/. .

# <BEAM-CLI-INSERT-FLAG:COPY> do not delete this line. It is used by the beam CLI to insert custom actions

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
