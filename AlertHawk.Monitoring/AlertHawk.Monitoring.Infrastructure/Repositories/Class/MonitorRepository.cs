using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

public class MonitorRepository : RepositoryBase, IMonitorRepository
{
    private readonly string _connstring;

    public MonitorRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<Monitor>> GetMonitorList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries FROM [Monitor]";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>> GetMonitorListByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string whereClause = $"WHERE Id IN ({string.Join(",", ids)})";

        string sql = $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries FROM [Monitor] {whereClause}";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHttp>> GetHttpMonitorByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);

        string whereClause = $"WHERE MonitorId IN ({string.Join(",", ids)})";

        string sql =
            $@"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, UpsideDownMode, MaxRedirects, UrlToCheck, Timeout FROM [MonitorHttp] {whereClause}";

        return await db.QueryAsync<MonitorHttp>(sql, commandType: CommandType.Text);
    }
}