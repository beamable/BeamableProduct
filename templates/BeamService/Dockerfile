﻿# use the dotnet sdk as a build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine as build-env
ARG TARGETARCH

# <BEAM-CLI-COPY-ENV> this line signals the start of environment variables copies into the built container. Do not remove it. This will be overwritten every time a variable changes in the execution of the CLI.
# </BEAM-CLI-COPY-ENV> this line signals the end of environment variables copies into the built container. Do not remove it.

# <BEAM-CLI-COPY-SRC> this line signals the start of Beamable Project Src copies into the built container. Do not remove it. The content between here and the closing tag will change anytime the Beam CLI modifies dependencies.
# </BEAM-CLI-COPY-SRC> this line signals the end of Beamable Project Src copies. Do not remove it.

WORKDIR /

# build the dotnet program
RUN dotnet publish ${BEAM_CSPROJ_PATH} -p:BeamableVersion=${BEAM_VERSION} -a $TARGETARCH -c release -o /beamApp

# use the dotnet runtime as the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine
WORKDIR /beamApp

# expose the health port
EXPOSE 6565 

# copy the built program
COPY --from=build-env /beamApp .

# when starting the container, run dotnet with the built dll
ENTRYPOINT ["dotnet", "/beamApp/BeamService.dll"]

# Swap entrypoints if the container is exploding and you want to keep it alive indefinitely so you can go look into it.
#ENTRYPOINT ["tail", "-f", "/dev/null"]

