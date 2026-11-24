FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["ECommerce.API/ECommerce.API.csproj", "ECommerce.API/"]
COPY ["ECommerce.Domain/ECommerce.Domain.csproj", "ECommerce.Domain/"]
COPY ["ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "ECommerce.Infrastructure/"]
COPY ["ECommerce.UseCases/ECommerce.UseCases.csproj", "ECommerce.UseCases/"]

RUN dotnet restore "ECommerce.API/ECommerce.API.csproj"

COPY . .

RUN dotnet publish "ECommerce.API/ECommerce.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# The aspnet image has no HTTP client; curl is only here for the HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The base image ships a non-root 'app' user but does not switch to it by default.
USER $APP_UID

EXPOSE 8080

# Liveness only: a database outage should not get the container restarted.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ECommerce.API.dll"]