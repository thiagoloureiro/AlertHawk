# Metrics Agent

The **Metrics Agent** is a lightweight application that runs inside your Kubernetes cluster, collects pod metrics, node metrics, pod logs, and Kubernetes events, and sends them to the **AlertHawk Metrics API**. It is typically deployed via the [Agents Chart](/agents-chart/) (Helm).

---

## Overview

- **Runs in-cluster**: Uses in-cluster Kubernetes config and a ServiceAccount with read access to the cluster (or specified namespaces).
- **Collection loop**: Periodically runs three collectors (pod metrics, node metrics, events) and optionally pod logs; then sends data to the Metrics API over HTTP.
- **No database**: The agent does not store data; it only reads from the Kubernetes API and POSTs to the Metrics API.
- **Cluster identification**: Every payload is tagged with `CLUSTER_NAME` (and optionally `CLUSTER_ENVIRONMENT`) so the Metrics API can distinguish multiple clusters.

---

## Environment Variables

The agent reads all configuration from environment variables. When deployed with the [Agents Chart](/agents-chart/), these are set in the chart’s `env` block.

### Required

| Variable | Description | Example |
|----------|-------------|---------|
| `CLUSTER_NAME` | Name of the cluster (used in all payloads sent to the Metrics API) | `aks-tools-01` |
| `METRICS_API_URL` | Base URL of the Metrics API (must be reachable from the cluster) | `http://alerthawk-metrics-api.alerthawk.svc.cluster.local:8080` |
| `NAMESPACES_TO_WATCH` | Comma-separated list of namespaces to collect from (pod metrics, events, pod logs) | `alerthawk,clickhouse,production` |

### Optional — Collection

| Variable | Description | Default |
|----------|-------------|---------|
| `METRICS_COLLECTION_INTERVAL_SECONDS` | Seconds between collection cycles | `30` |
| `COLLECT_LOGS` | Enable pod log collection and send to Metrics API | `false` |
| `LOG_TAIL_LINES` | Number of tail lines to collect per container when `COLLECT_LOGS=true` | `100` |

### Optional — Cluster / logging

| Variable | Description | Default |
|----------|-------------|---------|
| `CLUSTER_ENVIRONMENT` | Environment label sent with node metrics (e.g. PROD, DEV) | `PROD` |
| `LOG_LEVEL` | Serilog log level (Verbose, Debug, Information, Warning, Error, Fatal) | `Information` |

### Optional — Sentry

| Variable | Description | Default |
|----------|-------------|---------|
| `SENTRY_DSN` | Sentry DSN for error tracking | (chart may leave empty) |
| `ENVIRONMENT` | Environment name sent to Sentry | `Production` |

---

## What It Collects

The agent runs a loop every **METRICS_COLLECTION_INTERVAL_SECONDS** and, in order:

1. **Pod metrics** (PodMetricsCollector)  
   For each pod in **NAMESPACES_TO_WATCH**:
   - Reads container CPU/memory from the metrics.k8s.io API (if available) or estimates from node capacity.
   - Sends one payload per container to the Metrics API: `POST /api/metrics/pod` (namespace, pod, container, CPU/memory, node name, pod state, restart count, age).
   - If **COLLECT_LOGS** is `true`, collects **LOG_TAIL_LINES** lines per container and sends `POST /api/metrics/pod/log`.

2. **Node metrics** (NodeMetricsCollector)  
   For each node in the cluster:
   - Reads node capacity and conditions (Ready, MemoryPressure, DiskPressure, PIDPressure), Kubernetes version, and (when possible) cloud provider, region, instance type, OS, architecture.
   - Sends one payload per node: `POST /api/metrics/node`.  
   The Metrics API may use this to send node-status notifications and to fetch/store Azure prices.

3. **Kubernetes events** (EventsCollector)  
   For each namespace in **NAMESPACES_TO_WATCH**:
   - Lists events and sends new/updated events to the Metrics API: `POST /api/events`.

All requests include **CLUSTER_NAME** (and, for node metrics, **CLUSTER_ENVIRONMENT**) so the Metrics API can store and query by cluster.

---

## Data Sent to the Metrics API

| Data | Metrics API endpoint | Auth |
|------|----------------------|------|
| Pod/container metrics | `POST /api/metrics/pod` | None (ingest) |
| Node metrics | `POST /api/metrics/node` | None (ingest) |
| Pod logs | `POST /api/metrics/pod/log` | None (ingest) |
| Kubernetes events | `POST /api/events` | None (ingest) |

The agent does not use JWT; ingest endpoints on the Metrics API are AllowAnonymous.

---

## Deployment

- **Helm (recommended)**: Use the [Agents Chart](/agents-chart/) to deploy the Metrics Agent in each cluster you want to monitor. See [Agents Chart — Installation](/agents-chart/installation) and [Environment variables](/agents-chart/environment-variables).
- **Prerequisites**: Kubernetes cluster (1.19+), ServiceAccount with read access to the cluster or to **NAMESPACES_TO_WATCH**, and the **Metrics API** running and reachable (e.g. `METRICS_API_URL`).

---

## Troubleshooting

- **No data in Metrics API**: Ensure `METRICS_API_URL` is correct and reachable from the pod (e.g. `kubectl run -it --rm debug --image=curlimages/curl -- curl -v $METRICS_API_URL/api/Version`). Check that **NAMESPACES_TO_WATCH** contains existing namespaces and that the ServiceAccount has permission to list pods/nodes/events (and logs if **COLLECT_LOGS** is true).
- **Agent exits at startup**: If the agent logs "CLUSTER_NAME environment variable is required but not set!", set **CLUSTER_NAME** (and **NAMESPACES_TO_WATCH**) in the deployment.
- **Logs**: Check agent logs with `kubectl logs -n <namespace> <metrics-agent-pod>`.
