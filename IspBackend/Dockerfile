# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["IspBackend.csproj", "./"]
RUN dotnet restore "IspBackend.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "IspBackend.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "IspBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install fonts for PDF generation and curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends \
    fontconfig \
    fonts-liberation \
    fonts-dejavu-core \
    curl \
    && rm -rf /var/lib/apt/lists/* \
    && fc-cache -f -v

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Copy published output
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:5000/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "IspBackend.dll"]
