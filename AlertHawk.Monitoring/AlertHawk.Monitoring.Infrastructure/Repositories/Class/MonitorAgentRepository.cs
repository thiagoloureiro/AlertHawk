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

        if (!allMonitors.Any(x => x.IsMaster))
        {
            await RegisterMonitor(monitorAgent, db, true);
            monitorAgent.IsMaster = true;
            allMonitors.Add(monitorAgent);
        }
        
        var monitorToUpdate = allMonitors.FirstOrDefault(x =>
            string.Equals(x.Hostname, monitorAgent.Hostname, StringComparison.CurrentCultureIgnoreCase));

        if (monitorToUpdate != null)
        {
            await UpdateExistingMonitor(monitorAgent, db, monitorToUpdate);
        }
        else // if monitor don't exists register himself as secondary.
        {
            await RegisterMonitor(monitorAgent, db, false);
        }

        var monitorsToDelete = allMonitors
            .Where(agent => agent.TimeStamp < DateTime.UtcNow.AddMinutes(-2))
            .ToList();

        foreach (var monitor in monitorsToDelete)
        {
            var sqlDelete = @"DELETE FROM [MonitorAgent] WHERE Id = @Id";
            await db.ExecuteAsync(sqlDelete, new { monitor.Id }, commandType: CommandType.Text);
        }
    }

    private async Task<List<MonitorAgent>> GetAllMonitors(SqlConnection db)
    {
        await using var dbAllMonitors = new SqlConnection(_connstring);
        string sqlAllMonitors = @"SELECT Id, Hostname, TimeStamp, IsMaster FROM [MonitorAgent]";
        var result = await db.QueryAsync<MonitorAgent>(sqlAllMonitors, commandType: CommandType.Text);
        return result.ToList();
    }

    private static async Task UpdateExistingMonitor(MonitorAgent monitorAgent, SqlConnection db,
        MonitorAgent monitorToUpdate)
    {
        string sqlInsertMaster =
            @"UPDATE [MonitorAgent] SET TimeStamp = @TimeStamp WHERE Id = @id";

        await db.ExecuteAsync(sqlInsertMaster,
            new
            {
                Id = monitorToUpdate.Id,
                TimeStamp = monitorAgent.TimeStamp
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