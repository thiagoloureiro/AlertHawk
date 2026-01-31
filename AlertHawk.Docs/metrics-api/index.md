# Metrics API

The **AlertHawk.Metrics.API** service receives Kubernetes metrics and logs from the Metrics Agent (and other sources), stores them in **ClickHouse**, and exposes APIs to query metrics, pod logs, Kubernetes events, cluster/node status, and (optionally) Azure/cluster prices. It can publish node-status alerts to the Notification service via a message queue.

All API routes are prefixed with **`/metrics`** (e.g. `/metrics/api/metrics/namespace`). Most read endpoints require **JWT Bearer** or **Azure AD**; ingest endpoints (pod/node metrics, pod logs, events) are **AllowAnonymous** so the agent can push without a user token.

---

## Overview

- **ClickHouse**: Stores pod/container metrics, node metrics, pod logs, Kubernetes events, and cluster prices
- **Ingest**: Metrics Agent (or other clients) POST pod metrics, node metrics, pod logs, and events; node status changes can trigger notifications
- **Query**: Get metrics by namespace/node, pod logs, events; list clusters and namespaces; cleanup/retention
- **Prices**: Optional Azure price fetch on node ingest; query cluster prices from ClickHouse
- **Message queue**: RabbitMQ or Azure Service Bus for node-status notifications to AlertHawk.Notification
- **SQL Server**: Used for metrics alerts and metrics notifications (cluster–notification mapping)

---

## Environment Variables

Configuration can be set in `appsettings.json` or via environment variables (e.g. Helm chart under `metrics-api.env`). Use `__` for nested keys.

### General

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment (`Development`, `Production`) |
| `basePath` | Base path for Swagger (dev) |

### ClickHouse

| Variable | Description |
|----------|-------------|
| `CLICKHOUSE_CONNECTION_STRING` | **Required.** ClickHouse connection (e.g. `http://clickhouse:8123/default` or connection-string format) |
| `CLICKHOUSE_TABLE_NAME` | Base table name for metrics (default: `k8s_metrics`) |
| `CLUSTER_NAME` | Optional cluster name used when not provided per request |

### SQL Server

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | SQL Server connection string (for metrics alerts and metrics notifications) |

### Message queue (RabbitMQ or Azure Service Bus)

| Variable | Description |
|----------|-------------|
| `RabbitMq__Host` | RabbitMQ host |
| `RabbitMq__User` | RabbitMQ username |
| `RabbitMq__Pass` | RabbitMQ password |
| `QueueType` | `RABBITMQ` or `SERVICEBUS` |
| `ServiceBus__ConnectionString` | Azure Service Bus connection string |
| `ServiceBus__QueueName` | Service Bus queue name (e.g. `notifications`) |

### Log cleanup (Hangfire)

| Variable | Description |
|----------|-------------|
| `ENABLE_LOG_CLEANUP` | Enable scheduled log cleanup (`true` / `false`) |
| `LOG_CLEANUP_INTERVAL_HOURS` | Cron expression for cleanup (e.g. `0 0 * * *` = daily midnight) |

### Azure AD / JWT

| Variable | Description |
|----------|-------------|
| `AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__ClientSecret`, `AzureAd__Instance` | Azure AD (for token validation) |
| `Jwt__Key`, `Jwt__Issuers`, `Jwt__Audiences` | JWT validation |

### Cache

| Variable | Description |
|----------|-------------|
| `CacheSettings__CacheProvider` | Cache provider (e.g. `MemoryCache`) |

### Sentry and logging

| Variable | Description |
|----------|-------------|
| `Sentry__Enabled`, `Sentry__Dsn`, `Sentry__Environment` | Sentry error tracking |
| `Logging__LogLevel__Default` | Default log level |
| `Logging__LogLevel__Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter` | Identity logger level |

### Swagger (development)

| Variable | Description |
|----------|-------------|
| `SwaggerUICredentials__username`, `SwaggerUICredentials__password` | Basic auth for Swagger UI (if used) |

---

## API Controllers

Base path: **`/metrics/api`**. Auth: **JWT Bearer** or **Azure AD** unless noted.

