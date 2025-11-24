FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
RUN apk update && apk upgrade
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.Metrics.API/AlertHawk.Metrics.API.csproj", "AlertHawk.Metrics.API/"]
RUN dotnet restore "AlertHawk.Metrics.API/AlertHawk.Metrics.API.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Metrics.API"
RUN dotnet build "./AlertHawk.Metrics.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlertHawk.Metrics.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Metrics.API.dll"]
