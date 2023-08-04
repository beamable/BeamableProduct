# BUILD AND REPACKAGE DOTNET SDK.
ARG BUILD_TARGET_FRAMEWORK="6.0"
FROM mcr.microsoft.com/dotnet/sdk:${BUILD_TARGET_FRAMEWORK} AS build-env

# need to get the arg again after the FROM statement
ARG BUILD_TARGET_FRAMEWORK="6.0"

RUN dotnet --version
# commit the built sdk version to the base image...
ARG BEAMABLE_SDK_VERSION="0.0.0"
ENV BEAMABLE_SDK_VERSION=$BEAMABLE_SDK_VERSION

WORKDIR /src
COPY microservice.csproj .
RUN dotnet restore ./microservice.csproj
COPY . .
RUN dotnet publish ./microservice.csproj -c release --framework net${BUILD_TARGET_FRAMEWORK} -o /app

WORKDIR /app
ENTRYPOINT ["dotnet", "/app/microservice.dll"]
