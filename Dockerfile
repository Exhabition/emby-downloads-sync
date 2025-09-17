FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY src/EmbyDownloadsSync/*.csproj ./Project/
RUN dotnet restore ./Project/EmbyDownloadsSync.csproj

COPY src/EmbyDownloadsSync ./Project

RUN dotnet publish ./Project/EmbyDownloadsSync.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "EmbyDownloadsSync.dll"]
