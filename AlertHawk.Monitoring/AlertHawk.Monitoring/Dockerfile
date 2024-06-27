#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
#RUN apt-get update && apt-get upgrade -y
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.Monitoring/AlertHawk.Monitoring.csproj", "AlertHawk.Monitoring/"]
RUN dotnet restore "./AlertHawk.Monitoring/./AlertHawk.Monitoring.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Monitoring"
RUN dotnet build "./AlertHawk.Monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlertHawk.Monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apk update \
    && apk add --no-cache wget gnupg \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | gpg --import - \
    && echo "https://dl-ssl.google.com/linux/linux_signing_key.pub" >> /etc/apk/keys/google-chrome.pub \
    && echo "@edge http://dl-3.alpinelinux.org/alpine/edge/community" >> /etc/apk/repositories \
    && apk update \
    && apk add --no-cache google-chrome-stable \
    && rm -rf /var/cache/apk/*

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Monitoring.dll"]