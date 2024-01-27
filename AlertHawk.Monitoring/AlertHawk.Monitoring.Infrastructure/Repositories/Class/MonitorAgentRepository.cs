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

        var monitorList = allMonitors.Where(x => x.Hostname == monitorAgent.Hostname);

        var monitorAgents = monitorList.ToList();

        await RegisterMasterMonitor(monitorAgent, monitorAgents, db);

        var monitorToUpdate = monitorAgents.FirstOrDefault(x =>
            string.Equals(x.Hostname, monitorAgent.Hostname, StringComparison.CurrentCultureIgnoreCase));

        if (monitorToUpdate != null)
        {
            await UpdateExistingMonitor(monitorAgent, db, monitorToUpdate);
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

    private static async Task RegisterMasterMonitor(MonitorAgent monitorAgent, List<MonitorAgent> monitorAgents,
        SqlConnection db)
    {
        if (!monitorAgents.Any()) // If no monitors, register Master
        {
            monitorAgent.IsMaster = true;
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
}