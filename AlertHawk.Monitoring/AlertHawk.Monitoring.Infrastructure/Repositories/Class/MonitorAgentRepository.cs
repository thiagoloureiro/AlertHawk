using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
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
        // Select
        await using var db = new SqlConnection(_connstring);

        var allMonitors = await GetAllMonitors(db);

        await DeleteOutdatedMonitors(allMonitors, db);

        if (!allMonitors.Any(x => x.IsMaster))
        {
            var currentMonitor = allMonitors.FirstOrDefault(x => x.Hostname == monitorAgent.Hostname);
            monitorAgent.IsMaster = true;
            // If monitor exists but he is not the master.
            if (currentMonitor != null)
            {
                monitorAgent.Id = currentMonitor.Id;
                await UpdateExistingMonitor(db, monitorAgent);
                return;
            }
            else
            {
                await RegisterMonitor(monitorAgent, db, true);

                allMonitors.Add(monitorAgent);
            }
        }

        var monitorToUpdate = allMonitors.FirstOrDefault(x =>
            string.Equals(x.Hostname, monitorAgent.Hostname, StringComparison.CurrentCultureIgnoreCase));

        if (monitorToUpdate != null)
        {
            await UpdateExistingMonitor(db, monitorToUpdate);
        }
        else // if monitor don't exists register himself as secondary.
        {
            await RegisterMonitor(monitorAgent, db, false);
        }
    }

    private static async Task DeleteOutdatedMonitors(List<MonitorAgent> allMonitors, SqlConnection db)
    {
        var monitorsToDelete = allMonitors
            .Where(agent => agent.TimeStamp < DateTime.UtcNow.AddMinutes(-2))
            .ToList();

        foreach (var monitor in monitorsToDelete)
        {
            var sqlDelete = @"DELETE FROM [MonitorAgent] WHERE Id = @Id";
            await db.ExecuteAsync(sqlDelete, new { monitor.Id }, commandType: CommandType.Text);
            allMonitors.Remove(monitor);
        }
    }

    private async Task<List<MonitorAgent>> GetAllMonitors(SqlConnection db)
    {
        await using var dbAllMonitors = new SqlConnection(_connstring);
        string sqlAllMonitors = @"SELECT Id, Hostname, TimeStamp, IsMaster FROM [MonitorAgent]";
        var result = await db.QueryAsync<MonitorAgent>(sqlAllMonitors, commandType: CommandType.Text);
        return result.ToList();
    }

    private static async Task UpdateExistingMonitor(SqlConnection db,
        MonitorAgent monitorToUpdate)
    {
        string sqlInsertMaster =
            @"UPDATE [MonitorAgent] SET TimeStamp = @TimeStamp, IsMaster = @IsMaster WHERE Id = @id";

        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                Id = monitorToUpdate.Id,
                TimeStamp = DateTime.UtcNow,
                IsMaster = monitorToUpdate.IsMaster
            }, commandType: CommandType.Text);
    }

    private static async Task RegisterMonitor(MonitorAgent monitorAgent,
        SqlConnection db, bool isMaster)
    {
        monitorAgent.IsMaster = isMaster;
        // Insert
        string sqlInsertMaster =
            @"INSERT INTO [MonitorAgent] (Hostname, TimeStamp, IsMaster) VALUES (@Hostname, @TimeStamp, @IsMaster)";
        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                HostName = monitorAgent.Hostname,
                TimeStamp = monitorAgent.TimeStamp,
                IsMaster = monitorAgent.IsMaster
            }, commandType: CommandType.Text);
    }
}