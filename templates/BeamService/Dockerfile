FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BeamService/BeamService.csproj", "BeamService/"]
RUN dotnet restore "BeamService/BeamService.csproj"
COPY . .
WORKDIR "/src/BeamService"
RUN dotnet build "BeamService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BeamService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BeamService.dll"]
