# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["LocaLe.EscrowApi.csproj", "./"]
RUN dotnet restore "LocaLe.EscrowApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "LocaLe.EscrowApi.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "LocaLe.EscrowApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Image Assembly
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment Defaults for Northflank (Override in UI Dashboard)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection=""

ENTRYPOINT ["dotnet", "LocaLe.EscrowApi.dll"]
