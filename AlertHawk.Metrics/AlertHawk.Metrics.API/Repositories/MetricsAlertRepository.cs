using AlertHawk.Metrics.API.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Repositories;

[ExcludeFromCodeCoverage]
public class MetricsAlertRepository : RepositoryBase, IMetricsAlertRepository
{
    private readonly string _connstring;

    public MetricsAlertRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MetricsAlert>> GetMetricsAlerts(string? clusterName, string? nodeName, int? days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql;

        if (!string.IsNullOrWhiteSpace(clusterName) && !string.IsNullOrWhiteSpace(nodeName))
        {
            // Filter by both cluster and node
            sql = @$"SELECT Id, NodeName, ClusterName, TimeStamp, Status, Message
                    FROM MetricsAlert
                    WHERE ClusterName = @clusterName AND NodeName = @nodeName AND TimeStamp >= DATEADD(day, -@days, GETDATE())
                    ORDER BY TimeStamp DESC";
            return await db.QueryAsync<MetricsAlert>(sql, new { clusterName, nodeName, days }, commandType: CommandType.Text);
        }
        else if (!string.IsNullOrWhiteSpace(clusterName))
        {
            // Filter by cluster only
            sql = @$"SELECT Id, NodeName, ClusterName, TimeStamp, Status, Message
                    FROM MetricsAlert
                    WHERE ClusterName = @clusterName AND TimeStamp >= DATEADD(day, -@days, GETDATE())
                    ORDER BY TimeStamp DESC";
            return await db.QueryAsync<MetricsAlert>(sql, new { clusterName, days }, commandType: CommandType.Text);
        }
        else if (!string.IsNullOrWhiteSpace(nodeName))
        {
            // Filter by node only
            sql = @$"SELECT Id, NodeName, ClusterName, TimeStamp, Status, Message
                    FROM MetricsAlert
                    WHERE NodeName = @nodeName AND TimeStamp >= DATEADD(day, -@days, GETDATE())
                    ORDER BY TimeStamp DESC";
            return await db.QueryAsync<MetricsAlert>(sql, new { nodeName, days }, commandType: CommandType.Text);
        }
        else
        {
            // Get all alerts
            sql = @$"SELECT Id, NodeName, ClusterName, TimeStamp, Status, Message
                    FROM MetricsAlert
                    WHERE TimeStamp >= DATEADD(day, -@days, GETDATE())
                    ORDER BY TimeStamp DESC";
            return await db.QueryAsync<MetricsAlert>(sql, new { days }, commandType: CommandType.Text);
        }
    }

    public async Task SaveMetricsAlert(MetricsAlert metricsAlert)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MetricsAlert] (NodeName, ClusterName, TimeStamp, Status, Message) VALUES (@NodeName, @ClusterName, @TimeStamp, @Status, @Message)";
        await db.ExecuteAsync(sql,
            new
            {
                metricsAlert.NodeName,
                metricsAlert.ClusterName,
                metricsAlert.TimeStamp,
                metricsAlert.Status,
                metricsAlert.Message
            }, commandType: CommandType.Text);
    }
}
