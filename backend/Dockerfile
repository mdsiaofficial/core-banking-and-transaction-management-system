# Multi-stage Dockerfile for Bank.Api (.NET 10)
# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and build configuration files
COPY Directory.Build.props Directory.Packages.props global.json Bank.sln ./
COPY src/Bank.Domain/Bank.Domain.csproj src/Bank.Domain/
COPY src/Bank.Application/Bank.Application.csproj src/Bank.Application/
COPY src/Bank.Infrastructure/Bank.Infrastructure.csproj src/Bank.Infrastructure/
COPY src/Bank.Api/Bank.Api.csproj src/Bank.Api/
COPY src/Bank.Migrator/Bank.Migrator.csproj src/Bank.Migrator/
COPY tests/Bank.Domain.Tests/Bank.Domain.Tests.csproj tests/Bank.Domain.Tests/
COPY tests/Bank.Application.Tests/Bank.Application.Tests.csproj tests/Bank.Application.Tests/
COPY tests/Bank.Api.IntegrationTests/Bank.Api.IntegrationTests.csproj tests/Bank.Api.IntegrationTests/
COPY tests/Bank.ConcurrencyTests/Bank.ConcurrencyTests.csproj tests/Bank.ConcurrencyTests/
COPY tests/Bank.Architecture.Tests/Bank.Architecture.Tests.csproj tests/Bank.Architecture.Tests/

# Restore dependencies
RUN dotnet restore Bank.sln

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/Bank.Api/Bank.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=3 \
  CMD timeout 2 bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080' || exit 1

USER $APP_UID

ENTRYPOINT ["dotnet", "Bank.Api.dll"]
