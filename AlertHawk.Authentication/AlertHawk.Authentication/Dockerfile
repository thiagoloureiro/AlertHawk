#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
RUN apk update && apk upgrade
RUN apk add icu-libs
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.Authentication/AlertHawk.Authentication.csproj", "AlertHawk.Authentication/"]
RUN dotnet restore "./AlertHawk.Authentication/./AlertHawk.Authentication.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Authentication"
RUN dotnet build "./AlertHawk.Authentication.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlertHawk.Authentication.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Authentication.dll"]