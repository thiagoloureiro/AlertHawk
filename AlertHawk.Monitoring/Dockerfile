#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get upgrade -y
# Install required packages explicitly
# Install required packages explicitly and ensure they are up-to-date
RUN apt-get install -y --no-install-recommends \
    zlib1g \
    libperl5.36 \
    perl \
    perl-base \
    perl-modules-5.36 \
    krb5-locales \
    libexpat1 \
    libgssapi-krb5-2 \
    libharfbuzz0b \
    libk5crypto3 \
    libkrb5-3 \
    libkrb5support0 \
    libldap-2.5-0 \
    libldap-common \
    libnss3 \
    libtiff6 \
    libxml2 \
    xdg-utils \
    libsqlite3-0 \
    libgcrypt20 \
    openssl \
    libavahi-client3 \
    libavahi-common-data \
    libavahi-common3 \
    libpam-modules \
    libpam-modules-bin \
    libpam-runtime \
    libpam0g \
    login \
    passwd \
    wget \
    libnghttp2-14 \
    libpython3.11-minimal \
    libpython3.11-stdlib \
    python3.11 \
    python3.11-minimal \
    cpp-12 \
    gcc-12-base \
    libgcc-s1 \
    libstdc++6 \
    libcups2 \
    libllvm15 \
    bash \
    libegl-mesa0

# Clean up the package lists and cache to reduce the image size
RUN apt-get clean && rm -rf /var/lib/apt/lists/*

USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
RUN apt-get update \
    && apt-get install -y wget gnupg \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Monitoring.dll"]
