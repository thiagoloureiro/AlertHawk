#!/bin/bash

# Migration script to add cluster_name column to ClickHouse metrics tables
# Usage: ./migrate_add_cluster_name.sh [database_name] [cluster_name] [clickhouse_host] [clickhouse_port]

set -e

# Default values
DATABASE="${1:-default}"
CLUSTER_NAME="${2:-default-cluster}"
CLICKHOUSE_HOST="${3:-localhost}"
CLICKHOUSE_PORT="${4:-8123}"

echo "============================================"
echo "ClickHouse Migration: Add cluster_name column"
echo "============================================"
echo "Database: $DATABASE"
echo "Cluster Name: $CLUSTER_NAME"
echo "ClickHouse Host: $CLICKHOUSE_HOST:$CLICKHOUSE_PORT"
echo "============================================"
echo ""

# Check if clickhouse-client is available
if ! command -v clickhouse-client &> /dev/null; then
    echo "ERROR: clickhouse-client not found. Please install ClickHouse client tools."
    exit 1
fi

# Function to execute SQL
execute_sql() {
    local sql="$1"
    local description="$2"
    
    echo "[$description]"
    echo "$sql" | clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --multiline
    echo ""
}

# Check if tables exist
echo "Checking if tables exist..."
TABLES_EXIST=$(clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --query="
    SELECT count() FROM system.tables 
    WHERE database = '$DATABASE' 
    AND name IN ('k8s_metrics', 'k8s_node_metrics')
")

if [ "$TABLES_EXIST" -eq "0" ]; then
    echo "WARNING: Tables k8s_metrics and/or k8s_node_metrics not found in database '$DATABASE'"
    echo "The application will create them automatically on first run."
    exit 0
fi

# Migrate k8s_metrics table
echo "Migrating k8s_metrics table..."

# Check if column already exists
COLUMN_EXISTS=$(clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --query="
    SELECT count() FROM system.columns 
    WHERE database = '$DATABASE' 
    AND table = 'k8s_metrics' 
    AND name = 'cluster_name'
" 2>/dev/null || echo "0")

if [ "$COLUMN_EXISTS" -eq "0" ]; then
    echo "Adding cluster_name column to k8s_metrics..."
    execute_sql "ALTER TABLE $DATABASE.k8s_metrics ADD COLUMN cluster_name String AFTER timestamp" \
        "Added cluster_name column to k8s_metrics"
else
    echo "Column cluster_name already exists in k8s_metrics, skipping..."
fi

# Update existing rows
echo "Updating existing rows in k8s_metrics with cluster name: $CLUSTER_NAME"
execute_sql "ALTER TABLE $DATABASE.k8s_metrics UPDATE cluster_name = '$CLUSTER_NAME' WHERE cluster_name = '' OR cluster_name IS NULL" \
    "Updated existing rows in k8s_metrics"

# Migrate k8s_node_metrics table
echo "Migrating k8s_node_metrics table..."

# Check if column already exists
COLUMN_EXISTS=$(clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --query="
    SELECT count() FROM system.columns 
    WHERE database = '$DATABASE' 
    AND table = 'k8s_node_metrics' 
    AND name = 'cluster_name'
" 2>/dev/null || echo "0")

if [ "$COLUMN_EXISTS" -eq "0" ]; then
    echo "Adding cluster_name column to k8s_node_metrics..."
    execute_sql "ALTER TABLE $DATABASE.k8s_node_metrics ADD COLUMN cluster_name String AFTER timestamp" \
        "Added cluster_name column to k8s_node_metrics"
else
    echo "Column cluster_name already exists in k8s_node_metrics, skipping..."
fi

# Update existing rows
echo "Updating existing rows in k8s_node_metrics with cluster name: $CLUSTER_NAME"
execute_sql "ALTER TABLE $DATABASE.k8s_node_metrics UPDATE cluster_name = '$CLUSTER_NAME' WHERE cluster_name = '' OR cluster_name IS NULL" \
    "Updated existing rows in k8s_node_metrics"

# Verification
echo "============================================"
echo "Verification"
echo "============================================"

echo "k8s_metrics table structure:"
clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --query="DESCRIBE TABLE $DATABASE.k8s_metrics"
echo ""

echo "k8s_node_metrics table structure:"
clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --query="DESCRIBE TABLE $DATABASE.k8s_node_metrics"
echo ""

echo "Sample data from k8s_metrics:"
clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --query="
    SELECT timestamp, cluster_name, namespace, pod 
    FROM $DATABASE.k8s_metrics 
    LIMIT 5
" --format=Pretty
echo ""

echo "Sample data from k8s_node_metrics:"
clickhouse-client --host="$CLICKHOUSE_HOST" --port="$CLICKHOUSE_PORT" --query="
    SELECT timestamp, cluster_name, node_name 
    FROM $DATABASE.k8s_node_metrics 
    LIMIT 5
" --format=Pretty
echo ""

echo "============================================"
echo "Migration completed successfully!"
echo "============================================"

