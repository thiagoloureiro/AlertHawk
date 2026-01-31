# Metrics Agent Chart

::: tip Documentation moved
The full documentation for the Metrics Agent Helm chart is now in the **[Agents Chart](/agents-chart/)** section: [Overview](/agents-chart/), [Installation](/agents-chart/installation), [Configuration](/agents-chart/configuration), [Environment Variables](/agents-chart/environment-variables).
:::

The **AlertHawk Metrics Agent** is a **separate Helm chart** that deploys the metrics collection agent. It runs in your Kubernetes cluster, collects metrics from the cluster, and sends them to the AlertHawk Metrics API.

- **Chart name:** `alerthawk-metrics-agent`
- **Same Helm repo as main chart:** `https://thiagoloureiro.github.io/AlertHawk.Chart/`
- **Artifact Hub:** [alerthawk](https://artifacthub.io/packages/search?repo=alerthawk) (search for metrics-agent)

---

## What it does

1. **Collects Kubernetes metrics** — Pods, deployments, services, and other resources in the cluster (optionally scoped by namespace).
2. **Sends to Metrics API** — Forwards metrics to the AlertHawk Metrics API service (must be running and reachable).
3. **Cluster identification** — Tags metrics with a cluster name for multi-cluster setups.
4. **Configurable** — Collection interval, namespaces to watch, optional log collection.

---

## Prerequisites

- Kubernetes cluster (1.19+)
- Helm 3.x
- **AlertHawk Metrics API** running (to receive metrics)
- **ClickHouse** (used by the Metrics API to store metrics — not installed by this chart)

---

## Installation

### 1. Add the Helm repository

```bash
helm repo add alerthawk https://thiagoloureiro.github.io/AlertHawk.Chart/
helm repo update
```

### 2. Create a values file

Create `metrics-agent-values.yaml` and set the **required** env vars:

```yaml
env:
  CLUSTER_NAME: "YOUR-CLUSTER-NAME"   # e.g. aks-tools-01
  METRICS_API_URL: "http://alerthawk-metrics-api.alerthawk.svc.cluster.local:8080"
  NAMESPACES_TO_WATCH: "alerthawk,clickhouse"   # comma-separated namespaces to monitor
```

See [Configuration](#configuration) and [Environment variables](#environment-variables) below for full options.

### 3. Install the chart

```bash
helm install alerthawk-metrics-agent alerthawk/alerthawk-metrics-agent -f metrics-agent-values.yaml
```

With a specific namespace:

```bash
helm install alerthawk-metrics-agent alerthawk/alerthawk-metrics-agent -f metrics-agent-values.yaml -n alerthawk --create-namespace
```

### 4. Upgrade

```bash
helm upgrade alerthawk-metrics-agent alerthawk/alerthawk-metrics-agent -f metrics-agent-values.yaml
```

### 5. Uninstall

```bash
helm uninstall alerthawk-metrics-agent
```

---

## Configuration

### Top-level values

| Key | Description | Default |
|-----|-------------|---------|
| `nameOverride` | Override deployment name prefix | `alerthawk` |
| `replicas` | Number of pod replicas | `1` |

### Image

| Key | Description | Default |
|-----|-------------|---------|
| `image.repository` | Container image repository | `thiagoguaru/alerthawk.metrics` |
| `image.tag` | Image tag | `3.1.12` (or chart appVersion) |
| `image.pullPolicy` | Pull policy | `Always` |

### Deployment strategy

| Key | Description | Default |
|-----|-------------|---------|
| `strategy.type` | `RollingUpdate` or `Recreate` | `RollingUpdate` |
| `strategy.rollingUpdate.maxSurge` | Max surge | `25%` |
| `strategy.rollingUpdate.maxUnavailable` | Max unavailable | `25%` |

### Service account and RBAC

| Key | Description | Default |
|-----|-------------|---------|
| `serviceAccount.create` | Create a ServiceAccount | `true` |
| `serviceAccount.name` | ServiceAccount name | `alerthawk-sa` |
| `serviceAccount.annotations` | Annotations | — |
| `serviceAccount.clusterRoleBinding.create` | Create ClusterRoleBinding | `true` |
| `serviceAccount.clusterRoleBinding.clusterRole` | ClusterRole to bind | `cluster-admin` |

The agent needs cluster (or namespace) read access to collect metrics. By default the chart creates a ServiceAccount and binds it to `cluster-admin`. If you set `serviceAccount.create: false`, create the ServiceAccount and ClusterRoleBinding yourself.

### Security context

| Key | Description | Default |
|-----|-------------|---------|
| `securityContext.allowPrivilegeEscalation` | Allow privilege escalation | `false` |
| `securityContext.privileged` | Privileged container | `false` |
| `securityContext.readOnlyRootFilesystem` | Read-only root filesystem | `false` |
| `securityContext.runAsNonRoot` | Run as non-root | `false` |

### Resources (optional)

```yaml
resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 100m
    memory: 128Mi
```

### Other

| Key | Description | Default |
|-----|-------------|---------|
| `progressDeadlineSeconds` | Deployment progress deadline | `600` |
| `revisionHistoryLimit` | ReplicaSet history limit | `10` |
| `terminationGracePeriodSeconds` | Pod termination grace period | `30` |
| `podAnnotations` | Pod annotations | — |
| `imagePullSecrets` | Image pull secret names | — |

---

## Environment variables

All agent configuration is passed via the `env` map in `values.yaml`. Keys are environment variable names; values are strings.

### Required

| Variable | Description | Example |
|----------|-------------|---------|
| `CLUSTER_NAME` | Name of the cluster (used to tag metrics) | `aks-tools-01` |
| `METRICS_API_URL` | Metrics API base URL (must be reachable from the pod) | `http://alerthawk-metrics-api.alerthawk.svc.cluster.local:8080` |
| `NAMESPACES_TO_WATCH` | Comma-separated list of namespaces to monitor | `alerthawk,clickhouse,production` |

### Optional — Collection

| Variable | Description | Default |
|----------|-------------|---------|
| `METRICS_COLLECTION_INTERVAL_SECONDS` | Interval in seconds between metric collections | `40` |
| `COLLECT_LOGS` | Enable log collection | `false` |

### Optional — Cluster / logging

| Variable | Description | Default |
|----------|-------------|---------|
| `CLUSTER_ENVIRONMENT` | Environment label (e.g. PROD, DEV) | `PROD` |
| `LOG_LEVEL` | Log level (Verbose, Debug, Information, Warning, Error, Fatal) | `Information` |

### Optional — Sentry

| Variable | Description | Default |
|----------|-------------|---------|
| `SENTRY_DSN` | Sentry DSN for error tracking | — |
| `ENVIRONMENT` | Environment name sent to Sentry | `Production` |

---

## Example values.yaml

```yaml
nameOverride: "alerthawk"
replicas: 1

image:
  repository: thiagoguaru/alerthawk.metrics
  tag: "3.1.12"
  pullPolicy: Always

strategy:
  type: RollingUpdate
  rollingUpdate:
    maxSurge: 25%
    maxUnavailable: 25%

serviceAccount:
  create: true
  name: alerthawk-sa
  clusterRoleBinding:
    create: true
    clusterRole: cluster-admin

securityContext:
  allowPrivilegeEscalation: false
  privileged: false
  readOnlyRootFilesystem: false
  runAsNonRoot: false

env:
  CLUSTER_NAME: "aks-tools-01"
  METRICS_API_URL: "http://alerthawk-metrics-api.alerthawk.svc.cluster.local:8080"
  METRICS_COLLECTION_INTERVAL_SECONDS: "40"
  NAMESPACES_TO_WATCH: "alerthawk,clickhouse,production"
  COLLECT_LOGS: "false"
  CLUSTER_ENVIRONMENT: "PROD"
  LOG_LEVEL: "Information"
  SENTRY_DSN: ""
  ENVIRONMENT: "Production"
```

---

## Rancher

The chart includes `questions.yml` and `values.schema.json` for a form-based UI in Rancher. You can configure replicas, image, strategy, env vars, security context, and resources from the Rancher UI.

---

## Troubleshooting

- **Metrics not collected** — Check `METRICS_API_URL` is correct and reachable; ensure the ServiceAccount has permission to read cluster/namespace resources; confirm namespaces exist.
- **Connection errors** — Verify network from the agent pod to the Metrics API (e.g. `kubectl run -it --rm debug --image=curlimages/curl -- curl -v $METRICS_API_URL`).
- **Pod not starting** — Check logs: `kubectl logs -n <namespace> <pod-name>`; confirm required env vars are set; verify ServiceAccount exists.
