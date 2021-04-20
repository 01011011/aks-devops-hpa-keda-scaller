FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

COPY storage-table-queue.csproj .
RUN dotnet restore storage-table-queue.csproj

COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "storage-table-queue.dll"]