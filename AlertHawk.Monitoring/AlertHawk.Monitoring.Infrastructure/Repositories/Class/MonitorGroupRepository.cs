using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
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
            @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status FROM [MonitorGroupItems] MGI INNER JOIN [Monitor] M on M.Id = MGI.MonitorId WHERE MGI.MonitorGroupId=@id";

        var lstMonitors = await db.QueryAsync<Monitor>(sqlMonitor, new { id }, commandType: CommandType.Text);
        monitor.Monitors = lstMonitors;
        return monitor;
    }

    public async Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlInsert =
            @"INSERT INTO [monitorGroupItems] (MonitorId, MonitorGroupId) VALUES (@MonitorId, @MonitorGroupId)";
        await db.QueryAsync<MonitorGroup>(sqlInsert,
            new { monitorGroupItems.MonitorId, monitorGroupItems.MonitorGroupId }, commandType: CommandType.Text);
    }

    public async Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"DELETE FROM [MonitorGroupItems] WHERE MonitorId = @MonitorId AND MonitorGroupId = @MonitorGroupId";
        await db.QueryAsync<MonitorGroup>(sql,
            new { monitorGroupItems.MonitorId, monitorGroupItems.MonitorGroupId }, commandType: CommandType.Text);
    }

    public async Task AddMonitorGroup(MonitorGroup monitorGroup)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlInsert =
            @"INSERT INTO [MonitorGroup] (Name) VALUES (@Name)";
        await db.QueryAsync<MonitorGroup>(sqlInsert,
            new { monitorGroup.Name }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorGroup(MonitorGroup monitorGroup)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlUpdate =
            @"UPDATE [MonitorGroup] SET [Name] = @Name WHERE Id = @Id";
        await db.QueryAsync<MonitorGroup>(sqlUpdate,
            new { monitorGroup.Name, monitorGroup.Id }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitorGroup(int id)
    {
        await using var db = new SqlConnection(_connstring);

        string sqlDeleteGroupItems = @"DELETE FROM [MonitorGroupItems] WHERE MonitorGroupId = @id";
        await db.QueryAsync<MonitorGroup>(sqlDeleteGroupItems, new { id }, commandType: CommandType.Text);

        string sqlDeleteGroup = @"DELETE FROM [MonitorGroup] WHERE id = @id";
        await db.QueryAsync<MonitorGroup>(sqlDeleteGroup, new { id }, commandType: CommandType.Text);
    }
}