# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first, so this restore layer is cached until a package changes
COPY src/OrderFlow.Domain/OrderFlow.Domain.csproj               src/OrderFlow.Domain/
COPY src/OrderFlow.Application/OrderFlow.Application.csproj     src/OrderFlow.Application/
COPY src/OrderFlow.Infrastructure/OrderFlow.Infrastructure.csproj src/OrderFlow.Infrastructure/
COPY src/OrderFlow.Api/OrderFlow.Api.csproj                     src/OrderFlow.Api/
RUN dotnet restore src/OrderFlow.Api/OrderFlow.Api.csproj

# Now copy the source and publish
COPY src/ src/
RUN dotnet publish src/OrderFlow.Api/OrderFlow.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is used by the container healthcheck; the runtime image doesn't include it
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Serilog writes log files here, and the non-root user needs permission to create them
RUN mkdir -p /app/logs && chown $APP_UID /app/logs

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Api.dll"]