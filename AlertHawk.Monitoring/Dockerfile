# Using the official .NET images based on Alpine
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
# Install necessary packages and create user
RUN apk --no-cache add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.Monitoring/AlertHawk.Monitoring.csproj", "AlertHawk.Monitoring/"]
RUN dotnet restore "AlertHawk.Monitoring/AlertHawk.Monitoring.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Monitoring"
RUN dotnet build "AlertHawk.Monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AlertHawk.Monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
# Install additional packages needed
RUN apk add --no-cache chromium
# Setting up the environment for Chromium and WebDriver
ENV CHROME_BIN=/usr/bin/chromium-browser \
    CHROME_DRIVER=/usr/bin/chromedriver
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Monitoring.dll"]
