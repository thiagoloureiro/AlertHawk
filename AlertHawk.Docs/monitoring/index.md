# Monitoring

The **AlertHawk.Monitoring** service runs monitors (HTTP/HTTPS, TCP, Kubernetes), manages monitor groups and alerts, stores history, and publishes alert events to the Notification service via a message queue.

All API routes are prefixed with **`/monitoring`** (e.g. `/monitoring/api/Monitor/monitorList`). Most endpoints require **JWT Bearer** or **Azure AD**; exceptions are noted below.

---

## Overview

- **Monitor types**: HTTP/HTTPS, TCP, Kubernetes (K8s)
- **Monitor groups**: Organize monitors and apply shared settings; access is scoped by user token
- **Alerts**: Monitor failures and conditions that trigger notifications (sent via queue to Notification service)
- **History**: Monitor run history, dashboard data (uptime, response time), retention settings
- **Reports**: Uptime, alerts, response time by group (API key auth)

---

## Environment Variables

Configuration can be set in `appsettings.json` or via environment variables (e.g. Helm chart under `monitoring.env`). Use `__` for nested keys (e.g. `ConnectionStrings__SqlConnectionString`).

### General

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment (`Development`, `Production`) |
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | .NET globalization (`false` recommended) |
| `basePath` | Base path for Swagger (dev) |

### Database

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | **Required.** SQL Server connection string |

### Message queue (RabbitMQ or Azure Service Bus)

| Variable | Description |
|----------|-------------|
| `RabbitMq__Host` | RabbitMQ host |
| `RabbitMq__User` | RabbitMQ username |
| `RabbitMq__Pass` | RabbitMQ password |
| `QueueType` | `RABBITMQ` or `SERVICEBUS` |
| `ServiceBus__ConnectionString` | Azure Service Bus connection string |
| `ServiceBus__QueueName` | Service Bus queue name (e.g. `notifications`) |

### Authentication API

| Variable | Description |
|----------|-------------|
| `AUTH_API_URL` | **Required.** Base URL of the Authentication API (used to resolve user/group from JWT) |

### Cache

| Variable | Description |
|----------|-------------|
| `CacheSettings__CacheProvider` | Cache provider (e.g. `Redis`, `MemoryCache`) |
| `CacheSettings__RedisConnectionString` | Redis connection string (when using Redis) |
| `CACHE_PARALLEL_TASKS` | Parallel cache tasks (e.g. `10`) |

### Azure AD / JWT

| Variable | Description |
|----------|-------------|
| `AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__ClientSecret`, `AzureAd__Instance` | Azure AD (for token validation) |
| `Jwt__Key`, `Jwt__Issuers`, `Jwt__Audiences` | JWT validation |

### Blob storage (screenshots)

| Variable | Description |
|----------|-------------|
| `azure_blob_storage_connection_string` | Azure Blob Storage connection string |
| `azure_blob_storage_container_name` | Blob container name |
| `enable_screenshot_storage_account` | Use storage account for screenshots (`true` / `false`) |
| `screenshot_folder` | Local screenshot folder (e.g. `/screenshots/`) |

### Screenshot and location

| Variable | Description |
|----------|-------------|
| `enable_screenshot` | Enable HTTP screenshot capture (`true` / `false`) |
| `screenshot_wait_time_ms` | Wait time in ms before screenshot (e.g. `2000`) |
| `ipgeo_apikey` | IP geolocation API key |
| `enable_location_api` | Enable location API (`true` / `false`) |

### Downsampling and behavior

| Variable | Description |
|----------|-------------|
| `Downsampling__Active` | Enable downsampling for history (`true` / `false`) |
| `Downsampling__IntervalInSeconds` | Downsampling interval (e.g. `60`) |
| `CHECK_MONITOR_AFTER_CREATION` | Run check immediately after monitor creation (`true` / `false`) |
| `HTTP_RETRY_INTERVAL_MS` | HTTP retry interval in milliseconds |
| `monitor_region` | Optional monitor region label |

