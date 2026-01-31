# Helm Chart

The AlertHawk Helm chart deploys all AlertHawk components on Kubernetes as deployments and services.

## Components

| Component | Deployment name | Description |
|-----------|-----------------|-------------|
| **monitoring** | `alerthawk-monitoring` | Runs monitors (HTTP, TCP, K8s, etc.) and manages alerts |
| **auth** | `alerthawk-auth` | Authentication (Azure AD, JWT, user management) |
| **notification** | `alerthawk-notification` | Sends alerts (Slack, Teams, email, webhooks, etc.) |
| **metrics-api** | `alerthawk-metrics-api` | Metrics API (ClickHouse + SQL Server) |
| **ui** | `alerthawk-ui` | Web UI |

## Charts in the repo

- **Helm repo URL:** `https://thiagoloureiro.github.io/AlertHawk.Chart/`
- **Chart repository:** [AlertHawk.Chart](https://github.com/thiagoloureiro/AlertHawk.Chart)
- **Artifact Hub:** [alerthawk](https://artifacthub.io/packages/search?repo=alerthawk)

| Chart | Description |
|-------|-------------|
| **alerthawk** | Main chart: monitoring, auth, notification, metrics-api, UI (see [Installation](/helm/installation), [Configuration](/helm/configuration), [Environment variables](/helm/environment-variables)). |
| **alerthawk-metrics-agent** | Metrics Agent only: collects Kubernetes metrics and sends them to the Metrics API. See [Agents Chart](/agents-chart/). |

## Quick start

```bash
# Add the Helm repository
helm repo add alerthawk https://thiagoloureiro.github.io/AlertHawk.Chart/
helm repo update

# Create a values file (copy and edit from chart)
helm show values alerthawk/alerthawk > my-values.yaml
# Edit my-values.yaml with your connection strings and secrets

# Install
helm install alerthawk alerthawk/alerthawk -f my-values.yaml
```

See [Installation](/helm/installation) for prerequisites and detailed steps, and [Configuration](/helm/configuration) for `values.yaml` structure. For all per-service environment variables, see [Environment variables](/helm/environment-variables).

To deploy only the **Metrics Agent** (e.g. in each cluster you want to monitor), use the separate chart: [Metrics Agent chart](/helm/metrics-agent).
