# ---------- Base runtime (hardened, no shell) ----------
FROM dhi.io/aspnetcore:10-alpine3.23 AS base
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
USER 65532
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# ---------- Cert stage (has apk + shell) ----------
FROM alpine:3.22 AS certs
RUN apk add --no-cache ca-certificates \
 && update-ca-certificates


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.FinOps/AlertHawk.FinOps.csproj", "AlertHawk.FinOps/"]
RUN dotnet restore "./AlertHawk.FinOps/AlertHawk.FinOps.csproj"
COPY . .
WORKDIR "/src/AlertHawk.FinOps"
RUN dotnet build "./AlertHawk.FinOps.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ---------- Publish ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AlertHawk.FinOps.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


# ---------- Final ----------
FROM base AS final
WORKDIR /app

# ✅ Copy trusted CA certs into hardened image
COPY --from=certs /etc/ssl/certs /etc/ssl/certs

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.FinOps.dll"]
