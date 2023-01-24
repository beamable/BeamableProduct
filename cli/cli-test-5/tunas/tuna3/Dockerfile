## TODO: how do we get the actual beamable version?
#FROM beamservice AS build-env
#RUN dotnet --version
#WORKDIR /subapp
#
#COPY tuna3.csproj .
#
#RUN cp /src/baseImageDocs.xml .
#RUN echo $BEAMABLE_SDK_VERSION > /subapp/.beamablesdkversion
#
#EXPOSE 6565
#ENV BEAMABLE_SDK_VERSION_EXECUTION=0.0.0
#ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1
#
#RUN dotnet restore .
#ENTRYPOINT ["dotnet", "run"]



# step 1. Build...
FROM beamservice:latest AS build-env
WORKDIR /subsrc

COPY tuna3.csproj .

#RUN dotnet restore
COPY . .
RUN #cp /src/baseImageDocs.xml .
RUN dotnet publish -c release -o /subapp
RUN echo $BEAMABLE_SDK_VERSION > /subapp/.beamablesdkversion

# step 2. Package using the runtime
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine
WORKDIR /subapp
EXPOSE 6565
COPY --from=build-env /subapp .
COPY --from=build-env /app/baseImageDocs.xml .
ENV BEAMABLE_SDK_VERSION_EXECUTION=0.0.0
ENTRYPOINT ["dotnet", "/subapp/tuna3.dll"]
