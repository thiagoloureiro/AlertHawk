-- Migration script to convert k8s_pod_logs table from MergeTree to ReplacingMergeTree
-- This is necessary to support the FINAL clause used in queries for deduplication

-- Step 1: Rename the existing table
RENAME TABLE k8s_pod_logs TO k8s_pod_logs_old;

-- Step 2: Create the new table with ReplacingMergeTree engine
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

-- Step 3: Copy data from the old table to the new table
-- Note: If the old table doesn't have a 'version' column, we'll use the timestamp as version
INSERT INTO k8s_pod_logs (timestamp, cluster_name, namespace, pod, container, log_content, version)
SELECT 
    timestamp, 
    cluster_name, 
    namespace, 
    pod, 
    container, 
    log_content,
    timestamp as version  -- Use timestamp as version if no version column exists
FROM k8s_pod_logs_old;

-- Step 4: Verify the data was copied correctly
-- SELECT count(*) as old_count FROM k8s_pod_logs_old;
-- SELECT count(*) as new_count FROM k8s_pod_logs;

-- Step 5: Drop the old table (uncomment when you're confident the migration succeeded)
-- DROP TABLE k8s_pod_logs_old;
