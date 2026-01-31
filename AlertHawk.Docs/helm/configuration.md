# Configuration

The chart is configured via `values.yaml`. Top-level keys map to global settings and per-component settings.

## Top-level structure

| Key | Description |
|----|-------------|
| `defaultReplicas` | Default replica count when a component doesn’t set `replicas` |
| `image` | Container images per component (`monitoring`, `auth`, `notification`, `metrics-api`, `ui`) |
| `service` | Shared service config (e.g. `port`) |
| `clickhouse` | ClickHouse subchart (enable/disable and subchart values) |
| `auth` | Auth component: `replicas`, `env` |
| `monitoring` | Monitoring component: `replicas`, `env` |
| `notification` | Notification component: `replicas`, `env` |
| `metrics-api` | Metrics API component: `replicas`, `env` |
| `ui` | UI component: `replicas` (no env in default chart) |

## Global values

### defaultReplicas

```yaml
defaultReplicas: 1
```

Used when a component does not specify `replicas`.

### image

Override container images and tags:

```yaml
image:
  monitoring: thiagoguaru/alerthawk.monitoring:3.1.6
  auth: thiagoguaru/alerthawk.authentication:3.1.2
  notification: thiagoguaru/alerthawk.notification:3.1.2
  metrics-api: thiagoguaru/alerthawk.metrics.api:3.1.13
  ui: thiagoguaru/alerthawk.ui-demo-v2:3.1.16
```

### service

```yaml
service:
  port: 8080
```

Container port used by all deployments.

## ClickHouse

```yaml
clickhouse:
  enabled: false   # set true to install ClickHouse as subchart
  # Subchart options: https://artifacthub.io/packages/helm/clickhouse-alerthawk/clickhouse
```

- `enabled: true` — installs ClickHouse from the chart dependency; use `CLICKHOUSE_CONNECTION_STRING: http://clickhouse:8123/default` in `metrics-api.env`.
- `enabled: false` — use your own ClickHouse; set `CLICKHOUSE_CONNECTION_STRING` in `metrics-api.env` accordingly.

## Per-component: replicas and env

Each component (auth, monitoring, notification, metrics-api) can set:

```yaml
<component>:
  replicas: 1
  env:
    VAR_NAME: value
    Nested__Key: value   # ASP.NET Core uses __ for nested config
```

- **replicas** — number of pod replicas (defaults to `defaultReplicas` if omitted).
- **env** — environment variables for the container. Keys are passed as-is; use `__` for nested configuration (e.g. `ConnectionStrings__SqlConnectionString`, `AzureAd__ClientId`).

The **ui** component in the default chart only has `replicas` (no `env` block).

## Example minimal values

```yaml
defaultReplicas: 1

clickhouse:
  enabled: true

image:
  monitoring: thiagoguaru/alerthawk.monitoring:3.1.6
  auth: thiagoguaru/alerthawk.authentication:3.1.2
  notification: thiagoguaru/alerthawk.notification:3.1.2
  metrics-api: thiagoguaru/alerthawk.metrics.api:3.1.13
  ui: thiagoguaru/alerthawk.ui-demo-v2:3.1.16

service:
  port: 8080

auth:
  replicas: 1
  env:
    ASPNETCORE_ENVIRONMENT: Production
    ConnectionStrings__SqlConnectionString: "Server=..."
    Jwt__Key: "your-secret"
    Jwt__Issuers: "https://your-issuer"
    Jwt__Audiences: "your-audience"
    # ... see Environment variables page
```

For the full list of supported environment variables per service, see [Environment variables](/helm/environment-variables).
