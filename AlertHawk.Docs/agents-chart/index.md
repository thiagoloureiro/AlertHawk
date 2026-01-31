# Agents Chart

The **Agents Chart** deploys the **AlertHawk Metrics Agent** — a separate Helm chart that runs in your Kubernetes cluster, collects metrics from the cluster, and sends them to the AlertHawk Metrics API.

- **Chart name:** `alerthawk-metrics-agent`
- **Helm repo:** `https://thiagoloureiro.github.io/AlertHawk.Chart/` (same as main AlertHawk chart)
- **Artifact Hub:** [alerthawk](https://artifacthub.io/packages/search?repo=alerthawk) (search for metrics-agent)

## What it does

1. **Collects Kubernetes metrics** — Pods, deployments, services, and other resources in the cluster (optionally scoped by namespace).
2. **Sends to Metrics API** — Forwards metrics to the AlertHawk Metrics API service (must be running and reachable).
3. **Cluster identification** — Tags metrics with a cluster name for multi-cluster setups.
4. **Configurable** — Collection interval, namespaces to watch, optional log collection.

## Prerequisites

- Kubernetes cluster (1.19+)
- Helm 3.x
- **AlertHawk Metrics API** running (to receive metrics)
- **ClickHouse** (used by the Metrics API to store metrics — not installed by this chart)

## Quick start

```bash
helm repo add alerthawk https://thiagoloureiro.github.io/AlertHawk.Chart/
helm repo update

# Create values with required env: CLUSTER_NAME, METRICS_API_URL, NAMESPACES_TO_WATCH
helm install alerthawk-metrics-agent alerthawk/alerthawk-metrics-agent -f your-values.yaml
```

## Documentation

- [Installation](/agents-chart/installation) — Step-by-step install, upgrade, uninstall
- [Configuration](/agents-chart/configuration) — values.yaml structure (image, strategy, service account, etc.)
- [Environment Variables](/agents-chart/environment-variables) — Required and optional env vars

## Rancher

The chart includes `questions.yml` and `values.schema.json` for a form-based UI in Rancher.
