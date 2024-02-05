using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

public class MonitorGroupRepository : RepositoryBase, IMonitorGroupRepository
{
    private readonly string _connstring;

    public MonitorGroupRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT Id, Name FROM [MonitorGroup]";
        return await db.QueryAsync<MonitorGroup>(sql, commandType: CommandType.Text);
    }

    public async Task<MonitorGroup> GetMonitorGroupById(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT Id, Name FROM [MonitorGroup] WHERE id=@id";
        var monitor = await db.QueryFirstOrDefaultAsync<MonitorGroup>(sql, new { id }, commandType: CommandType.Text);

        string sqlMonitor =
            @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status FROM [MonitorGroupItems] MGI WHERE
                                                                                                INNER JOIN [Monitor] M on M.Id = MGI.MonitorId
                                                                                                WHERE MGI.MonitorGroupId=@id";

        var lstMonitors = await db.QueryAsync<Monitor>(sqlMonitor, new { id }, commandType: CommandType.Text);
        monitor.Monitors = lstMonitors;
        return monitor;
    }
}