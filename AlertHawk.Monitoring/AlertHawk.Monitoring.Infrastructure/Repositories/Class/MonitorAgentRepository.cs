using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.Utils;
using AlertHawk.Monitoring.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Npgsql;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorAgentRepository : RepositoryBase, IMonitorAgentRepository
{
    public MonitorAgentRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task ManageMonitorStatus(MonitorAgent monitorAgent)
    {
        var allMonitors = await GetAllMonitorAgents();
        monitorAgent.Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        using var db = CreateConnection();
        await DeleteOutdatedMonitors(allMonitors, db);

        var disableMaster = VariableUtils.GetBoolEnvVariable("DISABLE_MASTER");

        if (!allMonitors.Any(x => x.IsMaster) && !disableMaster)
        {
            var currentMonitor = allMonitors.Find(x => x.Hostname == monitorAgent.Hostname);
            monitorAgent.IsMaster = true;
            GlobalVariables.MasterNode = true;

            // If monitor exists but he is not the master.
            if (currentMonitor != null)
            {
                monitorAgent.Id = currentMonitor.Id;
                GlobalVariables.NodeId = monitorAgent.Id;
                await UpdateExistingMonitor(db, monitorAgent);
                return;
            }
            else
            {
                await RegisterMonitor(monitorAgent, db, true);
                GlobalVariables.NodeId = monitorAgent.Id;
                allMonitors.Add(monitorAgent);
            }
        }

        var monitorToUpdate = allMonitors.FirstOrDefault(x =>
            string.Equals(x.Hostname, monitorAgent.Hostname, StringComparison.CurrentCultureIgnoreCase));

        if (monitorToUpdate != null)
        {
            monitorToUpdate.Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
            monitorToUpdate.MonitorRegion = monitorAgent.MonitorRegion;
            GlobalVariables.NodeId = monitorToUpdate.Id;
            await UpdateExistingMonitor(db, monitorToUpdate);
        }
        else // if monitor don't exists register himself as secondary.
        {
            await RegisterMonitor(monitorAgent, db, false);
        }
    }

    private async Task DeleteOutdatedMonitors(List<MonitorAgent> allMonitors, IDbConnection db)
    {
        var monitorAgents = allMonitors
            .Where(agent => agent.TimeStamp < DateTime.UtcNow.AddMinutes(-1))
            .ToList();

        var agentTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgent", DatabaseProvider);
        var tasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);

        foreach (var monitorAgent in monitorAgents)
        {
            var sqlDelete = $"DELETE FROM {agentTable} WHERE Id = @Id";
            var sqlDeleteFromTasks = $"DELETE FROM {tasksTable} WHERE MonitorAgentId = @Id";

            await db.ExecuteAsync(sqlDelete, new { monitorAgent.Id }, commandType: CommandType.Text);
            await db.ExecuteAsync(sqlDeleteFromTasks, new { monitorAgent.Id }, commandType: CommandType.Text);
            allMonitors.Remove(monitorAgent);
        }
    }

    public async Task<List<MonitorAgent>> GetAllMonitorAgents()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorAgent", DatabaseProvider);
        string sqlAllMonitors = $"SELECT Id, Hostname, TimeStamp, IsMaster, MonitorRegion, Version FROM {tableName}";
        var result = await db.QueryAsyncWithRetry<MonitorAgent>(sqlAllMonitors, commandType: CommandType.Text, commandTimeout: 120);
        return result.ToList();
    }

    public async Task UpsertMonitorAgentTasks(List<MonitorAgentTasks> lstMonitorAgentTasks, int monitorRegion)
    {
        var lstRegion = new List<MonitorRegion> { (MonitorRegion)monitorRegion };
        var lstCurrentMonitorAgentTasks = await GetAllMonitorAgentTasks(lstRegion.Select(x => (int)x).ToList());

        bool areEqual = lstMonitorAgentTasks.OrderBy(x => x.MonitorId)
            .ThenBy(x => x.MonitorAgentId)
            .SequenceEqual(lstCurrentMonitorAgentTasks.OrderBy(x => x.MonitorId)
                    .ThenBy(x => x.MonitorAgentId),
                new MonitorAgentTasksEqualityComparer());

        if (!areEqual)
        {
            await DeleteAllMonitorAgentTasks(lstMonitorAgentTasks.Select(x => x.MonitorId).ToList());

            var tasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);
            
            if (DatabaseProvider == DatabaseProviderType.SqlServer)
            {
                DataTable table = new DataTable();
                table.Columns.Add("MonitorId", typeof(int));
                table.Columns.Add("MonitorAgentId", typeof(int));

                foreach (var item in lstMonitorAgentTasks)
                {
                    table.Rows.Add(item.MonitorId, item.MonitorAgentId);
                }

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Create an instance of SqlBulkCopy for SQL Server
                using var bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(connection);
                bulkCopy.DestinationTableName = tasksTable;
                bulkCopy.ColumnMappings.Add("MonitorId", "MonitorId");
                bulkCopy.ColumnMappings.Add("MonitorAgentId", "MonitorAgentId");
                await bulkCopy.WriteToServerAsync(table);
            }
            else // PostgreSQL
            {
                // For PostgreSQL, use COPY command or batch inserts
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                using var writer = await connection.BeginBinaryImportAsync($"COPY {tasksTable} (MonitorId, MonitorAgentId) FROM STDIN (FORMAT BINARY)");
                foreach (var item in lstMonitorAgentTasks)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(item.MonitorId);
                    await writer.WriteAsync(item.MonitorAgentId);
                }
                await writer.CompleteAsync();
            }
        }
    }

    private async Task DeleteAllMonitorAgentTasks(List<int> ids)
    {
        using var db = CreateConnection();
        var tasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);
        string sqlAllMonitors = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => $"DELETE FROM {tasksTable} WHERE MonitorId IN @ids",
            DatabaseProviderType.PostgreSQL => $"DELETE FROM {tasksTable} WHERE MonitorId = ANY(@ids)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        await db.ExecuteAsync(sqlAllMonitors, new { ids }, commandType: CommandType.Text);
    }

    public async Task<List<MonitorAgentTasks>> GetAllMonitorAgentTasks(List<int> monitorRegions)
    {
        using var db = CreateConnection();
        var tasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sqlAllMonitors = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT MonitorId, MonitorAgentId FROM {tasksTable} MAT " +
                $"INNER JOIN {monitorTable} M on M.Id = MAT.MonitorId WHERE M.MonitorRegion IN @monitorRegions",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT MonitorId, MonitorAgentId FROM {tasksTable} MAT " +
                $"INNER JOIN {monitorTable} M on M.Id = MAT.MonitorId WHERE M.MonitorRegion = ANY(@monitorRegions)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        var result =
            await db.QueryAsync<MonitorAgentTasks>(sqlAllMonitors, new { monitorRegions },
                commandType: CommandType.Text);
        return result.ToList();
    }

    public async Task<List<MonitorAgentTasks>> GetAllMonitorAgentTasksByAgentId(int id)
    {
        using var db = CreateConnection();
        var tasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);
        string sqlAllMonitors = $"SELECT MonitorId, MonitorAgentId FROM {tasksTable} WHERE MonitorAgentId = @id";
        var result = await db.QueryAsync<MonitorAgentTasks>(sqlAllMonitors, new { id }, commandType: CommandType.Text);
        return result.ToList();
    }

    private async Task UpdateExistingMonitor(IDbConnection db,
        MonitorAgent monitorToUpdate)
    {
        var agentTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgent", DatabaseProvider);
        string sqlInsertMaster =
            $"UPDATE {agentTable} SET TimeStamp = @TimeStamp, IsMaster = @IsMaster, MonitorRegion = @MonitorRegion, Version = @Version WHERE Id = @id";

        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                Id = monitorToUpdate.Id,
                TimeStamp = DateTime.UtcNow,
                IsMaster = monitorToUpdate.IsMaster,
                MonitorRegion = monitorToUpdate.MonitorRegion,
                Version = monitorToUpdate.Version
            }, commandType: CommandType.Text);
    }

    private async Task RegisterMonitor(MonitorAgent monitorAgent,
        IDbConnection db, bool isMaster)
    {
        monitorAgent.IsMaster = isMaster;
        var agentTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgent", DatabaseProvider);
        string sqlInsertMaster =
            $"INSERT INTO {agentTable} (Hostname, TimeStamp, IsMaster, MonitorRegion, Version) VALUES (@Hostname, @TimeStamp, @IsMaster, @MonitorRegion, @Version)";
        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                Hostname = monitorAgent.Hostname,
                TimeStamp = monitorAgent.TimeStamp,
                IsMaster = monitorAgent.IsMaster,
                MonitorRegion = monitorAgent.MonitorRegion,
                Version = monitorAgent.Version
            }, commandType: CommandType.Text);
    }

    private class MonitorAgentTasksEqualityComparer : IEqualityComparer<MonitorAgentTasks>
    {
        public bool Equals(MonitorAgentTasks? x, MonitorAgentTasks? y)
        {
            return x?.MonitorId == y?.MonitorId && x?.MonitorAgentId == y?.MonitorAgentId;
        }

        public int GetHashCode(MonitorAgentTasks obj)
        {
            return (obj.MonitorId.GetHashCode() * 397) ^ obj.MonitorAgentId.GetHashCode();
        }
    }
}