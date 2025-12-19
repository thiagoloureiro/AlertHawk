# ClickHouse Setup for AlertHawk Metrics

This document describes the ClickHouse integration for storing Kubernetes metrics.

## Tables

The application automatically creates two tables:

1. **k8s_metrics** - For pod/container metrics
2. **k8s_node_metrics** - For node-level metrics

## Pod/Container Metrics Table Schema

The application automatically creates a table with the following schema:

```sql
CREATE TABLE IF NOT EXISTS k8s_metrics
(
    timestamp DateTime64(3) DEFAULT now64(),
    namespace String,
    pod String,
    container String,
    cpu_usage_cores Float64,
    cpu_limit_cores Nullable(Float64),
    memory_usage_bytes Float64
)
ENGINE = MergeTree()
ORDER BY (timestamp, namespace, pod, container)
TTL toDateTime(timestamp) + INTERVAL 90 DAY
```

### Table Details

- **timestamp**: DateTime64(3) - Timestamp when the metric was collected (millisecond precision)
- **namespace**: String - Kubernetes namespace
- **pod**: String - Pod name
- **container**: String - Container name within the pod
- **cpu_usage_cores**: Float64 - CPU usage in cores
- **cpu_limit_cores**: Nullable(Float64) - CPU limit in cores (nullable if not set)
- **memory_usage_bytes**: Float64 - Memory usage in bytes

### Table Engine

- **Engine**: MergeTree - Optimized for time-series data
- **Order By**: (timestamp, namespace, pod, container) - Optimized for queries filtering by these fields
- **TTL**: 90 days - Data is automatically deleted after 90 days

## Node Metrics Table Schema

The application also creates a separate table for node-level metrics:

```sql
CREATE TABLE IF NOT EXISTS k8s_node_metrics
(
    timestamp DateTime64(3) DEFAULT now64(),
    node_name String,
    cpu_usage_cores Float64,
    cpu_capacity_cores Float64,
    memory_usage_bytes Float64,
    memory_capacity_bytes Float64
)
ENGINE = MergeTree()
ORDER BY (timestamp, node_name)
TTL toDateTime(timestamp) + INTERVAL 90 DAY
```

### Node Metrics Table Details

- **timestamp**: DateTime64(3) - Timestamp when the metric was collected (millisecond precision)
- **node_name**: String - Kubernetes node name
- **cpu_usage_cores**: Float64 - CPU usage in cores
- **cpu_capacity_cores**: Float64 - Total CPU capacity in cores
- **memory_usage_bytes**: Float64 - Memory usage in bytes
- **memory_capacity_bytes**: Float64 - Total memory capacity in bytes

### Node Metrics Table Engine

- **Engine**: MergeTree - Optimized for time-series data
- **Order By**: (timestamp, node_name) - Optimized for queries filtering by these fields
- **TTL**: 90 days - Data is automatically deleted after 90 days

## Connection String

The application uses the `CLICKHOUSE_CONNECTION_STRING` environment variable to connect to ClickHouse.

### Connection String Format

```
Host=<hostname>;Port=<port>;Database=<database>;Username=<username>;Password=<password>
```

### Default Values

If not specified, the application uses:

- Host: localhost
- Port: 8123
- Database: default
- Username: default
- Password: (empty)

### Example Connection Strings

**Local ClickHouse:**
```
Host=localhost;Port=8123;Database=default;Username=default;Password=
```

**Remote ClickHouse with authentication:**
```
Host=clickhouse.example.com;Port=8123;Database=metrics;Username=alerthawk;Password=your_password
```

**ClickHouse Cloud:**
```
Host=<your-endpoint>.clickhouse.cloud;Port=8123;Database=default;Username=default;Password=<your-password>;UseServerTimezone=false
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `CLICKHOUSE_CONNECTION_STRING` | ClickHouse connection string | `Host=localhost;Port=8123;Database=default;Username=default;Password=` |
| `CLICKHOUSE_TABLE_NAME` | Name of the table to store pod/container metrics | `k8s_metrics` |
| `METRICS_COLLECTION_INTERVAL_SECONDS` | Interval between metric collections | `30` |

**Note**: Node metrics are stored in a separate table `k8s_node_metrics` (not configurable via environment variable).

## Manual Table Creation

If you need to create the table manually, you can run:

```sql
CREATE TABLE IF NOT EXISTS k8s_metrics
(
    timestamp DateTime64(3) DEFAULT now64(),
    namespace String,
    pod String,
    container String,
    cpu_usage_cores Float64,
    cpu_limit_cores Nullable(Float64),
    memory_usage_bytes Float64
)
ENGINE = MergeTree()
ORDER BY (timestamp, namespace, pod, container)
TTL toDateTime(timestamp) + INTERVAL 90 DAY
```

## Querying Metrics

### Pod/Container Metrics Queries

**Get latest CPU usage for all containers:**
```sql
SELECT 
    namespace,
    pod,
    container,
    cpu_usage_cores,
    cpu_limit_cores,
    memory_usage_bytes,
    timestamp
