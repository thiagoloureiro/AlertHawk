using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorTypeRepository : RepositoryBase, IMonitorTypeRepository
{
    public MonitorTypeRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<IEnumerable<MonitorType>> GetMonitorType()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorType", DatabaseProvider);
        string sql = $"SELECT Id, Name FROM {tableName}";
        return await db.QueryAsync<MonitorType>(sql, commandType: CommandType.Text);
    }
}