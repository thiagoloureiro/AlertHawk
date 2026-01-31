# Environment Variables

All environment variables for the Helm chart are set under each component’s `env` block in `values.yaml`. Keys use double underscore `__` for nested configuration (e.g. `ConnectionStrings__SqlConnectionString`).

---

## Common (shared across services)

Used by **auth**, **monitoring**, **notification**, and **metrics-api** where applicable:

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment (`Development`, `Production`) |
| `ConnectionStrings__SqlConnectionString` | SQL Server connection string |
| `Sentry__Enabled` | Enable Sentry (`true` / `false`) |
| `Sentry__Dsn` | Sentry DSN URL |
| `Sentry__Environment` | Sentry environment name |
| `SwaggerUICredentials__username` | Swagger UI basic auth username |
| `SwaggerUICredentials__password` | Swagger UI basic auth password |
| `AzureAd__ClientId` | Azure AD application (client) ID |
| `AzureAd__TenantId` | Azure AD tenant ID |
| `AzureAd__ClientSecret` | Azure AD client secret |
| `AzureAd__Instance` | Azure AD instance URL (e.g. `https://login.microsoftonline.com/`) |
| `Jwt__Key` | JWT signing key |
| `Jwt__Issuers` | Comma-separated JWT issuers |
| `Jwt__Audiences` | Comma-separated JWT audiences |
| `Logging__LogLevel__Default` | Default log level (e.g. `Warning`) |
| `Logging__LogLevel__Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter` | Identity logger level (e.g. `Critical`) |
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | .NET globalization (`false` recommended) |

---

## auth

