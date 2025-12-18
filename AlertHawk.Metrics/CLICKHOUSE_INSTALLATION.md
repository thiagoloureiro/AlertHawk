# ClickHouse Installation Guide

This guide provides instructions for installing ClickHouse on Kubernetes using the Helm chart from Sentry.

## Prerequisites

- Kubernetes cluster (1.20+)
- Helm 3.x installed
- kubectl configured to access your cluster
- Storage class configured for persistent volumes (for data persistence)

## Installation Steps

### 1. Add Helm Repository

Add the Sentry Helm repository:

```bash
helm repo add sentry https://sentry-kubernetes.github.io/charts
helm repo update
```

### 2. Install ClickHouse

#### Option A: Install with Default Configuration

Install ClickHouse with default settings:

```bash
helm install clickhouse sentry/clickhouse --version 4.1.1
```

#### Option B: Install with Custom Configuration

Create a `clickhouse-values.yaml` file with your custom configuration:

```yaml
clickhouse:
  configmap:
    builtin_dictionaries_reload_interval: '3600'
    compression:
      cases:
        - method: zstd
          min_part_size: '10000000000'
          min_part_size_ratio: '0.01'
      enabled: false
    configOverride: ''
    default_session_timeout: '60'
    disable_internal_dns_cache: '1'
    enabled: true
    graphite:
      config:
        - asynchronous_metrics: true
          events: true
          events_cumulative: true
          interval: '60'
          metrics: true
          root_path: one_min
          timeout: '0.1'
      enabled: false
    keep_alive_timeout: '3'
    logger:
      count: '10'
      level: trace
      path: /var/log/clickhouse-server
      size: 1000M
      stdoutLogsEnabled: false
    mark_cache_size: '5368709120'
    max_concurrent_queries: '100'
    max_connections: '4096'
    max_session_timeout: '3600'
    merge_tree:
      enabled: false
      max_part_loading_threads: auto
      max_suspicious_broken_parts: 100
      parts_to_delay_insert: 150
      parts_to_throw_insert: 300
    mlock_executable: false
    profiles:
      enabled: false
      profile:
        - config:
            load_balancing: random
            max_memory_usage: '10000000000'
            use_uncompressed_cache: '0'
          name: default
    quotas:
      enabled: false
      quota:
        - config:
            - duration: '3600'
              errors: '0'
              execution_time: '0'
              queries: '0'
              read_rows: '0'
              result_rows: '0'
          name: default
    remote_servers:
      enabled: true
      internal_replication: false
      replica:
        backup:
          enabled: true
        compression: true
        user: default
    umask: '022'
    uncompressed_cache_size: '8589934592'
    users:
      enabled: true
      user:
        - config:
            networks:
              - '::/0'
            profile: default
            quota: default
          name: default
        - config:
            networks:
              - '::/0'
            password: DBPASSWORD
            profile: default
            quota: default
          name: admin
    zookeeper_servers:
      config:
        - host: ''
          index: ''
          port: ''
      enabled: false
      operation_timeout_ms: '10000'
      session_timeout_ms: '30000'
  http_port: '8123'
  image: clickhouse/clickhouse-server
  imagePullPolicy: IfNotPresent
  imageVersion: null
  ingress:
    enabled: false
  interserver_http_port: '9009'
  listen_host: 0.0.0.0
  livenessProbe:
    enabled: true
    failureThreshold: 3
    initialDelaySeconds: 0
    periodSeconds: 30
    successThreshold: 1
    timeoutSeconds: 5
  metrics:
    enabled: true
    podAnnotations:
      prometheus.io/port: '9116'
      prometheus.io/scrape: 'true'
    port: 9116
    prometheusRule:
      additionalLabels: {}
      enabled: false
      namespace: ''
      rules: []
    service:
      annotations: {}
      labels: {}
      type: ClusterIP
    serviceMonitor:
      enabled: false
      selector:
        prometheus: kube-prometheus
  path: /var/lib/clickhouse
  persistentVolumeClaim:
    dataPersistentVolume:
      accessModes:
        - ReadWriteOnce
      enabled: true
      storage: 100Gi
    enabled: true
    logsPersistentVolume:
      accessModes:
        - ReadWriteOnce
      enabled: false
      storage: 50Gi
  podManagementPolicy: Parallel
  podSecurityContext: {}
  priorityClassName: null
  readinessProbe:
    enabled: true
    failureThreshold: 3
    initialDelaySeconds: 0
    periodSeconds: 30
    successThreshold: 1
    timeoutSeconds: 5
  replicas: '1'
  resources: {}
  securityContext: {}
  startupProbe:
    enabled: true
    failureThreshold: 60
    periodSeconds: 5
    successThreshold: 1
    timeoutSeconds: 5
  tcp_port: '9000'
  updateStrategy: RollingUpdate

clusterDomain: cluster.local

serviceAccount:
  annotations: {}
  automountServiceAccountToken: true
  enabled: false
  name: clickhouse

tabix:
  enabled: false
  image: spoonest/clickhouse-tabix-web-client
  imagePullPolicy: IfNotPresent
  imageVersion: stable
  ingress:
    enabled: false
  livenessProbe:
    enabled: true
    failureThreshold: '3'
    initialDelaySeconds: '30'
    periodSeconds: '30'
    successThreshold: '1'
    timeoutSeconds: '5'
  podAnnotations: null
  podLabels: null
  readinessProbe:
    enabled: true
    failureThreshold: '3'
    initialDelaySeconds: '30'
    periodSeconds: '30'
    successThreshold: '1'
    timeoutSeconds: '5'
  replicas: '1'
  resources: {}
  security:
    password: admin
    user: admin
  updateStrategy:
    maxSurge: 3
    maxUnavailable: 1
    type: RollingUpdate

timezone: UTC

global:
  cattle:
    systemProjectId: p-255f7
```

