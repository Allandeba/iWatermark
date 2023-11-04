ARG ARCH=amd64
ARG VERSION=7.0
ARG TAG=$VERSION-bullseye-slim-$ARCH
FROM mcr.microsoft.com/dotnet/sdk:$VERSION AS build
WORKDIR /app

EXPOSE 80 443

# copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
WORKDIR /app
RUN dotnet publish -c release -o out --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:$TAG
WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "iWatermark.dll"]