Under `auth.env` in `values.yaml`. See also [Authentication](/authentication/#environment-variables) docs.

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | **Required.** SQL Server connection string |
| `CacheSettings__CacheProvider` | Cache provider (e.g. `MemoryCache`) |
| `AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__ClientSecret`, `AzureAd__Instance` | Azure AD |
| `AzureAd__CallbackPath` | OIDC callback path (e.g. `/signin-oidc`) |
| `DownstreamApi__BaseUrl` | Microsoft Graph base URL (e.g. `https://graph.microsoft.com/beta`) |
| `DownstreamApi__Scopes` | Scopes (e.g. `User.Read`) |
| `smtpHost` | SMTP server host |
| `smtpPort` | SMTP port |
| `smtpUsername` | SMTP username |
| `smtpPassword` | SMTP password |
| `smtpFrom` | From email address |
| `enableSsl` | Use SSL for SMTP (`true` / `false`) |
| `MOBILE_API_KEY` | API key for mobile Azure auth |
| `Sentry__*`, `SwaggerUICredentials__*`, `Jwt__*`, `Logging__*`, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | As in Common |

---

## monitoring

Under `monitoring.env` in `values.yaml`.

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | **Required.** SQL Server connection string |
| `RabbitMq__Host` | RabbitMQ host |
| `RabbitMq__User` | RabbitMQ username |
| `RabbitMq__Pass` | RabbitMQ password |
| `QueueType` | `RABBITMQ` or `SERVICEBUS` |
| `ServiceBus__ConnectionString` | Azure Service Bus connection string |
| `ServiceBus__QueueName` | Service Bus queue name (e.g. `notifications`) |
| `CacheSettings__CacheProvider` | Cache provider (e.g. `Redis`) |
| `CacheSettings__RedisConnectionString` | Redis connection string |
| `azure_blob_storage_connection_string` | Azure Blob Storage connection string |
| `azure_blob_storage_container_name` | Blob container name |
| `AUTH_API_URL` | Authentication API base URL |
| `CACHE_PARALLEL_TASKS` | Parallel cache tasks (e.g. `10`) |
| `ipgeo_apikey` | IP geolocation API key |
| `enable_location_api` | Enable location API (`true` / `false`) |
| `enable_screenshot` | Enable screenshot (`true` / `false`) |
| `screenshot_wait_time_ms` | Screenshot wait time in ms (e.g. `2000`) |
| `enable_screenshot_storage_account` | Use storage account for screenshots (`true` / `false`) |
| `Downsampling__Active` | Enable downsampling (`true` / `false`) |
| `Downsampling__IntervalInSeconds` | Downsampling interval (e.g. `60`) |
| `CHECK_MONITOR_AFTER_CREATION` | Check monitor after creation (`true` / `false`) |
| `Sentry__*`, `SwaggerUICredentials__*`, `AzureAd__*`, `Jwt__*`, `Logging__*`, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | As in Common |

---

## notification

Under `notification.env` in `values.yaml`.

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | **Required.** SQL Server connection string |
| `RabbitMq__Host` | RabbitMQ host |
| `RabbitMq__User` | RabbitMQ username |
| `RabbitMq__Pass` | RabbitMQ password |
| `QueueType` | `RABBITMQ` or `SERVICEBUS` |
| `ServiceBus__ConnectionString` | Azure Service Bus connection string |
| `ServiceBus__QueueName` | Service Bus queue name |
| `CacheSettings__CacheProvider` | Cache provider (e.g. `MemoryCache`) |
| `slack-webhookurl` | Slack webhook URL |
| `AesKey` | AES encryption key |
| `AesIV` | AES initialization vector |
| `AUTH_API_URL` | Authentication API base URL |
| `PUSHY_API_KEY` | Pushy API key for push notifications |
| `Sentry__*`, `SwaggerUICredentials__*`, `AzureAd__*`, `Jwt__*`, `Logging__*`, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | As in Common |

---

## metrics-api

Under `metrics-api.env` in `values.yaml`.

| Variable | Description |
|----------|-------------|
| `CLICKHOUSE_CONNECTION_STRING` | **Required.** ClickHouse connection (e.g. `http://clickhouse:8123/default` or external URL) |
| `ConnectionStrings__SqlConnectionString` | **Required.** SQL Server connection string |
| `RabbitMq__Host` | RabbitMQ host |
| `RabbitMq__User` | RabbitMQ username |
| `RabbitMq__Pass` | RabbitMQ password |
| `QueueType` | `RABBITMQ` or `SERVICEBUS` |
| `ServiceBus__ConnectionString` | Azure Service Bus connection string |
| `ServiceBus__QueueName` | Service Bus queue name |
| `ENABLE_LOG_CLEANUP` | Enable log cleanup (`true` / `false`) |
| `LOG_CLEANUP_INTERVAL_HOURS` | Cron expression for cleanup (e.g. `0 0 * * *`) |
| `Sentry__*`, `SwaggerUICredentials__*`, `AzureAd__*`, `Jwt__*`, `Logging__*` | As in Common |

---

## ui

The default chart does not define an `env` block for the **ui** component; it only uses `ui.replicas`. If your UI image requires environment variables, add them under `ui.env` in `values.yaml` in the same format as above.

---

## Example: full auth.env block

```yaml
auth:
  replicas: 1
  env:
    ASPNETCORE_ENVIRONMENT: Production
    ConnectionStrings__SqlConnectionString: "Server=sql;Database=alerthawk;User Id=sa;Password=***;"
    Sentry__Enabled: false
    Sentry__Dsn: ""
    Sentry__Environment: Production
    SwaggerUICredentials__username: admin
    SwaggerUICredentials__password: "***"
    CacheSettings__CacheProvider: MemoryCache
    AzureAd__ClientId: "your-client-id"
    AzureAd__TenantId: "your-tenant-id"
    AzureAd__ClientSecret: "***"
    AzureAd__Instance: "https://login.microsoftonline.com/"
    AzureAd__CallbackPath: /signin-oidc
    DownstreamApi__BaseUrl: https://graph.microsoft.com/beta
    DownstreamApi__Scopes: User.Read
    smtpHost: smtp.example.com
    smtpPort: "587"
    smtpUsername: noreply@example.com
    smtpPassword: "***"
    smtpFrom: noreply@example.com
    enableSsl: "true"
    Jwt__Key: "your-jwt-secret-key"
    Jwt__Issuers: "https://your-issuer"
    Jwt__Audiences: "your-audience"
    Logging__LogLevel__Default: Warning
    Logging__LogLevel__Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter: Critical
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: "false"
    MOBILE_API_KEY: "your_mobile_api_key"
```

Replace placeholders and secrets with your actual values.
