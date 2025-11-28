using System.Diagnostics.CodeAnalysis;
using ClickHouse.Client.ADO;
using AlertHawk.Metrics.API.Models;

namespace AlertHawk.Metrics.API.Services;

[ExcludeFromCodeCoverage]
public class ClickHouseService : IClickHouseService, IDisposable
{
    private readonly string _connectionString;
    private readonly string _database;
    private readonly string _tableName;
    private readonly string _nodeTableName;
    private readonly string _clusterName;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    public ClickHouseService(string connectionString, string? clusterName = null, string tableName = "k8s_metrics", string nodeTableName = "k8s_node_metrics")
    {
        _connectionString = connectionString;
        _database = ExtractDatabaseFromConnectionString(connectionString);
        _clusterName = clusterName ?? string.Empty;
        _tableName = tableName;
        _nodeTableName = nodeTableName;
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
                node_name Nullable(String)
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

        // Create node metrics table
        var createNodeTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_database}.{_nodeTableName}
            (
                timestamp DateTime64(3) DEFAULT now64(),
                cluster_name String,
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
                has_pid_pressure Nullable(UInt8)
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
    }

    public async Task WriteMetricsAsync(
        string @namespace,
        string pod,
        string container,
        double cpuUsageCores,
        double? cpuLimitCores,
        double memoryUsageBytes,
        string? clusterName = null,
        string? nodeName = null)
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

            var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
            var insertSql = $@"
                INSERT INTO {_database}.{_tableName}
                (timestamp, cluster_name, namespace, pod, container, cpu_usage_cores, cpu_limit_cores, memory_usage_bytes, node_name)
                VALUES
                ('{timestamp}', '{escapedClusterName}', '{escapedNamespace}', '{escapedPod}', '{escapedContainer}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuLimitValue}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {nodeNameValue})
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
        string? kubernetesVersion = null,
        string? cloudProvider = null,
        bool? isReady = null,
        bool? hasMemoryPressure = null,
        bool? hasDiskPressure = null,
        bool? hasPidPressure = null)
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

            var escapedClusterName = effectiveClusterName.Replace("'", "''").Replace("\\", "\\\\");
            var insertSql = $@"
                INSERT INTO {_database}.{_nodeTableName}
                (timestamp, cluster_name, node_name, cpu_usage_cores, cpu_capacity_cores, memory_usage_bytes, memory_capacity_bytes, kubernetes_version, cloud_provider, is_ready, has_memory_pressure, has_disk_pressure, has_pid_pressure)
                VALUES
                ('{timestamp}', '{escapedClusterName}', '{escapedNodeName}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuCapacityCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {memoryCapacityBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {kubernetesVersionValue}, {cloudProviderValue}, {isReadyValue}, {hasMemoryPressureValue}, {hasDiskPressureValue}, {hasPidPressureValue})
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

    public async Task<List<PodMetricDto>> GetMetricsByNamespaceAsync(string? namespaceFilter = null, int? hours = 24, int limit = 100, string? clusterName = null)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var effectiveClusterName = clusterName ?? _clusterName;
            var whereClause = $"timestamp >= now() - INTERVAL {hours ?? 24} HOUR";
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

            var query = $@"
                SELECT 
                    timestamp,
                    cluster_name,
                    namespace,
                    pod,
                    container,
                    cpu_usage_cores,
                    cpu_limit_cores,
                    memory_usage_bytes,
                    node_name
                FROM {_database}.{_tableName}
                WHERE {whereClause}
                ORDER BY timestamp DESC
            ";

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
                    NodeName = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<NodeMetricDto>> GetNodeMetricsAsync(string? nodeNameFilter = null, int? hours = 24, int limit = 100, string? clusterName = null)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var effectiveClusterName = clusterName ?? _clusterName;
            var whereClause = $"timestamp >= now() - INTERVAL {hours ?? 24} HOUR";
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

            var query = $@"
                SELECT 
                    timestamp,
                    cluster_name,
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
                    has_pid_pressure
                FROM {_database}.{_nodeTableName}
                WHERE {whereClause}
                ORDER BY timestamp DESC
            ";

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
                    NodeName = reader.GetString(2),
                    CpuUsageCores = reader.GetDouble(3),
                    CpuCapacityCores = reader.GetDouble(4),
                    MemoryUsageBytes = reader.GetDouble(5),
                    MemoryCapacityBytes = reader.GetDouble(6),
                    KubernetesVersion = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CloudProvider = reader.IsDBNull(8) ? null : reader.GetString(8),
                    IsReady = reader.IsDBNull(9) ? null : (reader.GetByte(9) == 1),
                    HasMemoryPressure = reader.IsDBNull(10) ? null : (reader.GetByte(10) == 1),
                    HasDiskPressure = reader.IsDBNull(11) ? null : (reader.GetByte(11) == 1),
                    HasPidPressure = reader.IsDBNull(12) ? null : (reader.GetByte(12) == 1)
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
                // Truncate both tables
                await using var truncateMetricsCommand = connection.CreateCommand();
                truncateMetricsCommand.CommandText = $"TRUNCATE TABLE IF EXISTS {_database}.{_tableName}";
                await truncateMetricsCommand.ExecuteNonQueryAsync();

                await using var truncateNodeMetricsCommand = connection.CreateCommand();
                truncateNodeMetricsCommand.CommandText = $"TRUNCATE TABLE IF EXISTS {_database}.{_nodeTableName}";
                await truncateNodeMetricsCommand.ExecuteNonQueryAsync();
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
            }
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