**Important:** Before installing, update the `DBPASSWORD` value in the `clickhouse.configmap.users.user` section with a secure password.

Then install with the custom values:

```bash
helm install clickhouse sentry/clickhouse --version 4.1.1 -f clickhouse-values.yaml
```

### 3. Verify Installation

Check if ClickHouse pods are running:

```bash
kubectl get pods -l app=clickhouse
```

Expected output should show the ClickHouse pod in `Running` state:

```
NAME                        READY   STATUS    RESTARTS   AGE
clickhouse-0                1/1     Running   0          2m
```

Check the service:

```bash
kubectl get svc clickhouse
```

### 4. Access ClickHouse

#### Get Connection Details

**HTTP Port (8123):**
```bash
kubectl port-forward svc/clickhouse 8123:8123
```

Then access via:

- HTTP: `http://localhost:8123`
- ClickHouse client: `clickhouse-client --host localhost --port 8123`

**Native TCP Port (9000):**
```bash
kubectl port-forward svc/clickhouse 9000:9000
```

#### Connection String Format

For use in applications, the connection string format is:

```
Host=<service-name>.<namespace>.svc.cluster.local;Port=8123;Database=default;Username=default;Password=
```

Or for the admin user:

```
Host=<service-name>.<namespace>.svc.cluster.local;Port=8123;Database=default;Username=admin;Password=DBPASSWORD
```

**Example (default namespace):**
```
Host=clickhouse.default.svc.cluster.local;Port=8123;Database=default;Username=default;Password=
```

### 5. Test Connection

Test the connection using the ClickHouse HTTP interface:

```bash
kubectl run -it --rm clickhouse-client --image=clickhouse/clickhouse-client --restart=Never -- clickhouse-client --host clickhouse --port 9000
```

Or test via HTTP:

```bash
kubectl exec -it clickhouse-0 -- clickhouse-client --query "SELECT version()"
```

## Configuration Details

### Key Configuration Points

1. **Storage**: 100Gi persistent volume for data (configurable)
2. **Ports**:
   - HTTP: 8123 (for HTTP interface)
   - Native TCP: 9000 (for native protocol)
   - Interserver: 9009 (for cluster communication)
3. **Users**:
   - `default`: No password (for internal use)
   - `admin`: Password-protected (set `DBPASSWORD` in values)
4. **Metrics**: Prometheus metrics enabled on port 9116

### Customizing Configuration

To modify the configuration after installation:

1. Update your `clickhouse-values.yaml` file
2. Upgrade the Helm release:

```bash
helm upgrade clickhouse sentry/clickhouse --version 4.1.1 -f clickhouse-values.yaml
```

## Uninstallation

To remove ClickHouse:

```bash
helm uninstall clickhouse
```

**Warning:** This will delete the ClickHouse deployment. Persistent volumes will remain unless manually deleted.

## Troubleshooting

### Pod Not Starting

Check pod logs:
```bash
kubectl logs clickhouse-0
```

### Connection Issues

Verify the service is running:
```bash
kubectl get svc clickhouse
kubectl describe svc clickhouse
```

### Storage Issues

Check persistent volume claims:
```bash
kubectl get pvc
kubectl describe pvc <pvc-name>
```

## Additional Resources

- [ClickHouse Helm Chart Documentation](https://artifacthub.io/packages/helm/sentry/clickhouse)
- [ClickHouse Official Documentation](https://clickhouse.com/docs)
- [Sentry Kubernetes Charts](https://github.com/sentry-kubernetes/charts)

## Notes

- The default user has no password. For production, always set a secure password for the admin user.
- Storage size (100Gi) can be adjusted based on your needs.
- For production environments, consider enabling replication and using multiple replicas.
- Monitor disk usage as ClickHouse can grow quickly with time-series data.

