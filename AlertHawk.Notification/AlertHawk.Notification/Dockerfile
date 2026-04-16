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
COPY ["AlertHawk.Notification/AlertHawk.Notification.csproj", "AlertHawk.Notification/"]
COPY ["AlertHawk.Notification.Domain/AlertHawk.Notification.Domain.csproj", "AlertHawk.Notification.Domain/"]
COPY ["AlertHawk.Notification.Infrastructure/AlertHawk.Notification.Infrastructure.csproj", "AlertHawk.Notification.Infrastructure/"]
RUN dotnet restore "./AlertHawk.Notification/AlertHawk.Notification.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Notification"
RUN dotnet build "./AlertHawk.Notification.csproj" -c $BUILD_CONFIGURATION -o /app/build


# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlertHawk.Notification.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


# ---------- Final ----------
FROM base AS final
WORKDIR /app

# Copy trusted CA certs into hardened image
COPY --from=certs /etc/ssl/certs /etc/ssl/certs

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Notification.dll"]
