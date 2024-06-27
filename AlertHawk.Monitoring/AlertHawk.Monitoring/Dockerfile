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
    && apk add --no-cache \
    wget \
    gnupg \
    libc6-compat \
    nss \
    alsa-lib \
    at-spi2-atk \
    cups-libs \
    dbus-libs \
    eudev-libs \
    expat \
    flac \
    harfbuzz \
    libevent \
    libffi \
    libjpeg-turbo \
    libpng \
    libwebp \
    libxcomposite \
    libxrandr \
    libxscrnsaver \
    nodejs \
    ttf-freefont

    # Download and install Google Chrome
RUN wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb \
&& mkdir -p /opt/google/chrome \
&& dpkg -x google-chrome-stable_current_amd64.deb /opt/google/chrome \
&& ln -s /opt/google/chrome/opt/google/chrome/chrome /usr/bin/google-chrome \
&& rm google-chrome-stable_current_amd64.deb

# Cleanup
RUN rm -rf /var/cache/apk/*

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Monitoring.dll"]