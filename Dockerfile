# Giai đoạn build ứng dụng .NET 8
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EcoSyncWeb/EcoSyncWeb.csproj", "EcoSyncWeb/"]
RUN dotnet restore "EcoSyncWeb/EcoSyncWeb.csproj"
COPY . .
WORKDIR "/src/EcoSyncWeb"
RUN dotnet publish -c Release -o /app/publish

# Giai đoạn chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EcoSyncWeb.dll"]
