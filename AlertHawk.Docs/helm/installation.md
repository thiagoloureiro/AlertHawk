# Installation

Step-by-step guide to installing AlertHawk with the Helm chart.

## Prerequisites

- **Kubernetes cluster** (1.x or later)
- **Helm 3.x** installed (`helm version`)
- **kubectl** configured for your cluster

## External dependencies

You must have (or install) these before or with the chart:

### SQL Server

Required for **auth**, **notification**, **monitoring**, and **metrics-api**. Provide a SQL Server instance and set `ConnectionStrings__SqlConnectionString` in each component’s `env` section (see [Environment variables](/helm/environment-variables)).

### ClickHouse (for metrics-api)

The **metrics-api** component needs ClickHouse for metrics storage.

**Option 1: Install ClickHouse with the chart (recommended)**

In your `values.yaml`:

```yaml
clickhouse:
  enabled: true
```

Then set in `metrics-api.env`:

```yaml
CLICKHOUSE_CONNECTION_STRING: http://clickhouse:8123/default
```

**Option 2: Use an existing ClickHouse**

Set `clickhouse.enabled: false` and configure `CLICKHOUSE_CONNECTION_STRING` in `metrics-api.env` to point to your instance (e.g. `http://my-clickhouse:8123/default`).

ClickHouse chart reference: [clickhouse-alerthawk](https://artifacthub.io/packages/helm/clickhouse-alerthawk/clickhouse).

### RabbitMQ or Azure Service Bus (for messaging)

**monitoring**, **notification**, and **metrics-api** use a message queue. Configure either:

- **RabbitMQ:** `RabbitMq__Host`, `RabbitMq__User`, `RabbitMq__Pass`, and `QueueType: RABBITMQ`
- **Azure Service Bus:** `ServiceBus__ConnectionString`, `ServiceBus__QueueName`, and `QueueType: SERVICEBUS` (or equivalent)

See [Environment variables](/helm/environment-variables) per service.

### Azure AD (optional)

For SSO and JWT validation, configure Azure AD in the services that need it (auth, monitoring, notification, metrics-api):

- `AzureAd__ClientId`
- `AzureAd__TenantId`
- `AzureAd__ClientSecret`
- `AzureAd__Instance` (e.g. `https://login.microsoftonline.com/`)

---

## Install steps

### 1. Add the Helm repository

```bash
helm repo add alerthawk https://thiagoloureiro.github.io/AlertHawk.Chart/
helm repo update
```

### 2. Prepare values

Download default values and edit them:

```bash
helm show values alerthawk/alerthawk > my-values.yaml
```

Edit `my-values.yaml`:

- Set **SQL Server** connection strings for auth, notification, monitoring, metrics-api.
- Set **ClickHouse** connection string in `metrics-api.env` (and optionally enable `clickhouse.enabled: true`).
- Set **RabbitMQ** or **Service Bus** in monitoring, notification, and metrics-api.
- Set **JWT** (`Jwt__Key`, `Jwt__Issuers`, `Jwt__Audiences`) consistently across services.
- Set **Azure AD** and other secrets as needed.

See [Configuration](/helm/configuration) and [Environment variables](/helm/environment-variables).

### 3. Install the chart

```bash
helm install alerthawk alerthawk/alerthawk -f my-values.yaml
```

Or use a custom release name and namespace:

```bash
helm install my-alerthawk alerthawk/alerthawk -f my-values.yaml -n alerthawk --create-namespace
```

### 4. Verify

```bash
kubectl get pods
kubectl get svc
```

Check that deployments for `alerthawk-monitoring`, `alerthawk-auth`, `alerthawk-notification`, `alerthawk-metrics-api`, and `alerthawk-ui` are running.

---

## Upgrade

After changing `values.yaml` or chart version:

```bash
helm repo update
helm upgrade alerthawk alerthawk/alerthawk -f my-values.yaml
```

---

## Uninstall

```bash
helm uninstall alerthawk
```

If you used a different release or namespace:

```bash
helm uninstall my-alerthawk -n alerthawk
```

Note: If ClickHouse was installed as a subchart (`clickhouse.enabled: true`), it is removed with the release. PVCs and external resources (SQL Server, RabbitMQ, etc.) are not removed by Helm.
