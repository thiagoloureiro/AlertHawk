# Migration Guide: Adding cluster_name Column

This guide explains how to migrate existing ClickHouse tables to include the `cluster_name` column.

## Overview

The application now requires a `cluster_name` column in both metrics tables. If you have existing tables without this column, you need to run a migration.

## Option 1: Using the Shell Script (Recommended)

The easiest way to migrate is using the provided shell script:

```bash
# Basic usage (uses defaults: database=default, cluster_name=default-cluster, host=localhost, port=8123)
./migrate_add_cluster_name.sh

# With custom parameters
./migrate_add_cluster_name.sh <database_name> <cluster_name> <clickhouse_host> <clickhouse_port>

# Example:
./migrate_add_cluster_name.sh metrics production-cluster clickhouse.example.com 8123
```

### Script Parameters:

1. **Database name** (default: `default`)
2. **Cluster name** (default: `default-cluster`) - **IMPORTANT**: Set this to your actual cluster name!
3. **ClickHouse host** (default: `localhost`)
4. **ClickHouse port** (default: `8123`)

## Option 2: Manual SQL Migration

If you prefer to run the SQL manually:

### Step 1: Connect to ClickHouse
```bash
clickhouse-client --host=your-host --port=8123
```

### Step 2: Add column to k8s_metrics table
```sql
USE your_database_name;

ALTER TABLE k8s_metrics 
ADD COLUMN cluster_name String AFTER timestamp;
```

### Step 3: Update existing rows in k8s_metrics
```sql
-- IMPORTANT: Replace 'your-cluster-name' with your actual cluster name!
ALTER TABLE k8s_metrics 
UPDATE cluster_name = 'your-cluster-name' 
WHERE cluster_name = '' OR cluster_name IS NULL;
```

### Step 4: Add column to k8s_node_metrics table
```sql
ALTER TABLE k8s_node_metrics 
ADD COLUMN cluster_name String AFTER timestamp;
```

### Step 5: Update existing rows in k8s_node_metrics
```sql
-- IMPORTANT: Replace 'your-cluster-name' with your actual cluster name!
ALTER TABLE k8s_node_metrics 
UPDATE cluster_name = 'your-cluster-name' 
WHERE cluster_name = '' OR cluster_name IS NULL;
```

### Step 6: Verify the migration
```sql
-- Check table structure
DESCRIBE TABLE k8s_metrics;
DESCRIBE TABLE k8s_node_metrics;

-- Check sample data
SELECT timestamp, cluster_name, namespace, pod 
FROM k8s_metrics 
LIMIT 5;

SELECT timestamp, cluster_name, node_name 
FROM k8s_node_metrics 
LIMIT 5;
```

## Important Notes

1. **Cluster Name**: Make sure to use the same cluster name that you'll set in the `CLUSTER_NAME` environment variable when running the application.

2. **Existing Data**: The migration script will update all existing rows with the cluster name you specify. If you have data from multiple clusters, you'll need to:
   - Manually update rows with the correct cluster name, or
   - Re-run the migration for each cluster with the appropriate cluster name filter

3. **ORDER BY Clause**: The table's ORDER BY clause includes `cluster_name` for better query performance. If you want to update the ORDER BY immediately (instead of waiting for table recreation), you'll need to:
   - Create a new table with the updated ORDER BY
   - Copy data from the old table
   - Drop the old table
   - Rename the new table

   However, this is optional - the application will work fine with the current ORDER BY, and new tables created by the application will have the correct ORDER BY.

4. **No Data Loss**: This migration only adds a column and updates existing rows. No data will be lost.

## Troubleshooting

### Error: Column already exists
If you see an error that the column already exists, that's fine - it means the migration was already run. You can skip that step.

### Error: Table doesn't exist
If the tables don't exist yet, that's also fine - the application will create them automatically on first run with the correct schema.

### Updating ORDER BY clause
If you want to update the ORDER BY clause to include `cluster_name` for better performance, you can use this approach:

```sql
-- For k8s_metrics
CREATE TABLE k8s_metrics_new AS k8s_metrics 
ENGINE = MergeTree()
ORDER BY (timestamp, cluster_name, namespace, pod, container)
TTL toDateTime(timestamp) + INTERVAL 90 DAY;

INSERT INTO k8s_metrics_new SELECT * FROM k8s_metrics;
DROP TABLE k8s_metrics;
RENAME TABLE k8s_metrics_new TO k8s_metrics;

-- For k8s_node_metrics
CREATE TABLE k8s_node_metrics_new AS k8s_node_metrics 
ENGINE = MergeTree()
ORDER BY (timestamp, cluster_name, node_name)
TTL toDateTime(timestamp) + INTERVAL 90 DAY;

INSERT INTO k8s_node_metrics_new SELECT * FROM k8s_node_metrics;
DROP TABLE k8s_node_metrics;
RENAME TABLE k8s_node_metrics_new TO k8s_node_metrics;
```

**Warning**: This approach requires downtime and should be done during a maintenance window.


