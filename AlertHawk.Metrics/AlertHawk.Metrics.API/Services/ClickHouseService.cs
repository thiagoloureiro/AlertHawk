using System.Diagnostics.CodeAnalysis;
using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;
using AlertHawk.Metrics.API.Models;

namespace AlertHawk.Metrics.API.Services;

[ExcludeFromCodeCoverage]
public class ClickHouseService : IClickHouseService, IDisposable
{
    private readonly string _connectionString;
    private readonly string _database;
    private readonly string _tableName;
    private readonly string _nodeTableName;
    private readonly string _podLogsTableName;
    private readonly string _eventsTableName;
    private readonly string _clusterName;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    public ClickHouseService(string connectionString, string? clusterName = null, string tableName = "k8s_metrics", string nodeTableName = "k8s_node_metrics", string podLogsTableName = "k8s_pod_logs", string eventsTableName = "k8s_events")
    {
        _connectionString = connectionString;
        _database = ExtractDatabaseFromConnectionString(connectionString);
        _clusterName = clusterName ?? string.Empty;
        _tableName = tableName;
        _nodeTableName = nodeTableName;
        _podLogsTableName = podLogsTableName;
        _eventsTableName = eventsTableName;
        EnsureTablesExistAsync().GetAwaiter().GetResult();
    }

