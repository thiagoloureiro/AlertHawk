using ClickHouse.Client.ADO;

namespace AlertHawk.Metrics;

public class ClickHouseService : IDisposable
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly string _nodeTableName;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    public ClickHouseService(string connectionString, string tableName = "k8s_metrics", string nodeTableName = "k8s_node_metrics")
    {
        _connectionString = connectionString;
        _tableName = tableName;
        _nodeTableName = nodeTableName;
        EnsureTablesExistAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureTablesExistAsync()
    {
        await using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();
        
        // Create pod/container metrics table
        var createTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_tableName}
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
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync();

        // Create node metrics table
        var createNodeTableSql = $@"
            CREATE TABLE IF NOT EXISTS {_nodeTableName}
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
        ";

        await using var nodeCommand = connection.CreateCommand();
        nodeCommand.CommandText = createNodeTableSql;
        await nodeCommand.ExecuteNonQueryAsync();
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

            var insertSql = $@"
                INSERT INTO {_tableName}
                (timestamp, namespace, pod, container, cpu_usage_cores, cpu_limit_cores, memory_usage_bytes)
                VALUES
                ('{timestamp}', '{escapedNamespace}', '{escapedPod}', '{escapedContainer}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuLimitValue}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)})
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

            var insertSql = $@"
                INSERT INTO {_nodeTableName}
                (timestamp, node_name, cpu_usage_cores, cpu_capacity_cores, memory_usage_bytes, memory_capacity_bytes)
                VALUES
                ('{timestamp}', '{escapedNodeName}', {cpuUsageCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {cpuCapacityCores.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, {memoryUsageBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}, {memoryCapacityBytes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)})
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

    public void Dispose()
    {
        _connectionSemaphore?.Dispose();
    }
}

