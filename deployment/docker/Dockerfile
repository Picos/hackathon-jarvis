# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/TeamsAssistBot.Api/TeamsAssistBot.Api.csproj", "src/TeamsAssistBot.Api/"]
COPY ["src/TeamsAssistBot.Core/TeamsAssistBot.Core.csproj", "src/TeamsAssistBot.Core/"]
COPY ["src/TeamsAssistBot.Infrastructure/TeamsAssistBot.Infrastructure.csproj", "src/TeamsAssistBot.Infrastructure/"]
COPY ["nuget.config", "."]

# Restore dependencies
RUN dotnet restore "src/TeamsAssistBot.Api/TeamsAssistBot.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/TeamsAssistBot.Api"
RUN dotnet build "TeamsAssistBot.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "TeamsAssistBot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create the final runtime image
FROM base AS final
WORKDIR /app

# Install additional dependencies if needed
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy the published app
COPY --from=publish /app/publish .

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "TeamsAssistBot.Api.dll"]
