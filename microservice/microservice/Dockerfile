# BUILD AND REPACKAGE DOTNET SDK.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

RUN dotnet --version
# commit the built sdk version to the base image...
ARG BEAMABLE_SDK_VERSION="0.0.0"
ENV BEAMABLE_SDK_VERSION=$BEAMABLE_SDK_VERSION

WORKDIR /src
COPY microservice.csproj .
RUN dotnet restore ./microservice.csproj
COPY . .
RUN dotnet publish ./microservice.csproj -c release -o /app

WORKDIR /app
ENTRYPOINT ["dotnet", "/app/microservice.dll"]
