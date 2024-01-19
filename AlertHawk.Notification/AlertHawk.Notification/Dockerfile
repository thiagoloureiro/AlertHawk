FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AlertHawk.Notification/AlertHawk.Notification.csproj", "AlertHawk.Notification/"]
RUN dotnet restore "AlertHawk.Notification/AlertHawk.Notification.csproj"
COPY . .
WORKDIR "/src/AlertHawk.Notification"
RUN dotnet build "AlertHawk.Notification.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AlertHawk.Notification.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AlertHawk.Notification.dll"]
