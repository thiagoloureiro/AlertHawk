using ClickHouse.Client.ADO;

namespace AlertHawk.Metrics;

public class ClickHouseService : IDisposable
{
    private readonly string _connectionString;
    private readonly string _database;
    private readonly string _tableName;
    private readonly string _nodeTableName;
    private readonly string _clusterName;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    public ClickHouseService(string connectionString, string clusterName, string tableName = "k8s_metrics", string nodeTableName = "k8s_node_metrics")
    {
        if (string.IsNullOrWhiteSpace(clusterName))
        {
            throw new ArgumentException("Cluster name is required and cannot be empty.", nameof(clusterName));
        }

        _connectionString = connectionString;
        _database = ExtractDatabaseFromConnectionString(connectionString);
        _clusterName = clusterName;
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
                memory_usage_bytes Float64
            )
            ENGINE = MergeTree()
            ORDER BY (timestamp, cluster_name, namespace, pod, container)
            TTL toDateTime(timestamp) + INTERVAL 90 DAY
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync();
        Console.WriteLine($"Table '{_database}.{_tableName}' ensured to exist");

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
                memory_capacity_bytes Float64
            )
            ENGINE = MergeTree()
            ORDER BY (timestamp, cluster_name, node_name)
            TTL toDateTime(timestamp) + INTERVAL 90 DAY
        ";

        await using var nodeCommand = connection.CreateCommand();
        nodeCommand.CommandText = createNodeTableSql;
        await nodeCommand.ExecuteNonQueryAsync();
        Console.WriteLine($"Table '{_database}.{_nodeTableName}' ensured to exist");
    }

    public async Task WriteMetricsAsync(
        string @namespace,
        string pod,
        string container,
        double cpuUsageCores,
        double? cpuLimitCores,
        double memoryUsageBytes)
    {
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

            var escapedClusterName = _clusterName.Replace("'", "''").Replace("\\", "\\\\");
            var insertSql = $@"
                INSERT INTO {_database}.{_tableName}
                (timestamp, cluster_name, namespace, pod, container, cpu_usage_cores, cpu_limit_cores, memory_usage_bytes)
                VALUES
                ('{timestamp}', '{escapedClusterName}', '{escapedNamespace}', '{escapedPod}', '{escapedContainer}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuLimitValue}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)})
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
        double memoryCapacityBytes)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            // Use ClickHouse format with proper escaping
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var escapedNodeName = nodeName.Replace("'", "''").Replace("\\", "\\\\");

            var escapedClusterName = _clusterName.Replace("'", "''").Replace("\\", "\\\\");
            var insertSql = $@"
                INSERT INTO {_database}.{_nodeTableName}
                (timestamp, cluster_name, node_name, cpu_usage_cores, cpu_capacity_cores, memory_usage_bytes, memory_capacity_bytes)
                VALUES
                ('{timestamp}', '{escapedClusterName}', '{escapedNodeName}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuCapacityCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {memoryCapacityBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)})
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

    public async Task<List<PodMetricDto>> GetMetricsByNamespaceAsync(string? namespaceFilter = null, int? hours = 24, int limit = 100)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var whereClause = $"timestamp >= now() - INTERVAL {hours ?? 24} HOUR";
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
                    memory_usage_bytes
                FROM {_database}.{_tableName}
                WHERE {whereClause}
                ORDER BY timestamp DESC
                LIMIT {limit}
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
                    MemoryUsageBytes = reader.GetDouble(7)
                });
            }

            return results;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<List<NodeMetricDto>> GetNodeMetricsAsync(string? nodeNameFilter = null, int? hours = 24, int limit = 100)
    {
        await _connectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            var whereClause = $"timestamp >= now() - INTERVAL {hours ?? 24} HOUR";
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
                    memory_capacity_bytes
                FROM {_database}.{_nodeTableName}
                WHERE {whereClause}
                ORDER BY timestamp DESC
                LIMIT {limit}
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
                    MemoryCapacityBytes = reader.GetDouble(6)
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

public class PodMetricDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double? CpuLimitCores { get; set; }
    public double MemoryUsageBytes { get; set; }
}

public class NodeMetricDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double CpuCapacityCores { get; set; }
    public double MemoryUsageBytes { get; set; }
    public double MemoryCapacityBytes { get; set; }
}

