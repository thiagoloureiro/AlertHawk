using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorGroupRepository : RepositoryBase, IMonitorGroupRepository
{
    private readonly IMonitorRepository _monitorRepository;

    public MonitorGroupRepository(IConfiguration configuration, IMonitorRepository monitorRepository) : base(
        configuration)
    {
        _monitorRepository = monitorRepository;
    }

    public async Task<IEnumerable<MonitorGroup>?> GetMonitorGroupList()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);
        string sql = $"SELECT Id, Name FROM {tableName}";
        var monitorGroupList = await db.QueryAsync<MonitorGroup>(sql, commandType: CommandType.Text);
        
        return monitorGroupList;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupListByEnvironment(MonitorEnvironment environment)
    {
        using var db = CreateConnection();

        var monitorList = await _monitorRepository.GetMonitorList(environment);

        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        string sqlMonitorGroupItems = $"SELECT MonitorId, MonitorGroupId FROM {groupItemsTable}";
        var groupItems = await db.QueryAsync<MonitorGroupItems>(sqlMonitorGroupItems, commandType: CommandType.Text);

        var groupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);
        string sql = $"SELECT Id, Name FROM {groupTable}";
        var monitorGroupList = await db.QueryAsync<MonitorGroup>(sql, commandType: CommandType.Text);

        foreach (var monitorGroup in monitorGroupList)
        {
            var monitors = groupItems.Where(x => x.MonitorGroupId == monitorGroup.Id).Select(x => x.MonitorId);
            monitorGroup.Monitors = monitorList.Where(x => monitors.Contains(x.Id));
        }

        return monitorGroupList;
    }

    public async Task<MonitorGroup> GetMonitorGroupById(int id)
    {
        using var db = CreateConnection();
        var groupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);
        string sql = $"SELECT Id, Name FROM {groupTable} WHERE id=@id";
        var monitor = await db.QueryFirstOrDefaultAsync<MonitorGroup>(sql, new { id }, commandType: CommandType.Text);

        if (monitor != null)
        {
            var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
            string sqlMonitor =
                $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status FROM {groupItemsTable} MGI INNER JOIN {monitorTable} M on M.Id = MGI.MonitorId WHERE MGI.MonitorGroupId=@id";

            var lstMonitors = await db.QueryAsync<Monitor>(sqlMonitor, new { id }, commandType: CommandType.Text);
            monitor.Monitors = lstMonitors;
            return monitor;
        }

        return new MonitorGroup
        {
            Name = null
        };
    }

    public async Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems)
    {
        using var db = CreateConnection();
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);

        string sqlRemove = $"DELETE FROM {groupItemsTable} WHERE MonitorId = @MonitorId";
        await db.QueryAsync(sqlRemove, new { monitorGroupItems.MonitorId }, commandType: CommandType.Text);

        string sqlInsert =
            $"INSERT INTO {groupItemsTable} (MonitorId, MonitorGroupId) VALUES (@MonitorId, @MonitorGroupId)";
        await db.QueryAsync<MonitorGroup>(sqlInsert,
            new { monitorGroupItems.MonitorId, monitorGroupItems.MonitorGroupId }, commandType: CommandType.Text);
    }

    public async Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems)
    {
        using var db = CreateConnection();
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        string sql =
            $"DELETE FROM {groupItemsTable} WHERE MonitorId = @MonitorId AND MonitorGroupId = @MonitorGroupId";
        await db.QueryAsync<MonitorGroup>(sql,
            new { monitorGroupItems.MonitorId, monitorGroupItems.MonitorGroupId }, commandType: CommandType.Text);
    }

    public async Task<int> AddMonitorGroup(MonitorGroup monitorGroup)
    {
        using var db = CreateConnection();
        var groupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);
        string sqlInsert = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"INSERT INTO {groupTable} (Name) VALUES (@Name); SELECT CAST(SCOPE_IDENTITY() as int)",
            DatabaseProviderType.PostgreSQL =>
                $"INSERT INTO {groupTable} (Name) VALUES (@Name) RETURNING Id",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        return DatabaseProvider == DatabaseProviderType.PostgreSQL
            ? await db.QuerySingleAsync<int>(sqlInsert, new { monitorGroup.Name }, commandType: CommandType.Text)
            : await db.ExecuteScalarAsync<int>(sqlInsert, new { monitorGroup.Name }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorGroup(MonitorGroup monitorGroup)
    {
        using var db = CreateConnection();
        var groupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);
        string sqlUpdate =
            $"UPDATE {groupTable} SET [Name] = @Name WHERE Id = @Id";
        await db.QueryAsync<MonitorGroup>(sqlUpdate,
            new { monitorGroup.Name, monitorGroup.Id }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitorGroup(int id)
    {
        using var db = CreateConnection();
        var groupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);

        string sqlDeleteGroup = $"DELETE FROM {groupTable} WHERE id = @id";
        await db.QueryAsync<MonitorGroup>(sqlDeleteGroup, new { id }, commandType: CommandType.Text);
    }

    public async Task<MonitorGroup?> GetMonitorGroupByName(string monitorGroupName)
    {
        using var db = CreateConnection();
        var groupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);
        string sql = $"SELECT Id, Name FROM {groupTable} WHERE Name=@monitorGroupName";
        return await db.QueryFirstOrDefaultAsync<MonitorGroup>(sql, new { monitorGroupName }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>?> GetMonitorListByGroupId(int monitorGroupId)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        string sql = $"select Id, Name, MonitorTypeId, HeartBeatInterval, Retries, " +
            $"Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag from {monitorTable} M " +
            $"inner join {groupItemsTable} MGI on MGI.MonitorId = M.ID " +
            $"where MGI.MonitorGroupId = @monitorGroupId";
        return await db.QueryAsync<Monitor>(sql, new { monitorGroupId }, commandType: CommandType.Text);
    }

    public async Task<int> GetMonitorGroupIdByMonitorId(int id)
    {
        using var db = CreateConnection();
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        string sql = $"SELECT MonitorGroupId FROM {groupItemsTable} WHERE MonitorId=@id";
        return await db.ExecuteScalarAsync<int>(sql, new { id }, commandType: CommandType.Text);
    }
}