### Metrics — `/metrics/api/metrics/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/metrics/namespace` | Bearer | Pod metrics by query (namespace, minutes, clusterName) |
| `GET` | `/metrics/namespace/{namespace}` | Bearer | Pod metrics for a namespace |
| `GET` | `/metrics/node` | Bearer | Node metrics by query (nodeName, minutes, clusterName) |
| `GET` | `/metrics/node/{nodeName}` | Bearer | Node metrics for a node |
| `POST` | `/metrics/pod` | None | Write pod/container metrics (ingest) |
| `POST` | `/metrics/node` | None | Write node metrics (ingest); may trigger node-status notification and Azure price fetch |
| `GET` | `/metrics/clusters` | Bearer | List unique cluster names |
| `GET` | `/metrics/namespaces` | Bearer | List unique namespaces (optional clusterName) |
| `DELETE` | `/metrics/cleanup` | Bearer | Cleanup metrics tables (query: days; 0 = truncate) |
| `POST` | `/metrics/pod/log` | None | Write pod log (ingest) |
| `GET` | `/metrics/pod/log` | Bearer | Get pod logs (namespace, pod, container, minutes, limit, clusterName) |
| `GET` | `/metrics/pod/log/namespace/{namespace}` | Bearer | Pod logs for namespace |
| `GET` | `/metrics/pod/log/namespace/{namespace}/pod/{pod}` | Bearer | Pod logs for pod |

### Events — `/metrics/api/events/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/events` | None | Write Kubernetes event (ingest) |
| `GET` | `/events` | Bearer | Get Kubernetes events (filters: namespace, involvedObjectKind/Name, eventType, minutes, limit, clusterName) |
| `GET` | `/events/namespace/{namespace}` | Bearer | Events for namespace |

### ClickHouse — `/metrics/api/clickhouse/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/clickhouse/table-sizes` | Bearer | Table sizes from ClickHouse system.parts |

### MetricsAlert — `/metrics/api/MetricsAlert/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/MetricsAlert/metricsAlerts` | Bearer | Metrics alerts (clusterName, nodeName, days) |
| `GET` | `/MetricsAlert/metricsAlerts/cluster/{clusterName}` | Bearer | Alerts by cluster |
| `GET` | `/MetricsAlert/metricsAlerts/cluster/{clusterName}/node/{nodeName}` | Bearer | Alerts by cluster and node |

### MetricsNotification — `/metrics/api/MetricsNotification/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/MetricsNotification/clusterNotifications/{clusterName}` | Bearer | Notifications for cluster |
| `POST` | `/MetricsNotification/addMetricsNotification` | Bearer | Add notification to cluster |
| `POST` | `/MetricsNotification/removeMetricsNotification` | Bearer | Remove notification from cluster |

### Cluster prices — `/metrics/api/cluster-prices/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/cluster-prices` | Bearer | Cluster prices (clusterName, nodeName, region, instanceType, minutes) |
| `GET` | `/cluster-prices/cluster/{clusterName}` | Bearer | Prices by cluster |
| `GET` | `/cluster-prices/cluster/{clusterName}/node/{nodeName}` | Bearer | Prices by cluster and node |

### Azure prices — `/metrics/api/azure-prices`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/azure-prices` | None | Get Azure prices (body: AzurePriceRequest; region, SKU, OS, etc.) |

### Version — `/metrics/api/Version`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/Version` | None | API version |

---

## Data flow

1. **Metrics Agent** (or other clients) sends pod metrics, node metrics, pod logs, and Kubernetes events to the Metrics API (POST endpoints; no auth).
2. Metrics API writes to **ClickHouse** (metrics, node metrics, pod logs, events tables).
3. On node metric ingest, the API may send **node status** changes to the Notification service via the message queue and may fetch **Azure prices** and store them in ClickHouse.
4. **Alerts** and **notification mappings** (cluster → notification ID) are stored in **SQL Server** and queried via MetricsAlert and MetricsNotification controllers.

---

## Helm Chart Reference

In the [Helm chart](/helm/), Metrics API is configured under the `metrics-api` section. Set `metrics-api.env` with the variables above (e.g. `CLICKHOUSE_CONNECTION_STRING`, `ConnectionStrings__SqlConnectionString`, `RabbitMq__*` or `ServiceBus__*`, `ENABLE_LOG_CLEANUP`, `LOG_CLEANUP_INTERVAL_HOURS`, Azure AD, JWT, etc.). See [Environment variables](/helm/environment-variables#metrics-api) in the Helm docs.
