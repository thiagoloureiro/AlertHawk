using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

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

        await using var db = new SqlConnection(_connstring);
        await DeleteOutdatedMonitors(allMonitors, db);

        if (!allMonitors.Any(x => x.IsMaster))
        {
            var currentMonitor = allMonitors.FirstOrDefault(x => x.Hostname == monitorAgent.Hostname);
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
        string sqlAllMonitors = @"SELECT Id, Hostname, TimeStamp, IsMaster, MonitorRegion FROM [MonitorAgent]";
        var result = await db.QueryAsync<MonitorAgent>(sqlAllMonitors, commandType: CommandType.Text);
        return result.ToList();
    }


    public async Task UpsertMonitorAgentTasks(List<MonitorAgentTasks> lstMonitorAgentTasks)
    {
        var lstCurrentMonitorAgentTasks = await GetAllMonitorAgentTasks();

        bool areEqual = lstMonitorAgentTasks.OrderBy(x => x.MonitorId)
            .ThenBy(x => x.MonitorAgentId)
            .SequenceEqual(lstCurrentMonitorAgentTasks.OrderBy(x => x.MonitorId)
                    .ThenBy(x => x.MonitorAgentId),
                new MonitorAgentTasksEqualityComparer());

        if (!areEqual)
        {
            await DeleteAllMonitorAgentTasks(lstMonitorAgentTasks.Select(x => x.MonitorAgentId).ToList());

            string sqlInsertMaster =
                @"INSERT INTO [MonitorAgentTasks] (MonitorId, MonitorAgentId) VALUES (@MonitorId, @MonitorAgentId)";
            await using var db = new SqlConnection(_connstring);

            foreach (var item in lstMonitorAgentTasks)
            {
                await db.ExecuteAsync(sqlInsertMaster,
                    new
                    {
                        MonitorId = item.MonitorId,
                        MonitorAgentId = item.MonitorAgentId
                    }, commandType: CommandType.Text);
            }
        }
    }

    private async Task DeleteAllMonitorAgentTasks(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"DELETE FROM [MonitorAgentTasks] WHERE MonitorAgentId IN @ids";
        await db.ExecuteAsync(sqlAllMonitors, new { ids }, commandType: CommandType.Text);
    }

    public async Task<List<MonitorAgentTasks>> GetAllMonitorAgentTasks()
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"SELECT MonitorId, MonitorAgentId FROM [MonitorAgentTasks]";
        var result = await db.QueryAsync<MonitorAgentTasks>(sqlAllMonitors, commandType: CommandType.Text);
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
            @"UPDATE [MonitorAgent] SET TimeStamp = @TimeStamp, IsMaster = @IsMaster, MonitorRegion = @MonitorRegion WHERE Id = @id";

        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                Id = monitorToUpdate.Id,
                TimeStamp = DateTime.UtcNow,
                IsMaster = monitorToUpdate.IsMaster,
                MonitorRegion = monitorToUpdate.MonitorRegion
            }, commandType: CommandType.Text);
    }

    private static async Task RegisterMonitor(MonitorAgent monitorAgent,
        SqlConnection db, bool isMaster)
    {
        monitorAgent.IsMaster = isMaster;
        // Insert
        string sqlInsertMaster =
            @"INSERT INTO [MonitorAgent] (Hostname, TimeStamp, IsMaster, MonitorRegion) VALUES (@Hostname, @TimeStamp, @IsMaster, @MonitorRegion)";
        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                HostName = monitorAgent.Hostname,
                TimeStamp = monitorAgent.TimeStamp,
                IsMaster = monitorAgent.IsMaster,
                MonitorRegion = monitorAgent.MonitorRegion
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