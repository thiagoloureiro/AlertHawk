# Configuration

The Metrics Agent chart is configured via `values.yaml`. This page describes the main options.

## Top-level values

| Key | Description | Default |
|-----|-------------|---------|
| `nameOverride` | Override deployment name prefix | `alerthawk` |
| `replicas` | Number of pod replicas | `1` |

## Image

| Key | Description | Default |
|-----|-------------|---------|
| `image.repository` | Container image repository | `thiagoguaru/alerthawk.metrics` |
| `image.tag` | Image tag | `3.1.12` (or chart appVersion) |
| `image.pullPolicy` | Pull policy | `Always` |

## Deployment strategy

| Key | Description | Default |
|-----|-------------|---------|
| `strategy.type` | `RollingUpdate` or `Recreate` | `RollingUpdate` |
| `strategy.rollingUpdate.maxSurge` | Max surge | `25%` |
| `strategy.rollingUpdate.maxUnavailable` | Max unavailable | `25%` |

## Service account and RBAC

| Key | Description | Default |
|-----|-------------|---------|
| `serviceAccount.create` | Create a ServiceAccount | `true` |
| `serviceAccount.name` | ServiceAccount name | `alerthawk-sa` |
| `serviceAccount.annotations` | Annotations | — |
| `serviceAccount.clusterRoleBinding.create` | Create ClusterRoleBinding | `true` |
| `serviceAccount.clusterRoleBinding.clusterRole` | ClusterRole to bind | `cluster-admin` |

The agent needs cluster (or namespace) read access to collect metrics. By default the chart creates a ServiceAccount and binds it to `cluster-admin`. If you set `serviceAccount.create: false`, create the ServiceAccount and ClusterRoleBinding yourself.

## Security context

| Key | Description | Default |
|-----|-------------|---------|
| `securityContext.allowPrivilegeEscalation` | Allow privilege escalation | `false` |
| `securityContext.privileged` | Privileged container | `false` |
| `securityContext.readOnlyRootFilesystem` | Read-only root filesystem | `false` |
| `securityContext.runAsNonRoot` | Run as non-root user | `false` |

## Resources (optional)

```yaml
resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 100m
    memory: 128Mi
```

## Other

| Key | Description | Default |
|-----|-------------|---------|
| `progressDeadlineSeconds` | Deployment progress deadline | `600` |
| `revisionHistoryLimit` | ReplicaSet history limit | `10` |
| `terminationGracePeriodSeconds` | Pod termination grace period | `30` |
| `podAnnotations` | Pod annotations | — |
| `imagePullSecrets` | Image pull secret names | — |

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

See [Environment variables](/agents-chart/environment-variables) for all `env` options.
