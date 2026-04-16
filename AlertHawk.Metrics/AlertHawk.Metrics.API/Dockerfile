# ---------- Base runtime (hardened, no shell) ----------
FROM dhi.io/aspnetcore:10-alpine3.22 AS base
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
USER 65532
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# ---------- Cert stage (has apk + shell) ----------
FROM alpine:3.22 AS certs
RUN apk add --no-cache ca-certificates \
 && update-ca-certificates


# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.Metrics.API/AlertHawk.Metrics.API.csproj", "AlertHawk.Metrics.API/"]
RUN dotnet restore "./AlertHawk.Metrics.API/AlertHawk.Metrics.API.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Metrics.API"
RUN dotnet build "./AlertHawk.Metrics.API.csproj" -c $BUILD_CONFIGURATION -o /app/build


# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlertHawk.Metrics.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


# ---------- Final ----------
FROM base AS final
WORKDIR /app

# Copy trusted CA certs into hardened image
COPY --from=certs /etc/ssl/certs /etc/ssl/certs

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Metrics.API.dll"]
