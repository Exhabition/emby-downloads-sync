FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/EmbyDownloadsSync/*.csproj ./src/EmbyDownloadsSync/
COPY src/Emby.ApiClient/*.csproj ./src/Emby.ApiClient/
RUN dotnet restore ./src/EmbyDownloadsSync/EmbyDownloadsSync.csproj

COPY src/ ./src/

RUN dotnet publish ./src/EmbyDownloadsSync/EmbyDownloadsSync.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "EmbyDownloadsSync.dll"]
