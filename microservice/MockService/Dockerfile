FROM mcr.microsoft.com/dotnet/sdk:5.0


WORKDIR /src
COPY mockservice.csproj .
RUN dotnet restore ./mockservice.csproj
COPY . .
RUN dotnet publish ./mockservice.csproj -c release -o /app

WORKDIR /app
ENTRYPOINT ["dotnet", "BeamableMockService.dll"]
