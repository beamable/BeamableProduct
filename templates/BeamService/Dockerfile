# TODO: how do we get the actual beamable version?
FROM beamservice AS build-env
RUN dotnet --version
WORKDIR /subapp

COPY BeamService.csproj .

RUN cp /src/baseImageDocs.xml .
RUN echo $BEAMABLE_SDK_VERSION > /subapp/.beamablesdkversion

EXPOSE 6565
ENV BEAMABLE_SDK_VERSION_EXECUTION=0.0.0
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1

RUN dotnet restore .
ENTRYPOINT ["dotnet", "watch"]
