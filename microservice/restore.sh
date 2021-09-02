# Restore all projects
dotnet restore ../client/Packages/com.beamable/Common/beamable.common.csproj
dotnet restore ../client/Packages/com.beamable.server/SharedRuntime/beamable.server.csproj

dotnet restore ./microservice/microservice.csproj
