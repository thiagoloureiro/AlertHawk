using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorNotificationRepository : RepositoryBase, IMonitorNotificationRepository
{
    public MonitorNotificationRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorNotification", DatabaseProvider);
        string sql = $"SELECT MonitorId, NotificationId FROM {tableName} WHERE MonitorId=@id";
        return await db.QueryAsync<MonitorNotification>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task AddMonitorNotification(MonitorNotification monitorNotification)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorNotification", DatabaseProvider);
        string sql =
            $"INSERT INTO {tableName} (MonitorId, NotificationId) VALUES (@MonitorId, @NotificationId)";
        await db.ExecuteAsync(sql, new { monitorNotification.MonitorId, monitorNotification.NotificationId },
            commandType: CommandType.Text);
    }

    public async Task RemoveMonitorNotification(MonitorNotification monitorNotification)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorNotification", DatabaseProvider);
        string sql =
            $"DELETE FROM {tableName} WHERE MonitorId=@MonitorId AND NotificationId=@NotificationId";
        await db.ExecuteAsync(sql, new { monitorNotification.MonitorId, monitorNotification.NotificationId },
            commandType: CommandType.Text);
    }
}