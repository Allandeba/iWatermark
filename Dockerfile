FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY iWatermark.csproj ./
RUN dotnet restore iWatermark.csproj
COPY . .
RUN dotnet publish iWatermark.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/ ./
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl --fail --silent --max-time 4 --output /dev/null http://127.0.0.1:8080/ || exit 1
ENTRYPOINT ["dotnet", "iWatermark.dll"]
