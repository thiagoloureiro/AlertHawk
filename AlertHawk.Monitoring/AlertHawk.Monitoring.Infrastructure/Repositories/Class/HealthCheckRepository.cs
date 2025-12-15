using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class HealthCheckRepository : RepositoryBase, IHealthCheckRepository
{
    public HealthCheckRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            using var db = CreateConnection();
            var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorAgent", DatabaseProvider);
            string sqlAllMonitors = DatabaseProvider switch
            {
                DatabaseProviderType.SqlServer => $"SELECT TOP 1 Id FROM {tableName}",
                DatabaseProviderType.PostgreSQL => $"SELECT Id FROM {tableName} LIMIT 1",
                _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
            };
            var result = await db.QueryFirstOrDefaultAsync<MonitorAgent>(sqlAllMonitors, commandType: CommandType.Text);

            return result != null;
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
            return false;
        }
    }
}