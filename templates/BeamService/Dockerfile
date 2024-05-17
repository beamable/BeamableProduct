ARG BUILD_MODE=beamable_dev

# Processes specific 
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build_game_maker
ONBUILD RUN echo "Making a game-maker build"

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build_beamable_dev
ONBUILD COPY ./BeamableSource ./BeamableSource
ONBUILD RUN dotnet nuget add source ./BeamableSource

# use the dotnet SDK as a build stage
FROM build_${BUILD_MODE} as build-env

# Since generating the common library is optional, the CLI will replace the insert flag below to copy it correctly 
# At the moment, it'll only do so, if the common library project was created at the time of setting up the solution.
# If not, this line will remain so that we can make a command that adds only the common library to your C#MS solution.

# <BEAM-CLI-COPY-SRC> this line signals the start of Beamable Project Src copies into the built container. Do not remove it. The content between here and the closing tag will change anytime the Beam CLI modifies dependencies.

# </BEAM-CLI-COPY-SRC> this line signals the end of Beamable Project Src copies. Do not remove it.

# build the dotnet program
WORKDIR /
RUN dotnet publish /subsrc/BeamService/BeamService.csproj -c release -o /subapp

# use the dotnet runtime as the final stage
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine
WORKDIR /subapp

# expose the health port
EXPOSE 6565 

# copy the built program
COPY --from=build-env /subapp .

# when starting the container, run dotnet with the built dll
ENTRYPOINT ["dotnet", "/subapp/BeamService.dll"]

# Swap entrypoints if the container is exploding and you want to keep it alive indefinitely so you can go look into it.
#ENTRYPOINT ["tail", "-f", "/dev/null"]