### Sentry and logging

| Variable | Description |
|----------|-------------|
| `Sentry__Enabled`, `Sentry__Dsn`, `Sentry__Environment` | Sentry error tracking |
| `Logging__LogLevel__Default` | Default log level |
| `Logging__LogLevel__Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter` | Identity logger level |

### Swagger (development)

| Variable | Description |
|----------|-------------|
| `SwaggerUICredentials__username`, `SwaggerUICredentials__password` | Basic auth for Swagger UI |

---

## API Controllers

Base path: **`/monitoring/api`**. Auth: **JWT Bearer** or **Azure AD** unless noted.

### Monitor — `/monitoring/api/Monitor/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/monitor/{id}` | Bearer | Get monitor by ID |
| `GET` | `/monitorStatusDashboard/{environment}` | Bearer | Status dashboard for current agent |
| `GET` | `/monitorAgentStatus` | Bearer | Agent status (master node, task counts) |
| `GET` | `/monitorList` | Bearer | List all monitors |
| `GET` | `/monitorListByTag/{tag}` | Bearer | Monitors by tag |
| `GET` | `/monitorTagList` | Bearer | List of monitor tags |
| `GET` | `/monitorListByMonitorGroupIds/{environment}` | Bearer | Monitors by user's group IDs |
| `GET` | `/allMonitorAgents` | Bearer | List all monitor agents |
| `POST` | `/createMonitorHttp` | Bearer | Create HTTP monitor |
| `POST` | `/updateMonitorHttp` | Bearer | Update HTTP monitor |
| `POST` | `/createMonitorTcp` | Bearer | Create TCP monitor |
| `POST` | `/updateMonitorTcp` | Bearer | Update TCP monitor |
| `POST` | `/createMonitorK8s` | Bearer | Create K8s monitor |
| `POST` | `/UpdateMonitorK8s` | Bearer | Update K8s monitor |
| `POST` | `/clone/{id}` | Bearer | Clone monitor |
| `DELETE` | `/deleteMonitor/{id}` | Bearer | Delete monitor |
| `PUT` | `/pauseMonitor/{id}/{paused}` | Bearer | Pause/resume monitor |
| `PUT` | `/pauseMonitorByGroupId/{groupId}/{paused}` | Bearer | Pause/resume by group |
| `GET` | `/getMonitorFailureCount/{days}` | Bearer | Failure count |
| `GET` | `/getMonitorHttpByMonitorId/{monitorId}` | Bearer | HTTP monitor by ID |
| `GET` | `/getMonitorTcpByMonitorId/{monitorId}` | Bearer | TCP monitor by ID |
| `GET` | `/getMonitorK8sByMonitorId/{monitorId}` | Bearer | K8s monitor by ID |
| `GET` | `/GetMonitorCount` | Bearer | Monitor count |
| `GET` | `/GetMonitorJsonBackup` | Admin | Backup monitors as JSON |
| `POST` | `/UploadMonitorJsonBackup` | Admin | Restore from JSON backup |
| `PUT` | `/setMonitorExecutionDisabled/{disabled}` | Admin | Disable/enable all monitor execution |
| `GET` | `/getMonitorExecutionStatus` | Bearer | Execution and maintenance status |
| `PUT` | `/setMaintenanceWindow` | Admin | Set maintenance window (body: StartUtc, EndUtc) |
| `GET` | `/getMaintenanceWindow` | Admin | Get maintenance window |

### MonitorGroup — `/monitoring/api/MonitorGroup/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/monitorGroupList` | Bearer | List all groups |
| `GET` | `/monitorGroupListByUser` | Bearer | Groups by user token |
| `GET` | `/monitorDashboardGroupListByUser` | Bearer | Groups with dashboard data by user |
| `GET` | `/monitorDashboardGroupListByUser/{environment}` | Bearer | Same by environment |
| `GET` | `/monitorGroup/{id}` | Bearer | Group by ID |
| `POST` | `/addMonitorToGroup` | Bearer | Add monitors to group |
| `POST` | `/addMonitorGroup` | Bearer | Create group |
| `POST` | `/updateMonitorGroup` | Bearer | Update group |
| `DELETE` | `/deleteMonitorGroup/{id}` | Bearer | Delete group |
| `DELETE` | `/removeMonitorFromGroup` | Bearer | Remove monitors from group |

