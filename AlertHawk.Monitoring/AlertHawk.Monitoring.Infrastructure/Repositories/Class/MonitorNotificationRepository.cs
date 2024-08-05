using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorNotificationRepository : RepositoryBase, IMonitorNotificationRepository
{
    private readonly string _connstring;

    public MonitorNotificationRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT MonitorId, NotificationId FROM [MonitorNotification] WHERE MonitorId=@id";
        return await db.QueryAsync<MonitorNotification>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task AddMonitorNotification(MonitorNotification monitorNotification)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MonitorNotification] (MonitorId, NotificationId) VALUES (@MonitorId, @NotificationId)";
        await db.ExecuteAsync(sql, new { monitorNotification.MonitorId, monitorNotification.NotificationId },
            commandType: CommandType.Text);
    }

    public async Task RemoveMonitorNotification(MonitorNotification monitorNotification)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"DELETE FROM [MonitorNotification] WHERE MonitorId=@MonitorId AND NotificationId=@NotificationId";
        await db.ExecuteAsync(sql, new { monitorNotification.MonitorId, monitorNotification.NotificationId },
            commandType: CommandType.Text);
    }
}