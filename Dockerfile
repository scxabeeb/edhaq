# ================================
#   e-Dhaq — .NET 9 Multi-Stage Dockerfile
#   ASP.NET Core 9 Razor Pages + REST API
# ================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore caching
COPY ["eDhaq.sln", "./"]
COPY ["src/eDhaq.Models/eDhaq.Models.csproj", "src/eDhaq.Models/"]
COPY ["src/eDhaq.Common/eDhaq.Common.csproj", "src/eDhaq.Common/"]
COPY ["src/eDhaq.Data/eDhaq.Data.csproj", "src/eDhaq.Data/"]
COPY ["src/eDhaq.Repositories/eDhaq.Repositories.csproj", "src/eDhaq.Repositories/"]
COPY ["src/eDhaq.Services/eDhaq.Services.csproj", "src/eDhaq.Services/"]
COPY ["src/eDhaq.Web/eDhaq.Web.csproj", "src/eDhaq.Web/"]

# Restore NuGet packages
RUN dotnet restore "eDhaq.sln"

# Copy remaining source files
COPY . .

# Publish the Web app (build + publish in one step, preserving per-project output paths)
RUN dotnet publish "src/eDhaq.Web/eDhaq.Web.csproj" -c Release -o /app/publish

# ================================
#   Runtime Stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser -G appuser appuser

# Copy published output
COPY --from=build /app/publish .

# Make logs directory writable
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# DOTNET_RUNNING_IN_CONTAINER improves startup performance in containers
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_EnableWriteXorExit=false

# ASPNETCORE_URLS is resolved at runtime (not build time) so the
# $PORT env var provided by Railway takes effect correctly.
EXPOSE 80
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://+:${PORT:-80}; exec dotnet eDhaq.Web.dll"]