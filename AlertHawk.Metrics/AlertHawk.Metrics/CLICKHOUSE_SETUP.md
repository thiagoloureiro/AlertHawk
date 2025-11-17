# ClickHouse Setup for AlertHawk Metrics

This document describes the ClickHouse integration for storing Kubernetes metrics.

## Table Schema

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
| `CLICKHOUSE_TABLE_NAME` | Name of the table to store metrics | `k8s_metrics` |
| `METRICS_COLLECTION_INTERVAL_SECONDS` | Interval between metric collections | `30` |

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

### Example Queries

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

## Migration from Prometheus

The application has been migrated from Prometheus to ClickHouse. The main changes:

1. **Removed**: Prometheus HTTP metrics endpoint (`/metrics`)
2. **Removed**: Prometheus gauge metrics
3. **Added**: Direct writes to ClickHouse database
4. **Added**: Automatic table creation on startup
5. **Added**: Configurable connection string and table name

Metrics are now stored directly in ClickHouse instead of being exposed via Prometheus format.

