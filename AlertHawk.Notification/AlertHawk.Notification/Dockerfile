#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.Notification/AlertHawk.Notification.csproj", "AlertHawk.Notification/"]
COPY ["AlertHawk.Notification.Domain/AlertHawk.Notification.Domain.csproj", "AlertHawk.Notification.Domain/"]
COPY ["AlertHawk.Notification.Infrastructure/AlertHawk.Notification.Infrastructure.csproj", "AlertHawk.Notification.Infrastructure/"]
RUN dotnet restore "./AlertHawk.Notification/./AlertHawk.Notification.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Notification"
RUN dotnet build "./AlertHawk.Notification.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlertHawk.Notification.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Notification.dll"]