    private static string ExtractDatabaseFromConnectionString(string connectionString)
    {
        // Parse Database=value from connection string
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 && keyValue[0].Trim().Equals("Database", StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1].Trim();
            }
        }
        return "default";
    }

    private async Task EnsureTablesExistAsync()
    {
        await using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();
        
        // Ensure database exists
        try
        {
            await using var createDbCommand = connection.CreateCommand();
            createDbCommand.CommandText = $"CREATE DATABASE IF NOT EXISTS {_database}";
            await createDbCommand.ExecuteNonQueryAsync();
            Console.WriteLine($"Database '{_database}' ensured to exist");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not ensure database exists (it may already exist): {ex.Message}");
        }
        
        // Use the database explicitly
        await using var useDbCommand = connection.CreateCommand();
        useDbCommand.CommandText = $"USE {_database}";
        await useDbCommand.ExecuteNonQueryAsync();
        
        // Create pod/container metrics table
        var createTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_database}.{_tableName}
            (
                timestamp DateTime64(3) DEFAULT now64(),
                cluster_name String,
                namespace String,
                pod String,
                container String,
                cpu_usage_cores Float64,
                cpu_limit_cores Nullable(Float64),
                memory_usage_bytes Float64,
                node_name Nullable(String),
                pod_state Nullable(String),
                restart_count UInt32,
                pod_age Nullable(Int64)
            )
            ENGINE = MergeTree()
            ORDER BY (timestamp, cluster_name, namespace, pod, container)
            TTL toDateTime(timestamp) + INTERVAL 90 DAY
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync();
        Console.WriteLine($"Table '{_database}.{_tableName}' ensured to exist");

        // Add node_name column if it doesn't exist (for existing tables)
        try
        {
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $@"
                ALTER TABLE {_database}.{_tableName}
                ADD COLUMN IF NOT EXISTS node_name Nullable(String)
            ";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine($"Column 'node_name' ensured to exist in '{_database}.{_tableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add node_name column (it may already exist): {ex.Message}");
        }

        // Add pod_state, restart_count, and pod_age columns if they don't exist (for existing tables)
        try
        {
            await using var alterCommand1 = connection.CreateCommand();
            alterCommand1.CommandText = $@"
                ALTER TABLE {_database}.{_tableName}
                ADD COLUMN IF NOT EXISTS pod_state Nullable(String)
            ";
            await alterCommand1.ExecuteNonQueryAsync();
            
            await using var alterCommand2 = connection.CreateCommand();
            alterCommand2.CommandText = $@"
                ALTER TABLE {_database}.{_tableName}
                ADD COLUMN IF NOT EXISTS restart_count UInt32 DEFAULT 0
            ";
            await alterCommand2.ExecuteNonQueryAsync();
            
            await using var alterCommand3 = connection.CreateCommand();
            alterCommand3.CommandText = $@"
                ALTER TABLE {_database}.{_tableName}
                ADD COLUMN IF NOT EXISTS pod_age Nullable(Int64)
            ";
            await alterCommand3.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Columns 'pod_state', 'restart_count', and 'pod_age' ensured to exist in '{_database}.{_tableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add pod_state/restart_count/pod_age columns (they may already exist): {ex.Message}");
        }

        // Create node metrics table
        var createNodeTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_database}.{_nodeTableName}
            (
                timestamp DateTime64(3) DEFAULT now64(),
                cluster_name String,
                cluster_environment Nullable(String),
                node_name String,
                cpu_usage_cores Float64,
                cpu_capacity_cores Float64,
                memory_usage_bytes Float64,
                memory_capacity_bytes Float64,
                kubernetes_version Nullable(String),
                cloud_provider Nullable(String),
                is_ready Nullable(UInt8),
                has_memory_pressure Nullable(UInt8),
                has_disk_pressure Nullable(UInt8),
                has_pid_pressure Nullable(UInt8),
                architecture Nullable(String),
                operating_system Nullable(String),
                region Nullable(String),
                instance_type Nullable(String)
            )
            ENGINE = MergeTree()
            ORDER BY (timestamp, cluster_name, node_name)
            TTL toDateTime(timestamp) + INTERVAL 90 DAY
        ";

        await using var nodeCommand = connection.CreateCommand();
        nodeCommand.CommandText = createNodeTableSql;
        await nodeCommand.ExecuteNonQueryAsync();
        Console.WriteLine($"Table '{_database}.{_nodeTableName}' ensured to exist");

        // Add kubernetes_version and cloud_provider columns if they don't exist (for existing tables)
        try
        {
            await using var alterCommand1 = connection.CreateCommand();
            alterCommand1.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS kubernetes_version Nullable(String)
            ";
            await alterCommand1.ExecuteNonQueryAsync();
            
            await using var alterCommand2 = connection.CreateCommand();
            alterCommand2.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS cloud_provider Nullable(String)
            ";
            await alterCommand2.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Columns 'kubernetes_version' and 'cloud_provider' ensured to exist in '{_database}.{_nodeTableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add kubernetes_version/cloud_provider columns (they may already exist): {ex.Message}");
        }

        // Add node condition columns if they don't exist (for existing tables)
        try
        {
            await using var alterCommand1 = connection.CreateCommand();
            alterCommand1.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS is_ready Nullable(UInt8)
            ";
            await alterCommand1.ExecuteNonQueryAsync();
            
            await using var alterCommand2 = connection.CreateCommand();
            alterCommand2.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS has_memory_pressure Nullable(UInt8)
            ";
            await alterCommand2.ExecuteNonQueryAsync();
            
            await using var alterCommand3 = connection.CreateCommand();
            alterCommand3.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS has_disk_pressure Nullable(UInt8)
            ";
            await alterCommand3.ExecuteNonQueryAsync();
            
            await using var alterCommand4 = connection.CreateCommand();
            alterCommand4.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS has_pid_pressure Nullable(UInt8)
            ";
            await alterCommand4.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Columns 'is_ready', 'has_memory_pressure', 'has_disk_pressure', and 'has_pid_pressure' ensured to exist in '{_database}.{_nodeTableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add node condition columns (they may already exist): {ex.Message}");
        }

        // Add architecture and operating_system columns if they don't exist (for existing tables)
        try
        {
            await using var alterCommand1 = connection.CreateCommand();
            alterCommand1.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS architecture Nullable(String)
            ";
            await alterCommand1.ExecuteNonQueryAsync();
            
            await using var alterCommand2 = connection.CreateCommand();
            alterCommand2.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS operating_system Nullable(String)
            ";
            await alterCommand2.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Columns 'architecture' and 'operating_system' ensured to exist in '{_database}.{_nodeTableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add architecture/operating_system columns (they may already exist): {ex.Message}");
        }

        // Add region and instance_type columns if they don't exist (for existing tables)
        try
        {
            await using var alterCommand1 = connection.CreateCommand();
            alterCommand1.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS region Nullable(String)
            ";
            await alterCommand1.ExecuteNonQueryAsync();
            
            await using var alterCommand2 = connection.CreateCommand();
            alterCommand2.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS instance_type Nullable(String)
            ";
            await alterCommand2.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Columns 'region' and 'instance_type' ensured to exist in '{_database}.{_nodeTableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add region/instance_type columns (they may already exist): {ex.Message}");
        }

        // Add cluster_environment column if it doesn't exist (for existing tables)
        try
        {
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $@"
                ALTER TABLE {_database}.{_nodeTableName}
                ADD COLUMN IF NOT EXISTS cluster_environment Nullable(String)
            ";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine($"Column 'cluster_environment' ensured to exist in '{_database}.{_nodeTableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add cluster_environment column (it may already exist): {ex.Message}");
        }

        // Create pod logs table with ReplacingMergeTree to keep only latest log per pod/container
        var createPodLogsTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_database}.{_podLogsTableName}
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
            TTL toDateTime(timestamp) + INTERVAL 90 DAY
        ";

        await using var podLogsCommand = connection.CreateCommand();
        podLogsCommand.CommandText = createPodLogsTableSql;
        await podLogsCommand.ExecuteNonQueryAsync();
        Console.WriteLine($"Table '{_database}.{_podLogsTableName}' ensured to exist");

        // Add version column if it doesn't exist (for existing tables that were created with MergeTree)
        try
        {
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $@"
                ALTER TABLE {_database}.{_podLogsTableName}
                ADD COLUMN IF NOT EXISTS version DateTime64(3) DEFAULT now64()
            ";
            await alterCommand.ExecuteNonQueryAsync();
            Console.WriteLine($"Column 'version' ensured to exist in '{_database}.{_podLogsTableName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add version column (it may already exist): {ex.Message}");
        }

        // Check if table needs to be recreated with ReplacingMergeTree
        try
        {
            await using var checkEngineCommand = connection.CreateCommand();
            checkEngineCommand.CommandText = $@"
                SELECT engine 
                FROM system.tables 
                WHERE database = '{_database}' AND name = '{_podLogsTableName}'
            ";
            await using var reader = await checkEngineCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var engine = reader.GetString(0);
                if (!engine.Contains("ReplacingMergeTree"))
                {
                    Console.WriteLine($"WARNING: Table '{_database}.{_podLogsTableName}' is using '{engine}' engine instead of 'ReplacingMergeTree'.");
                    Console.WriteLine($"Dropping and recreating the table with ReplacingMergeTree engine...");
                    
                    // Drop the old table
                    await using var dropCommand = connection.CreateCommand();
                    dropCommand.CommandText = $"DROP TABLE IF EXISTS {_database}.{_podLogsTableName}";
                    await dropCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"Dropped old table '{_database}.{_podLogsTableName}'");
                    
                    // Recreate with ReplacingMergeTree
                    await using var recreateCommand = connection.CreateCommand();
                    recreateCommand.CommandText = createPodLogsTableSql;
                    await recreateCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"Recreated table '{_database}.{_podLogsTableName}' with ReplacingMergeTree engine");
                }
                else
                {
                    Console.WriteLine($"Table '{_database}.{_podLogsTableName}' is correctly using '{engine}' engine");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not verify or migrate table engine: {ex.Message}");
        }

        // Create Kubernetes events table
        var createEventsTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_database}.{_eventsTableName}
            (
                timestamp DateTime64(3) DEFAULT now64(),
                cluster_name String,
                namespace String,
                event_name String,
                event_uid String,
                involved_object_kind String,
                involved_object_name String,
                involved_object_namespace String,
                event_type String,
                reason String,
                message String,
                source_component String,
                count UInt32,
                first_timestamp Nullable(DateTime64(3)),
                last_timestamp Nullable(DateTime64(3))
            )
            ENGINE = ReplacingMergeTree(last_timestamp)
            ORDER BY (cluster_name, namespace, event_uid, involved_object_kind, involved_object_name)
            TTL toDateTime(timestamp) + INTERVAL 90 DAY
        ";

        await using var eventsCommand = connection.CreateCommand();
        eventsCommand.CommandText = createEventsTableSql;
        await eventsCommand.ExecuteNonQueryAsync();
        Console.WriteLine($"Table '{_database}.{_eventsTableName}' ensured to exist");
    }

    public async Task WriteMetricsAsync(
        string @namespace,
        string pod,
        string container,
        double cpuUsageCores,
        double? cpuLimitCores,
        double memoryUsageBytes,
        string? clusterName = null,
        string? nodeName = null,
        string? podState = null,
        int restartCount = 0,
        long? podAge = null)
    {
        var effectiveClusterName = clusterName ?? _clusterName;
        if (string.IsNullOrWhiteSpace(effectiveClusterName))
        {
            throw new InvalidOperationException("Cluster name is required for writing metrics. Provide it as a parameter or set CLUSTER_NAME environment variable.");
        }

        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            // Use ClickHouse format with proper escaping
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var escapedNamespace = @namespace.Replace("'", "''").Replace("\\", "\\\\");
            var escapedPod = pod.Replace("'", "''").Replace("\\", "\\\\");
            var escapedContainer = container.Replace("'", "''").Replace("\\", "\\\\");
            var cpuLimitValue = cpuLimitCores?.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";
            var nodeNameValue = !string.IsNullOrWhiteSpace(nodeName)
                ? $"'{nodeName.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var podStateValue = !string.IsNullOrWhiteSpace(podState)
                ? $"'{podState.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var podAgeValue = podAge?.ToString() ?? "NULL";

            var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
            var insertSql = $@"
                INSERT INTO {_database}.{_tableName}
                (timestamp, cluster_name, namespace, pod, container, cpu_usage_cores, cpu_limit_cores, memory_usage_bytes, node_name, pod_state, restart_count, pod_age)
                VALUES
                ('{timestamp}', '{escapedClusterName}', '{escapedNamespace}', '{escapedPod}', '{escapedContainer}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuLimitValue}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {nodeNameValue}, {podStateValue}, {restartCount}, {podAgeValue})
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task WriteNodeMetricsAsync(
        string nodeName,
        double cpuUsageCores,
        double cpuCapacityCores,
        double memoryUsageBytes,
        double memoryCapacityBytes,
        string? clusterName = null,
        string? clusterEnvironment = null,
        string? kubernetesVersion = null,
        string? cloudProvider = null,
        bool? isReady = null,
        bool? hasMemoryPressure = null,
        bool? hasDiskPressure = null,
        bool? hasPidPressure = null,
        string? architecture = null,
        string? operatingSystem = null,
        string? region = null,
        string? instanceType = null)
    {
        var effectiveClusterName = clusterName ?? _clusterName;
        if (string.IsNullOrWhiteSpace(effectiveClusterName))
        {
            throw new InvalidOperationException("Cluster name is required for writing metrics. Provide it as a parameter or set CLUSTER_NAME environment variable.");
        }
        var effectiveClusterEnvironment = !string.IsNullOrWhiteSpace(clusterEnvironment) 
            ? clusterEnvironment 
            : "PROD";

        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            // Use ClickHouse format with proper escaping
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var escapedNodeName = nodeName.Replace("'", "''").Replace("\\", "\\\\");
            var kubernetesVersionValue = !string.IsNullOrWhiteSpace(kubernetesVersion)
                ? $"'{kubernetesVersion.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var cloudProviderValue = !string.IsNullOrWhiteSpace(cloudProvider)
                ? $"'{cloudProvider.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var isReadyValue = isReady.HasValue ? (isReady.Value ? "1" : "0") : "NULL";
            var hasMemoryPressureValue = hasMemoryPressure.HasValue ? (hasMemoryPressure.Value ? "1" : "0") : "NULL";
            var hasDiskPressureValue = hasDiskPressure.HasValue ? (hasDiskPressure.Value ? "1" : "0") : "NULL";
            var hasPidPressureValue = hasPidPressure.HasValue ? (hasPidPressure.Value ? "1" : "0") : "NULL";
            var architectureValue = !string.IsNullOrWhiteSpace(architecture)
                ? $"'{architecture.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var operatingSystemValue = !string.IsNullOrWhiteSpace(operatingSystem)
                ? $"'{operatingSystem.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var regionValue = !string.IsNullOrWhiteSpace(region)
                ? $"'{region.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var instanceTypeValue = !string.IsNullOrWhiteSpace(instanceType)
                ? $"'{instanceType.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";
            var clusterEnvironmentValue = !string.IsNullOrWhiteSpace(effectiveClusterEnvironment)
                ? $"'{effectiveClusterEnvironment.Replace("'", "''").Replace("\\", "\\\\")}'"
                : "NULL";

            var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
            var insertSql = $@"
                INSERT INTO {_database}.{_nodeTableName}
                (timestamp, cluster_name, cluster_environment, node_name, cpu_usage_cores, cpu_capacity_cores, memory_usage_bytes, memory_capacity_bytes, kubernetes_version, cloud_provider, is_ready, has_memory_pressure, has_disk_pressure, has_pid_pressure, architecture, operating_system, region, instance_type)
                VALUES
                ('{timestamp}', '{escapedClusterName}', {clusterEnvironmentValue}, '{escapedNodeName}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuCapacityCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {memoryCapacityBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {kubernetesVersionValue}, {cloudProviderValue}, {isReadyValue}, {hasMemoryPressureValue}, {hasDiskPressureValue}, {hasPidPressureValue}, {architectureValue}, {operatingSystemValue}, {regionValue}, {instanceTypeValue})
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<PodMetricDto>> GetMetricsByNamespaceAsync(string? namespaceFilter = null, int? minutes = 1440, string? clusterName = null)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var effectiveClusterName = clusterName ?? _clusterName;
            var minutesValue = minutes ?? 1440;
            var whereClause = $"timestamp >= now() - INTERVAL {minutesValue} MINUTE";
            if (!string.IsNullOrWhiteSpace(effectiveClusterName))
            {
                var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
                whereClause += $" AND cluster_name = '{escapedClusterName}'";
            }
            if (!string.IsNullOrWhiteSpace(namespaceFilter))
            {
                var escapedNamespace = namespaceFilter.Replace("'", "''").Replace("\\", "\\\\");
                whereClause += $" AND namespace = '{escapedNamespace}'";
            }

            string query;
            
            // If more than 6 hours (360 minutes), interpolate data using time intervals
            if (minutesValue > 360)
            {
                // Calculate interval based on time range:
                // - 6-24 hours: 5 minute intervals
                // - 1-7 days: 15 minute intervals
                // - 7+ days: 30 minute intervals
                int intervalMinutes;
                if (minutesValue <= 1440) // Up to 24 hours
                {
                    intervalMinutes = 5;
                }
                else if (minutesValue <= 10080) // Up to 7 days
                {
                    intervalMinutes = 15;
                }
                else // More than 7 days
                {
                    intervalMinutes = 30;
                }

                query = $@"
                    SELECT 
                        toStartOfInterval(timestamp, INTERVAL {intervalMinutes} MINUTE) AS timestamp,
                        cluster_name,
                        namespace,
                        pod,
                        container,
                        avg(cpu_usage_cores) AS cpu_usage_cores,
                        avg(cpu_limit_cores) AS cpu_limit_cores,
                        avg(memory_usage_bytes) AS memory_usage_bytes,
                        any(node_name) AS node_name,
                        any(pod_state) AS pod_state,
                        max(restart_count) AS restart_count,
                        max(pod_age) AS pod_age
                    FROM {_database}.{_tableName}
                    WHERE {whereClause}
                    GROUP BY 
                        timestamp,
                        cluster_name,
                        namespace,
                        pod,
                        container
                    ORDER BY timestamp DESC
                ";
            }
            else
            {
                // Return all data without interpolation for <= 6 hours
                query = $@"
                    SELECT 
                        timestamp,
                        cluster_name,
                        namespace,
                        pod,
                        container,
                        cpu_usage_cores,
                        cpu_limit_cores,
                        memory_usage_bytes,
                        node_name,
                        pod_state,
                        restart_count,
                        pod_age
                    FROM {_database}.{_tableName}
                    WHERE {whereClause}
                    ORDER BY timestamp DESC
                ";
            }

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var results = new List<PodMetricDto>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new PodMetricDto
                {
                    Timestamp = reader.GetDateTime(0),
                    ClusterName = reader.GetString(1),
                    Namespace = reader.GetString(2),
                    Pod = reader.GetString(3),
                    Container = reader.GetString(4),
                    CpuUsageCores = reader.GetDouble(5),
                    CpuLimitCores = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                    MemoryUsageBytes = reader.GetDouble(7),
                    NodeName = reader.IsDBNull(8) ? null : reader.GetString(8),
                    PodState = reader.IsDBNull(9) ? null : reader.GetString(9),
                    RestartCount = reader.IsDBNull(10) ? 0 : Convert.ToInt32(reader.GetValue(10)),
                    PodAge = reader.IsDBNull(11) ? null : reader.GetInt64(11)
                });
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<NodeMetricDto>> GetNodeMetricsAsync(string? nodeNameFilter = null, int? minutes = 1440, string? clusterName = null)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var effectiveClusterName = clusterName ?? _clusterName;
            var minutesValue = minutes ?? 1440;
            var whereClause = $"timestamp >= now() - INTERVAL {minutesValue} MINUTE";
            if (!string.IsNullOrWhiteSpace(effectiveClusterName))
            {
                var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
                whereClause += $" AND cluster_name = '{escapedClusterName}'";
            }
            if (!string.IsNullOrWhiteSpace(nodeNameFilter))
            {
                var escapedNodeName = nodeNameFilter.Replace("'", "''").Replace("\\", "\\\\");
                whereClause += $" AND node_name = '{escapedNodeName}'";
            }

            string query;
            
            // If more than 6 hours (360 minutes), interpolate data using time intervals
            if (minutesValue > 360)
            {
                // Calculate interval based on time range:
                // - 6-24 hours: 5 minute intervals
                // - 1-7 days: 15 minute intervals
                // - 7+ days: 30 minute intervals
                int intervalMinutes;
                if (minutesValue <= 1440) // Up to 24 hours
                {
                    intervalMinutes = 5;
                }
                else if (minutesValue <= 10080) // Up to 7 days
                {
                    intervalMinutes = 15;
                }
                else // More than 7 days
                {
                    intervalMinutes = 30;
                }

                query = $@"
                    SELECT 
                        toStartOfInterval(timestamp, INTERVAL {intervalMinutes} MINUTE) AS timestamp,
                        cluster_name,
                        any(cluster_environment) AS cluster_environment,
                        node_name,
                        avg(cpu_usage_cores) AS cpu_usage_cores,
                        avg(cpu_capacity_cores) AS cpu_capacity_cores,
                        avg(memory_usage_bytes) AS memory_usage_bytes,
                        avg(memory_capacity_bytes) AS memory_capacity_bytes,
                        any(kubernetes_version) AS kubernetes_version,
                        any(cloud_provider) AS cloud_provider,
                        any(is_ready) AS is_ready,
                        any(has_memory_pressure) AS has_memory_pressure,
                        any(has_disk_pressure) AS has_disk_pressure,
                        any(has_pid_pressure) AS has_pid_pressure,
                        any(architecture) AS architecture,
                        any(operating_system) AS operating_system,
                        any(region) AS region,
                        any(instance_type) AS instance_type
                    FROM {_database}.{_nodeTableName}
                    WHERE {whereClause}
                    GROUP BY 
                        timestamp,
                        cluster_name,
                        node_name
                    ORDER BY timestamp DESC
                ";
            }
            else
            {
                // Return all data without interpolation for <= 6 hours
                query = $@"
                    SELECT 
                        timestamp,
                        cluster_name,
                        cluster_environment,
                        node_name,
                        cpu_usage_cores,
                        cpu_capacity_cores,
                        memory_usage_bytes,
                        memory_capacity_bytes,
                        kubernetes_version,
                        cloud_provider,
                        is_ready,
                        has_memory_pressure,
                        has_disk_pressure,
                        has_pid_pressure,
                        architecture,
                        operating_system,
                        region,
                        instance_type
                    FROM {_database}.{_nodeTableName}
                    WHERE {whereClause}
                    ORDER BY timestamp DESC
                ";
            }

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var results = new List<NodeMetricDto>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new NodeMetricDto
                {
                    Timestamp = reader.GetDateTime(0),
                    ClusterName = reader.GetString(1),
                    ClusterEnvironment = reader.IsDBNull(2) ? null : reader.GetString(2),
                    NodeName = reader.GetString(3),
                    CpuUsageCores = reader.GetDouble(4),
                    CpuCapacityCores = reader.GetDouble(5),
                    MemoryUsageBytes = reader.GetDouble(6),
                    MemoryCapacityBytes = reader.GetDouble(7),
                    KubernetesVersion = reader.IsDBNull(8) ? null : reader.GetString(8),
                    CloudProvider = reader.IsDBNull(9) ? null : reader.GetString(9),
                    IsReady = reader.IsDBNull(10) ? null : (reader.GetByte(10) == 1),
                    HasMemoryPressure = reader.IsDBNull(11) ? null : (reader.GetByte(11) == 1),
                    HasDiskPressure = reader.IsDBNull(12) ? null : (reader.GetByte(12) == 1),
                    HasPidPressure = reader.IsDBNull(13) ? null : (reader.GetByte(13) == 1),
                    Architecture = reader.IsDBNull(14) ? null : reader.GetString(14),
                    OperatingSystem = reader.IsDBNull(15) ? null : reader.GetString(15),
                    Region = reader.IsDBNull(16) ? null : reader.GetString(16),
                    InstanceType = reader.IsDBNull(17) ? null : reader.GetString(17)
                });
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<string>> GetUniqueClusterNamesAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            // Get distinct cluster names from both tables using UNION
            var query = $@"
                SELECT DISTINCT cluster_name
                FROM (
                    SELECT cluster_name FROM {_database}.{_tableName}
                    UNION ALL
                    SELECT cluster_name FROM {_database}.{_nodeTableName}
                )
                WHERE cluster_name != ''
                ORDER BY cluster_name
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var results = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var clusterName = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(clusterName))
                {
                    results.Add(clusterName);
                }
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<string>> GetUniqueNamespaceNamesAsync(string? clusterName = null)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var effectiveClusterName = clusterName ?? _clusterName;
            var whereClause = "namespace != ''";
            if (!string.IsNullOrWhiteSpace(effectiveClusterName))
            {
                var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
                whereClause += $" AND cluster_name = '{escapedClusterName}'";
            }

            // Get distinct namespace names from the metrics table
            var query = $@"
                SELECT DISTINCT namespace
                FROM {_database}.{_tableName}
                WHERE {whereClause}
                ORDER BY namespace
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var results = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var namespaceName = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(namespaceName))
                {
                    results.Add(namespaceName);
                }
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task CleanupMetricsAsync(int days)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            await using var useDbCommand = connection.CreateCommand();
            useDbCommand.CommandText = $"USE {_database}";
            await useDbCommand.ExecuteNonQueryAsync();

            if (days == 0)
            {
                // Truncate all four tables
                await using var truncateMetricsCommand = connection.CreateCommand();
                truncateMetricsCommand.CommandText = $"TRUNCATE TABLE IF EXISTS {_database}.{_tableName}";
                await truncateMetricsCommand.ExecuteNonQueryAsync();

                await using var truncateNodeMetricsCommand = connection.CreateCommand();
                truncateNodeMetricsCommand.CommandText = $"TRUNCATE TABLE IF EXISTS {_database}.{_nodeTableName}";
                await truncateNodeMetricsCommand.ExecuteNonQueryAsync();

                await using var truncatePodLogsCommand = connection.CreateCommand();
                truncatePodLogsCommand.CommandText = $"TRUNCATE TABLE IF EXISTS {_database}.{_podLogsTableName}";
                await truncatePodLogsCommand.ExecuteNonQueryAsync();

                await using var truncateEventsCommand = connection.CreateCommand();
                truncateEventsCommand.CommandText = $"TRUNCATE TABLE IF EXISTS {_database}.{_eventsTableName}";
                await truncateEventsCommand.ExecuteNonQueryAsync();
            }
            else
            {
                // Delete records older than the specified number of days
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var cutoffDateString = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss.fff");

                await using var deleteMetricsCommand = connection.CreateCommand();
                deleteMetricsCommand.CommandText = $@"
                    ALTER TABLE {_database}.{_tableName}
                    DELETE WHERE timestamp < '{cutoffDateString}'
                ";
                await deleteMetricsCommand.ExecuteNonQueryAsync();

                await using var deleteNodeMetricsCommand = connection.CreateCommand();
                deleteNodeMetricsCommand.CommandText = $@"
                    ALTER TABLE {_database}.{_nodeTableName}
                    DELETE WHERE timestamp < '{cutoffDateString}'
                ";
                await deleteNodeMetricsCommand.ExecuteNonQueryAsync();

                await using var deletePodLogsCommand = connection.CreateCommand();
                deletePodLogsCommand.CommandText = $@"
                    ALTER TABLE {_database}.{_podLogsTableName}
                    DELETE WHERE timestamp < '{cutoffDateString}'
                ";
                await deletePodLogsCommand.ExecuteNonQueryAsync();

                await using var deleteEventsCommand = connection.CreateCommand();
                deleteEventsCommand.CommandText = $@"
                    ALTER TABLE {_database}.{_eventsTableName}
                    DELETE WHERE timestamp < '{cutoffDateString}'
                ";
                await deleteEventsCommand.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task WritePodLogAsync(
        string @namespace,
        string pod,
        string container,
        string logContent,
        string? clusterName = null)
    {
        var effectiveClusterName = clusterName ?? _clusterName;
        if (string.IsNullOrWhiteSpace(effectiveClusterName))
        {
            throw new InvalidOperationException("Cluster name is required for writing pod logs. Provide it as a parameter or set CLUSTER_NAME environment variable.");
        }

        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            // Use ClickHouse format with proper escaping
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var version = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var escapedNamespace = @namespace.Replace("'", "''").Replace("\\", "\\\\");
            var escapedPod = pod.Replace("'", "''").Replace("\\", "\\\\");
            var escapedContainer = container.Replace("'", "''").Replace("\\", "\\\\");
            var escapedLogContent = logContent.Replace("'", "''").Replace("\\", "\\\\");
            var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");

            // Insert new log entry with new version
            // ReplacingMergeTree will automatically replace old rows with the same ORDER BY key (cluster_name, namespace, pod, container)
            // when the version is higher, keeping only the latest log per pod/container
            var insertSql = $@"
                INSERT INTO {_database}.{_podLogsTableName}
                (timestamp, cluster_name, namespace, pod, container, log_content, version)
                VALUES
                ('{timestamp}', '{escapedClusterName}', '{escapedNamespace}', '{escapedPod}', '{escapedContainer}', '{escapedLogContent}', '{version}')
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<PodLogDto>> GetPodLogsAsync(
        string? namespaceFilter = null,
        string? podFilter = null,
        string? containerFilter = null,
        int? minutes = 1440,
        int limit = 100,
        string? clusterName = null)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var effectiveClusterName = clusterName ?? _clusterName;
            
            // Build WHERE clause with parameter placeholders
            var whereConditions = new List<string> { "timestamp >= now() - INTERVAL {minutes:Int32} MINUTE" };
            
            if (!string.IsNullOrWhiteSpace(effectiveClusterName))
            {
                whereConditions.Add("cluster_name = {cluster_name:String}");
            }
            if (!string.IsNullOrWhiteSpace(namespaceFilter))
            {
                whereConditions.Add("namespace = {namespace:String}");
            }
            if (!string.IsNullOrWhiteSpace(podFilter))
            {
                whereConditions.Add("pod = {pod:String}");
            }
            if (!string.IsNullOrWhiteSpace(containerFilter))
            {
                whereConditions.Add("container = {container:String}");
            }
            
            var whereClause = string.Join(" AND ", whereConditions);

            // Check table engine to determine if we can use FINAL
            string? tableEngine = null;
            try
            {
                await using var checkEngineCommand = connection.CreateCommand();
                checkEngineCommand.CommandText = @"
                    SELECT engine 
                    FROM system.tables 
                    WHERE database = {database:String} AND name = {table_name:String}
                ";
                
                checkEngineCommand.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "database",
                    DbType = System.Data.DbType.String,
                    Value = _database
                });
                
                checkEngineCommand.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "table_name",
                    DbType = System.Data.DbType.String,
                    Value = _podLogsTableName
                });
                
                await using var engineReader = await checkEngineCommand.ExecuteReaderAsync();
                if (await engineReader.ReadAsync())
                {
                    tableEngine = engineReader.GetString(0);
                }
            }
            catch
            {
                // If we can't check, assume ReplacingMergeTree and try FINAL
            }

            // Use FINAL only if table is using ReplacingMergeTree, otherwise query without FINAL
            var finalClause = (tableEngine != null && tableEngine.Contains("ReplacingMergeTree")) ? " FINAL" : "";
            
            // Build query with parameter placeholders (using string concatenation for table names which are safe)
            var query = $@"
                SELECT 
                    timestamp,
                    cluster_name,
                    namespace,
                    pod,
                    container,
                    log_content
                FROM {_database}.{_podLogsTableName}{finalClause}
                WHERE {whereClause}
                ORDER BY timestamp DESC
                LIMIT " + "{limit:Int32}";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            
            // Add parameters
            command.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "minutes",
                DbType = System.Data.DbType.Int32,
                Value = minutes ?? 1440
            });
            
            command.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "limit",
                DbType = System.Data.DbType.Int32,
                Value = limit
            });
            
            if (!string.IsNullOrWhiteSpace(effectiveClusterName))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "cluster_name",
                    DbType = System.Data.DbType.String,
                    Value = effectiveClusterName
                });
            }
            
            if (!string.IsNullOrWhiteSpace(namespaceFilter))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "namespace",
                    DbType = System.Data.DbType.String,
                    Value = namespaceFilter
                });
            }
            
            if (!string.IsNullOrWhiteSpace(podFilter))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "pod",
                    DbType = System.Data.DbType.String,
                    Value = podFilter
                });
            }
            
            if (!string.IsNullOrWhiteSpace(containerFilter))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "container",
                    DbType = System.Data.DbType.String,
                    Value = containerFilter
                });
            }
            
            var results = new List<PodLogDto>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new PodLogDto
                {
                    Timestamp = reader.GetDateTime(0),
                    ClusterName = reader.GetString(1),
                    Namespace = reader.GetString(2),
                    Pod = reader.GetString(3),
                    Container = reader.GetString(4),
                    LogContent = reader.GetString(5)
                });
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task CleanupSystemLogsAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            // List of system log tables to clean
            var systemLogTables = new[]
            {
                "processors_profile_log",
                "opentelemetry_span_log",
                "query_log",
                "part_log",
                "asynchronous_metric_log",
                "metric_log",
                "query_metric_log",
                "text_log",
                "error_log",
                "trace_log"
            };

            foreach (var tableName in systemLogTables)
            {
                try
                {
                    await using var truncateCommand = connection.CreateCommand();
                    truncateCommand.CommandText = $"TRUNCATE TABLE IF EXISTS system.{tableName}";
                    await truncateCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"Truncated system.{tableName}");
                }
                catch (Exception ex)
                {
                    // Log but continue with other tables
                    Console.WriteLine($"Warning: Could not truncate system.{tableName}: {ex.Message}");
                }
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<TableSizeDto>> GetTableSizesAsync()
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT
                    database,
                    table,
                    formatReadableSize(sum(bytes_on_disk)) AS total_size,
                    sum(bytes_on_disk) AS total_size_bytes
                FROM system.parts
                GROUP BY
                    database,
                    table
                ORDER BY sum(bytes_on_disk) DESC
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            
            var results = new List<TableSizeDto>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new TableSizeDto
                {
                    Database = reader.GetString(0),
                    Table = reader.GetString(1),
                    TotalSize = reader.GetString(2),
                    TotalSizeBytes = reader.GetInt64(3)
                });
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task WriteKubernetesEventAsync(
        string @namespace,
        string eventName,
        string eventUid,
        string involvedObjectKind,
        string involvedObjectName,
        string involvedObjectNamespace,
        string eventType,
        string reason,
        string message,
        string sourceComponent,
        int count,
        DateTime? firstTimestamp,
        DateTime? lastTimestamp,
        string? clusterName = null)
    {
        var effectiveClusterName = clusterName ?? _clusterName;
        if (string.IsNullOrWhiteSpace(effectiveClusterName))
        {
            throw new InvalidOperationException("Cluster name is required for writing events. Provide it as a parameter or set CLUSTER_NAME environment variable.");
        }

        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            // Use ClickHouse format with proper escaping
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var escapedNamespace = @namespace.Replace("'", "''").Replace("\\", "\\\\");
            var escapedEventName = eventName.Replace("'", "''").Replace("\\", "\\\\");
            var escapedEventUid = eventUid.Replace("'", "''").Replace("\\", "\\\\");
            var escapedInvolvedObjectKind = involvedObjectKind.Replace("'", "''").Replace("\\", "\\\\");
            var escapedInvolvedObjectName = involvedObjectName.Replace("'", "''").Replace("\\", "\\\\");
            var escapedInvolvedObjectNamespace = involvedObjectNamespace.Replace("'", "''").Replace("\\", "\\\\");
            var escapedEventType = eventType.Replace("'", "''").Replace("\\", "\\\\");
            var escapedReason = reason.Replace("'", "''").Replace("\\", "\\\\");
            var escapedMessage = message.Replace("'", "''").Replace("\\", "\\\\");
            var escapedSourceComponent = sourceComponent.Replace("'", "''").Replace("\\", "\\\\");
            var firstTimestampValue = firstTimestamp.HasValue
                ? $"'{firstTimestamp.Value:yyyy-MM-dd HH:mm:ss.fff}'"
                : "NULL";
            var lastTimestampValue = lastTimestamp.HasValue
                ? $"'{lastTimestamp.Value:yyyy-MM-dd HH:mm:ss.fff}'"
                : "NULL";

            var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
            
            // ReplacingMergeTree will automatically replace old rows with the same ORDER BY key
            // when last_timestamp is higher, keeping only the latest event per unique combination
            var insertSql = $@"
                INSERT INTO {_database}.{_eventsTableName}
                (timestamp, cluster_name, namespace, event_name, event_uid, involved_object_kind, involved_object_name, involved_object_namespace, event_type, reason, message, source_component, count, first_timestamp, last_timestamp)
                VALUES
                ('{timestamp}', '{escapedClusterName}', '{escapedNamespace}', '{escapedEventName}', '{escapedEventUid}', '{escapedInvolvedObjectKind}', '{escapedInvolvedObjectName}', '{escapedInvolvedObjectNamespace}', '{escapedEventType}', '{escapedReason}', '{escapedMessage}', '{escapedSourceComponent}', {count}, {firstTimestampValue}, {lastTimestampValue})
            ";

            await using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<KubernetesEventDto>> GetKubernetesEventsAsync(
        string? namespaceFilter = null,
        string? involvedObjectKindFilter = null,
        string? involvedObjectNameFilter = null,
        string? eventTypeFilter = null,
        int? minutes = 1440,
        int limit = 1000,
        string? clusterName = null)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var effectiveClusterName = clusterName ?? _clusterName;
            
            // Build WHERE clause
            var whereConditions = new List<string> { "timestamp >= now() - INTERVAL {minutes:Int32} MINUTE" };
            
            if (!string.IsNullOrWhiteSpace(effectiveClusterName))
            {
                whereConditions.Add("cluster_name = {cluster_name:String}");
            }
            if (!string.IsNullOrWhiteSpace(namespaceFilter))
            {
                whereConditions.Add("namespace = {namespace:String}");
            }
            if (!string.IsNullOrWhiteSpace(involvedObjectKindFilter))
            {
                whereConditions.Add("involved_object_kind = {involved_object_kind:String}");
            }
            if (!string.IsNullOrWhiteSpace(involvedObjectNameFilter))
            {
                whereConditions.Add("involved_object_name = {involved_object_name:String}");
            }
            if (!string.IsNullOrWhiteSpace(eventTypeFilter))
            {
                whereConditions.Add("event_type = {event_type:String}");
            }
            
            var whereClause = string.Join(" AND ", whereConditions);

            // Use FINAL to get the latest version of each event (ReplacingMergeTree deduplication)
            var query = $@"
                SELECT 
                    timestamp,
                    cluster_name,
                    namespace,
                    event_name,
                    event_uid,
                    involved_object_kind,
                    involved_object_name,
                    involved_object_namespace,
                    event_type,
                    reason,
                    message,
                    source_component,
                    count,
                    first_timestamp,
                    last_timestamp
                FROM {_database}.{_eventsTableName} FINAL
                WHERE {whereClause}
                ORDER BY timestamp DESC
                LIMIT " + "{limit:Int32}";

            await using var command = connection.CreateCommand();
            command.CommandText = query;
            
            // Add parameters
            command.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "minutes",
                DbType = System.Data.DbType.Int32,
                Value = minutes ?? 1440
            });
            
            command.Parameters.Add(new ClickHouseDbParameter
            {
                ParameterName = "limit",
                DbType = System.Data.DbType.Int32,
                Value = limit
            });
            
            if (!string.IsNullOrWhiteSpace(effectiveClusterName))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "cluster_name",
                    DbType = System.Data.DbType.String,
                    Value = effectiveClusterName
                });
            }
            
            if (!string.IsNullOrWhiteSpace(namespaceFilter))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "namespace",
                    DbType = System.Data.DbType.String,
                    Value = namespaceFilter
                });
            }
            
            if (!string.IsNullOrWhiteSpace(involvedObjectKindFilter))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "involved_object_kind",
                    DbType = System.Data.DbType.String,
                    Value = involvedObjectKindFilter
                });
            }
            
            if (!string.IsNullOrWhiteSpace(involvedObjectNameFilter))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "involved_object_name",
                    DbType = System.Data.DbType.String,
                    Value = involvedObjectNameFilter
                });
            }
            
            if (!string.IsNullOrWhiteSpace(eventTypeFilter))
            {
                command.Parameters.Add(new ClickHouseDbParameter
                {
                    ParameterName = "event_type",
                    DbType = System.Data.DbType.String,
                    Value = eventTypeFilter
                });
            }
            
            var results = new List<KubernetesEventDto>();
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new KubernetesEventDto
                {
                    Timestamp = reader.GetDateTime(0),
                    ClusterName = reader.GetString(1),
                    Namespace = reader.GetString(2),
                    EventName = reader.GetString(3),
                    EventUid = reader.GetString(4),
                    InvolvedObjectKind = reader.GetString(5),
                    InvolvedObjectName = reader.GetString(6),
                    InvolvedObjectNamespace = reader.GetString(7),
                    EventType = reader.GetString(8),
                    Reason = reader.GetString(9),
                    Message = reader.GetString(10),
                    SourceComponent = reader.GetString(11),
                    Count = Convert.ToInt32(reader.GetValue(12)),
                    FirstTimestamp = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
                    LastTimestamp = reader.IsDBNull(14) ? null : reader.GetDateTime(14)
                });
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _connectionSemaphore?.Dispose();
    }
}

