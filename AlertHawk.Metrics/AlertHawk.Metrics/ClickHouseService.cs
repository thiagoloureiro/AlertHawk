using ClickHouse.Client.ADO;

namespace AlertHawk.Metrics;

public class ClickHouseService : IDisposable
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    public ClickHouseService(string connectionString, string tableName = "k8s_metrics")
    {
        _connectionString = connectionString;
        _tableName = tableName;
        EnsureTableExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureTableExistsAsync()
    {
        await using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();
        
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
            TTL timestamp + INTERVAL 90 DAY
        ";

        await using var command = connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync();
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

    public void Dispose()
    {
        _connectionSemaphore?.Dispose();
    }
}

