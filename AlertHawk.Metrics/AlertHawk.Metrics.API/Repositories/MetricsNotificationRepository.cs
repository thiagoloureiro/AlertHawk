using AlertHawk.Metrics.API.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Repositories;

[ExcludeFromCodeCoverage]
public class MetricsNotificationRepository : RepositoryBase, IMetricsNotificationRepository
{
    private readonly string _connstring;

    public MetricsNotificationRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MetricsNotification>> GetMetricsNotifications(string clusterName)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT ClusterName, NotificationId FROM [MetricsNotification] WHERE ClusterName=@clusterName";
        return await db.QueryAsync<MetricsNotification>(sql, new { clusterName }, commandType: CommandType.Text);
    }

    public async Task AddMetricsNotification(MetricsNotification metricsNotification)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MetricsNotification] (ClusterName, NotificationId) VALUES (@ClusterName, @NotificationId)";
        await db.ExecuteAsync(sql, new { metricsNotification.ClusterName, metricsNotification.NotificationId },
            commandType: CommandType.Text);
    }

    public async Task RemoveMetricsNotification(MetricsNotification metricsNotification)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"DELETE FROM [MetricsNotification] WHERE ClusterName=@ClusterName AND NotificationId=@NotificationId";
        await db.ExecuteAsync(sql, new { metricsNotification.ClusterName, metricsNotification.NotificationId },
            commandType: CommandType.Text);
    }
}
