# Migration Guide: Pod Logs Table to ReplacingMergeTree

## Problem
The application is experiencing the following error:
```
ClickHouse.Client.ClickHouseServerException
Code: 181. DB::Exception: Storage MergeTree doesn't support FINAL. (ILLEGAL_FINAL)
```

This error occurs because:
1. The code uses `FINAL` clause in queries to get deduplicated results
2. `FINAL` is only supported by ReplacingMergeTree, CollapsingMergeTree, and similar engines
3. The existing `k8s_pod_logs` table was created with `MergeTree` engine instead of `ReplacingMergeTree`

## Solution
Migrate the `k8s_pod_logs` table to use `ReplacingMergeTree(version)` engine.

## Migration Steps

### Option 1: Using the SQL Migration Script (Recommended)

1. **Backup your data** (if needed):
   ```bash
   clickhouse-client --query="SELECT * FROM k8s_pod_logs FORMAT Native" > k8s_pod_logs_backup.native
   ```

2. **Run the migration script**:
   ```bash
   clickhouse-client < migrate_pod_logs_to_replacing_merge_tree.sql
   ```

3. **Verify the migration**:
   ```sql
   -- Check the new table structure
   SHOW CREATE TABLE k8s_pod_logs;
   
   -- Verify row counts match
   SELECT count(*) as old_count FROM k8s_pod_logs_old;
   SELECT count(*) as new_count FROM k8s_pod_logs;
   ```

4. **Drop the old table** (after verification):
   ```sql
   DROP TABLE k8s_pod_logs_old;
   ```

### Option 2: Manual Migration

If you prefer to do it manually or need more control:

1. **Check current table structure**:
   ```sql
   SHOW CREATE TABLE k8s_pod_logs;
   ```

2. **If the table uses MergeTree**, rename it:
   ```sql
   RENAME TABLE k8s_pod_logs TO k8s_pod_logs_old;
   ```

3. **Create the new table**:
   ```sql
   CREATE TABLE k8s_pod_logs
   (
       timestamp DateTime64(3) DEFAULT now64(),
       cluster_name String,
       namespace String,
       pod String,
       container String,
       log_content String,
       version DateTime64(3) DEFAULT now64()
   )
   ENGINE = ReplacingMergeTree(version)
   ORDER BY (cluster_name, namespace, pod, container)
   TTL toDateTime(timestamp) + INTERVAL 90 DAY;
   ```

4. **Copy the data**:
   ```sql
   INSERT INTO k8s_pod_logs (timestamp, cluster_name, namespace, pod, container, log_content, version)
   SELECT 
       timestamp, 
       cluster_name, 
       namespace, 
       pod, 
       container, 
       log_content,
       timestamp as version
   FROM k8s_pod_logs_old;
   ```

5. **Verify and cleanup**:
   ```sql
   SELECT count(*) FROM k8s_pod_logs;
   SELECT count(*) FROM k8s_pod_logs_old;
   -- If counts match, drop the old table:
   DROP TABLE k8s_pod_logs_old;
   ```

### Option 3: Drop and Recreate (If no data needs to be preserved)

If you don't need to preserve existing log data:

```sql
DROP TABLE IF EXISTS k8s_pod_logs;

CREATE TABLE k8s_pod_logs
(
    timestamp DateTime64(3) DEFAULT now64(),
    cluster_name String,
    namespace String,
    pod String,
    container String,
    log_content String,
    version DateTime64(3) DEFAULT now64()
)
ENGINE = ReplacingMergeTree(version)
ORDER BY (cluster_name, namespace, pod, container)
TTL toDateTime(timestamp) + INTERVAL 90 DAY;
```

## Understanding ReplacingMergeTree

- **ReplacingMergeTree** is designed for deduplication
- The `version` column determines which row is kept during merges
- The `FINAL` modifier forces immediate deduplication in queries
- Without `FINAL`, you may see duplicate rows until ClickHouse performs background merges

## Post-Migration

After migration, the application will:
- Successfully execute queries with the `FINAL` clause
- Properly deduplicate log entries based on the version column
- Continue to append logs as designed in the upsert logic

## Troubleshooting

If you still see the error after migration:
1. Verify the table engine: `SHOW CREATE TABLE k8s_pod_logs;`
2. Check you're using the correct database: `SELECT currentDatabase();`
3. Restart the application to ensure it reconnects to the database
4. Check for multiple databases or table name mismatches

## Notes

- The TTL is set to 90 days, so old logs will be automatically deleted
- The `version` column uses `DateTime64(3)` for millisecond precision
- New applications should have this table created correctly by the `EnsureTablesExistAsync()` method