### MonitorAlert — `/monitoring/api/MonitorAlert/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/monitorAlerts/{monitorId}/{days}` | Bearer | Alerts for monitor (optional environment) |
| `GET` | `/monitorAlerts/{monitorId}/{days}/{environment}` | Bearer | Alerts by environment |
| `GET` | `/monitorAlertsReport/{monitorId}/{days}/{environment}/{reportType}` | Bearer | Alerts report (e.g. Excel) |
| `GET` | `/monitorAlertsByMonitorGroup/{monitorGroupId}/{days}` | Bearer | Alerts by group |

### MonitorHistory — `/monitoring/api/MonitorHistory/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/MonitorHistory/{id}` | Bearer | History for monitor (limited rows) |
| `GET` | `/MonitorHistoryByIdDays/{id}/{days}` | Bearer | History by ID and days |
| `GET` | `/MonitorHistoryByIdDays/{id}/{days}/{downSampling}/{downSamplingFactor}` | Bearer | With downsampling |
| `GET` | `/MonitorDashboardData/{id}` | Bearer | Dashboard data (uptime, cert, etc.) |
| `POST` | `/MonitorDashboardDataList` | Bearer | Dashboard data for list of IDs |
| `DELETE` | (query: days) | Admin | Delete history older than days |
| `GET` | `/GetMonitorHistoryCount` | Bearer | History row count |
| `GET` | `/GetMonitorHistoryRetention` | Bearer | Retention settings |
| `POST` | `/SetMonitorHistoryRetention` | Admin | Set retention (body: HistoryDaysRetention) |
| `GET` | `/GetMonitorSecurityHeaders/{id}` | Bearer | Latest security headers for HTTP monitor |

### MonitorNotification — `/monitoring/api/MonitorNotification/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/monitorNotifications/{id}` | Bearer | Notifications by monitor ID |
| `POST` | `/addMonitorNotification` | Bearer | Add notification to monitor |
| `POST` | `/removeMonitorNotification` | Bearer | Remove notification from monitor |
| `POST` | `/addMonitorGroupNotification` | Bearer | Add notification to group |
| `POST` | `/removeMonitorGroupNotification` | Bearer | Remove notification from group |

### MonitorType — `/monitoring/api/MonitorType/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/` | Bearer | List monitor types (cached) |

### MonitorReport — `/monitoring/api/MonitorReport/*` (API key auth)

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/Uptime/{groupId}/{hours}` | API Key | Uptime report by group |
| `GET` | `/Uptime/{groupId}/{hours}/{filter}` | API Key | Uptime with filter |
| `GET` | `/UptimeByStartAndEndDate/{groupId}/{startDate}/{endDate}` | API Key | Uptime by date range |
| `GET` | `/Alert/{groupId}/{hours}` | API Key | Alerts by group |
| `GET` | `/ResponseTime/{groupId}/{hours}` | API Key | Response time by group |
| `GET` | `/ResponseTime/{groupId}/{hours}/{filter}` | API Key | Response time with filter |

### HealthCheck — `/monitoring/api/HealthCheck`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/` | None | Health/version string |

### Version — `/monitoring/api/Version`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/` | None | API version |

---

## Helm Chart Reference

In the [Helm chart](/helm/), Monitoring is configured under the `monitoring` section. Set `monitoring.env` with the variables above (e.g. `ConnectionStrings__SqlConnectionString`, `AUTH_API_URL`, `RabbitMq__Host`, cache, Azure AD, JWT, blob/screenshot, downsampling, etc.). See [Environment variables](/helm/environment-variables#monitoring) in the Helm docs.
