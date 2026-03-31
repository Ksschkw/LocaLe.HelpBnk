FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY backend/LocaLe.EscrowApi.csproj backend/
COPY LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj LocaLe.EscrowApi.Tests/
RUN dotnet restore backend/LocaLe.EscrowApi.csproj
RUN dotnet restore LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj

# Copy everything else and build tests
COPY . .
WORKDIR /src
RUN dotnet build LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj -c Release

# Run tests
# Only proceed if tests pass
RUN dotnet test LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj -c Release --no-build -v normal

# Build the main API
WORKDIR /src/backend
RUN dotnet publish LocaLe.EscrowApi.csproj -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "LocaLe.EscrowApi.dll"]
