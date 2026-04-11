# ----- FRONTEND BUILD STAGE -----
FROM node:20-alpine AS frontend-build
WORKDIR /src/frontend
# Copy package files first for layer caching
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
# Copy the rest of the frontend source
COPY frontend/ ./
# Build the frontend (vite.config.js outputs to ../backend/wwwroot)
RUN npm run build

# ----- BACKEND BUILD STAGE -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY backend/LocaLe.EscrowApi.csproj backend/
COPY LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj LocaLe.EscrowApi.Tests/
RUN dotnet restore backend/LocaLe.EscrowApi.csproj
RUN dotnet restore LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj

# Copy everything else
COPY . .

# Copy the built frontend artifacts into the backend wwwroot so publish includes them
COPY --from=frontend-build /src/backend/wwwroot backend/wwwroot/

# Build tests
WORKDIR /src
RUN dotnet build LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj -c Release

# Run tests
# Only proceed if tests pass
RUN dotnet test LocaLe.EscrowApi.Tests/LocaLe.EscrowApi.Tests.csproj -c Release --no-build -v normal

# Build the main API
WORKDIR /src/backend
RUN dotnet publish LocaLe.EscrowApi.csproj -c Release -o /app/publish

# ----- FINAL RUNTIME IMAGE -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "LocaLe.EscrowApi.dll"]
