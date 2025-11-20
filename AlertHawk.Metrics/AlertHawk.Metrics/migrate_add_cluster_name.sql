-- Migration script to add cluster_name column to metrics tables
-- Run this script using: clickhouse-client --multiline < migrate_add_cluster_name.sql
-- Or execute each statement individually in your ClickHouse client

-- Set the database (change 'default' to your actual database name if different)
-- USE default;

-- ============================================
-- Migration for k8s_metrics table
-- ============================================

-- Check if column exists and add it if it doesn't
-- Note: ClickHouse doesn't support IF NOT EXISTS for ALTER TABLE ADD COLUMN
-- So we'll use a try-catch approach or check manually

-- Step 1: Add cluster_name column to k8s_metrics table
-- Note: If the column already exists, this will fail with an error
-- You can check if it exists first with: DESCRIBE TABLE k8s_metrics;
ALTER TABLE k8s_metrics 
ADD COLUMN cluster_name String AFTER timestamp;

-- If the above fails because the column exists, you can check with:
-- DESCRIBE TABLE k8s_metrics;

-- Step 2: Update existing rows with a default cluster name (change 'default-cluster' to your actual cluster name)
-- IMPORTANT: Update this value to match your cluster name before running!
-- Note: ClickHouse uses ALTER TABLE UPDATE syntax, not UPDATE SET
ALTER TABLE k8s_metrics 
UPDATE cluster_name = 'default-cluster' 
WHERE cluster_name = '' OR cluster_name IS NULL;

-- Step 3: Modify the ORDER BY clause to include cluster_name
-- Note: This requires recreating the table, which is more complex
-- For now, we'll just add the column. The ORDER BY will be updated when tables are recreated
-- If you need to update ORDER BY immediately, you'll need to:
-- 1. Create a new table with the new ORDER BY
-- 2. Insert data from old table
-- 3. Drop old table
-- 4. Rename new table

-- ============================================
-- Migration for k8s_node_metrics table
-- ============================================

-- Step 1: Add cluster_name column to k8s_node_metrics table
-- Note: If the column already exists, this will fail with an error
-- You can check if it exists first with: DESCRIBE TABLE k8s_node_metrics;
ALTER TABLE k8s_node_metrics 
ADD COLUMN cluster_name String AFTER timestamp;

-- Step 2: Update existing rows with a default cluster name (change 'default-cluster' to your actual cluster name)
-- IMPORTANT: Update this value to match your cluster name before running!
-- Note: ClickHouse uses ALTER TABLE UPDATE syntax, not UPDATE SET
ALTER TABLE k8s_node_metrics 
UPDATE cluster_name = 'default-cluster' 
WHERE cluster_name = '' OR cluster_name IS NULL;

-- ============================================
-- Verification queries
-- ============================================

-- Verify the columns were added
SELECT 'k8s_metrics table structure:' as info;
DESCRIBE TABLE k8s_metrics;

SELECT 'k8s_node_metrics table structure:' as info;
DESCRIBE TABLE k8s_node_metrics;

-- Check sample data
SELECT 'Sample from k8s_metrics:' as info;
SELECT timestamp, cluster_name, namespace, pod, container 
FROM k8s_metrics 
LIMIT 5;

SELECT 'Sample from k8s_node_metrics:' as info;
SELECT timestamp, cluster_name, node_name 
FROM k8s_node_metrics 
LIMIT 5;

