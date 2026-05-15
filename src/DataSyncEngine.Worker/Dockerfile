FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DataSyncEngine.sln .
COPY src/DataSyncEngine.Contracts/*.csproj src/DataSyncEngine.Contracts/
COPY src/DataSyncEngine.Core/*.csproj src/DataSyncEngine.Core/
COPY src/DataSyncEngine.Infrastructure/*.csproj src/DataSyncEngine.Infrastructure/
COPY src/DataSyncEngine.Worker/*.csproj src/DataSyncEngine.Worker/

RUN dotnet restore src/DataSyncEngine.Worker/DataSyncEngine.Worker.csproj

COPY src/DataSyncEngine.Contracts/ src/DataSyncEngine.Contracts/
COPY src/DataSyncEngine.Core/ src/DataSyncEngine.Core/
COPY src/DataSyncEngine.Infrastructure/ src/DataSyncEngine.Infrastructure/
COPY src/DataSyncEngine.Worker/ src/DataSyncEngine.Worker/

RUN dotnet build src/DataSyncEngine.Worker/DataSyncEngine.Worker.csproj -c Release --no-restore
RUN dotnet publish src/DataSyncEngine.Worker/DataSyncEngine.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DataSyncEngine.Worker.dll"]