FROM k8s_metrics
ORDER BY timestamp DESC
LIMIT 100
```

**Get average CPU usage per namespace in the last hour:**
```sql
SELECT 
    namespace,
    avg(cpu_usage_cores) as avg_cpu_cores,
    max(cpu_usage_cores) as max_cpu_cores
FROM k8s_metrics
WHERE timestamp >= now() - INTERVAL 1 HOUR
GROUP BY namespace
ORDER BY avg_cpu_cores DESC
```

**Get memory usage trends for a specific pod:**
```sql
SELECT 
    timestamp,
    container,
    memory_usage_bytes / 1024 / 1024 as memory_mb
FROM k8s_metrics
WHERE namespace = 'alerthawk' 
  AND pod = 'my-pod-name'
  AND timestamp >= now() - INTERVAL 24 HOUR
ORDER BY timestamp ASC
```

### Node Metrics Queries

**Get latest metrics for all nodes:**
```sql
SELECT 
    node_name,
    cpu_usage_cores,
    cpu_capacity_cores,
    (cpu_usage_cores / cpu_capacity_cores * 100) as cpu_percent,
    memory_usage_bytes / 1024 / 1024 / 1024 as memory_usage_gb,
    memory_capacity_bytes / 1024 / 1024 / 1024 as memory_capacity_gb,
    (memory_usage_bytes / memory_capacity_bytes * 100) as memory_percent,
    timestamp
FROM k8s_node_metrics
ORDER BY timestamp DESC
LIMIT 50
```

**Get average CPU and memory usage per node (last hour):**
```sql
SELECT 
    node_name,
    avg(cpu_usage_cores) as avg_cpu_cores,
    max(cpu_usage_cores) as max_cpu_cores,
    avg(memory_usage_bytes) / 1024 / 1024 / 1024 as avg_memory_gb,
    max(memory_usage_bytes) / 1024 / 1024 / 1024 as max_memory_gb
FROM k8s_node_metrics
WHERE timestamp >= now() - INTERVAL 1 HOUR
GROUP BY node_name
ORDER BY avg_cpu_cores DESC
```

**Get node resource utilization trends:**
```sql
SELECT 
    timestamp,
    node_name,
    (cpu_usage_cores / cpu_capacity_cores * 100) as cpu_percent,
    (memory_usage_bytes / memory_capacity_bytes * 100) as memory_percent
FROM k8s_node_metrics
WHERE node_name = 'your-node-name'
  AND timestamp >= now() - INTERVAL 24 HOUR
ORDER BY timestamp ASC
```

**Find nodes with high CPU usage:**
```sql
SELECT 
    node_name,
    (cpu_usage_cores / cpu_capacity_cores * 100) as cpu_percent,
    cpu_usage_cores,
    cpu_capacity_cores,
    timestamp
FROM k8s_node_metrics
WHERE timestamp >= now() - INTERVAL 1 HOUR
  AND (cpu_usage_cores / cpu_capacity_cores * 100) > 80
ORDER BY cpu_percent DESC
```

## Migration from Prometheus

The application has been migrated from Prometheus to ClickHouse. The main changes:

1. **Removed**: Prometheus HTTP metrics endpoint (`/metrics`)
2. **Removed**: Prometheus gauge metrics
3. **Added**: Direct writes to ClickHouse database
4. **Added**: Automatic table creation on startup
5. **Added**: Configurable connection string and table name
6. **Added**: Node metrics collection and storage

Metrics are now stored directly in ClickHouse instead of being exposed via Prometheus format. The application collects both pod/container metrics and node-level metrics from all nodes in the cluster.

