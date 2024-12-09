using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorAgentRepository : RepositoryBase, IMonitorAgentRepository
{
    private readonly string _connstring;

    public MonitorAgentRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task ManageMonitorStatus(MonitorAgent monitorAgent)
    {
        var allMonitors = await GetAllMonitorAgents();
        monitorAgent.Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        await using var db = new SqlConnection(_connstring);
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

    private static async Task DeleteOutdatedMonitors(List<MonitorAgent> allMonitors, SqlConnection db)
    {
        var monitorAgents = allMonitors
            .Where(agent => agent.TimeStamp < DateTime.UtcNow.AddMinutes(-1))
            .ToList();

        foreach (var monitorAgent in monitorAgents)
        {
            var sqlDelete = @"DELETE FROM [MonitorAgent] WHERE Id = @Id";
            var sqlDeleteFromTasks = @"DELETE FROM [MonitorAgentTasks] WHERE MonitorAgentId = @Id";

            await db.ExecuteAsync(sqlDelete, new { monitorAgent.Id }, commandType: CommandType.Text);
            await db.ExecuteAsync(sqlDeleteFromTasks, new { monitorAgent.Id }, commandType: CommandType.Text);
            allMonitors.Remove(monitorAgent);
        }
    }

    public async Task<List<MonitorAgent>> GetAllMonitorAgents()
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"SELECT Id, Hostname, TimeStamp, IsMaster, MonitorRegion, Version FROM [MonitorAgent]";
        var result = await db.QueryAsync<MonitorAgent>(sqlAllMonitors, commandType: CommandType.Text, commandTimeout: 120);
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

            DataTable table = new DataTable();
            table.Columns.Add("MonitorId", typeof(int));
            table.Columns.Add("MonitorAgentId", typeof(int));

            foreach (var item in lstMonitorAgentTasks)
            {
                table.Rows.Add(item.MonitorId, item.MonitorAgentId);
            }

            await using var connection = new SqlConnection(_connstring);
            await connection.OpenAsync();

            // Create an instance of SqlBulkCopy
            using var bulkCopy = new SqlBulkCopy(connection);
            // Set the destination table name
            bulkCopy.DestinationTableName = "MonitorAgentTasks";

            // Optionally, map the DataTable columns to the database table's columns if they are not in the same order or have different names
            bulkCopy.ColumnMappings.Add("MonitorId", "MonitorId");
            bulkCopy.ColumnMappings.Add("MonitorAgentId", "MonitorAgentId");

            // Perform the bulk copy
            await bulkCopy.WriteToServerAsync(table);
        }
    }

    private async Task DeleteAllMonitorAgentTasks(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"DELETE FROM [MonitorAgentTasks] WHERE MonitorId IN @ids";
        await db.ExecuteAsync(sqlAllMonitors, new { ids }, commandType: CommandType.Text);
    }

    public async Task<List<MonitorAgentTasks>> GetAllMonitorAgentTasks(List<int> monitorRegions)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"SELECT MonitorId, MonitorAgentId FROM [MonitorAgentTasks] MAT
                                INNER JOIN Monitor M on M.Id = MAT.MonitorId WHERE M.MonitorRegion IN @monitorRegions";
        var result =
            await db.QueryAsync<MonitorAgentTasks>(sqlAllMonitors, new { monitorRegions },
                commandType: CommandType.Text);
        return result.ToList();
    }

    public async Task<List<MonitorAgentTasks>> GetAllMonitorAgentTasksByAgentId(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"SELECT MonitorId, MonitorAgentId FROM [MonitorAgentTasks] WHERE MonitorAgentId = @id";
        var result = await db.QueryAsync<MonitorAgentTasks>(sqlAllMonitors, new { id }, commandType: CommandType.Text);
        return result.ToList();
    }

    private static async Task UpdateExistingMonitor(SqlConnection db,
        MonitorAgent monitorToUpdate)
    {
        string sqlInsertMaster =
            @"UPDATE [MonitorAgent] SET TimeStamp = @TimeStamp, IsMaster = @IsMaster, MonitorRegion = @MonitorRegion, Version = @Version WHERE Id = @id";

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

    private static async Task RegisterMonitor(MonitorAgent monitorAgent,
        SqlConnection db, bool isMaster)
    {
        monitorAgent.IsMaster = isMaster;
        // Insert
        string sqlInsertMaster =
            @"INSERT INTO [MonitorAgent] (Hostname, TimeStamp, IsMaster, MonitorRegion, Version) VALUES (@Hostname, @TimeStamp, @IsMaster, @MonitorRegion, @Version)";
        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                HostName = monitorAgent.Hostname,
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