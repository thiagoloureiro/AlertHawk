# Environment Variables

All Metrics Agent configuration is passed via the `env` map in `values.yaml`. Keys are environment variable names; values are strings.

## Required

| Variable | Description | Example |
|----------|-------------|---------|
| `CLUSTER_NAME` | Name of the cluster (used to tag metrics) | `aks-tools-01` |
| `METRICS_API_URL` | Metrics API base URL (must be reachable from the pod) | `http://alerthawk-metrics-api.alerthawk.svc.cluster.local:8080` |
| `NAMESPACES_TO_WATCH` | Comma-separated list of namespaces to monitor | `alerthawk,clickhouse,production` |

## Optional — Collection

| Variable | Description | Default |
|----------|-------------|---------|
| `METRICS_COLLECTION_INTERVAL_SECONDS` | Interval in seconds between metric collections | `40` |
| `COLLECT_LOGS` | Enable log collection | `false` |

## Optional — Cluster / logging

| Variable | Description | Default |
|----------|-------------|---------|
| `CLUSTER_ENVIRONMENT` | Environment label (e.g. PROD, DEV) | `PROD` |
| `LOG_LEVEL` | Log level (Verbose, Debug, Information, Warning, Error, Fatal) | `Information` |

## Optional — Sentry

| Variable | Description | Default |
|----------|-------------|---------|
| `SENTRY_DSN` | Sentry DSN for error tracking | — |
| `ENVIRONMENT` | Environment name sent to Sentry | `Production` |

## Example env block

```yaml
env:
  # Required
  CLUSTER_NAME: "aks-tools-01"
  METRICS_API_URL: "http://alerthawk-metrics-api.alerthawk.svc.cluster.local:8080"
  NAMESPACES_TO_WATCH: "alerthawk,clickhouse,production"
  # Optional
  METRICS_COLLECTION_INTERVAL_SECONDS: "40"
  COLLECT_LOGS: "false"
  CLUSTER_ENVIRONMENT: "PROD"
  LOG_LEVEL: "Information"
  SENTRY_DSN: ""
  ENVIRONMENT: "Production"
```
