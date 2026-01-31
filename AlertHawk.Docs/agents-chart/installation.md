# Installation

Step-by-step guide to installing the AlertHawk Metrics Agent with the Helm chart.

## Prerequisites

- Kubernetes cluster (1.19+)
- Helm 3.x
- **AlertHawk Metrics API** running (to receive metrics)
- **ClickHouse** (used by the Metrics API to store metrics — not installed by this chart)

## 1. Add the Helm repository

```bash
helm repo add alerthawk https://thiagoloureiro.github.io/AlertHawk.Chart/
helm repo update
```

## 2. Create a values file

Create `metrics-agent-values.yaml` and set the **required** env vars:

```yaml
env:
  CLUSTER_NAME: "YOUR-CLUSTER-NAME"   # e.g. aks-tools-01
  METRICS_API_URL: "http://alerthawk-metrics-api.alerthawk.svc.cluster.local:8080"
  NAMESPACES_TO_WATCH: "alerthawk,clickhouse"   # comma-separated namespaces to monitor
```

See [Configuration](/agents-chart/configuration) and [Environment variables](/agents-chart/environment-variables) for full options.

## 3. Install the chart

```bash
helm install alerthawk-metrics-agent alerthawk/alerthawk-metrics-agent -f metrics-agent-values.yaml
```

With a specific namespace:

```bash
helm install alerthawk-metrics-agent alerthawk/alerthawk-metrics-agent -f metrics-agent-values.yaml -n alerthawk --create-namespace
```

## 4. Upgrade

```bash
helm upgrade alerthawk-metrics-agent alerthawk/alerthawk-metrics-agent -f metrics-agent-values.yaml
```

## 5. Uninstall

```bash
helm uninstall alerthawk-metrics-agent
```

If you used a different release name or namespace:

```bash
helm uninstall alerthawk-metrics-agent -n alerthawk
```

## Troubleshooting

- **Metrics not collected** — Check `METRICS_API_URL` is correct and reachable; ensure the ServiceAccount has permission to read cluster/namespace resources; confirm namespaces exist.
- **Connection errors** — Verify network from the agent pod to the Metrics API (e.g. `kubectl run -it --rm debug --image=curlimages/curl -- curl -v $METRICS_API_URL`).
- **Pod not starting** — Check logs: `kubectl logs -n <namespace> <pod-name>`; confirm required env vars are set; verify ServiceAccount exists.
