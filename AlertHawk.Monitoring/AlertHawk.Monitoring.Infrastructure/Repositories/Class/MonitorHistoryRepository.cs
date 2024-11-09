using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorHistoryRepository : RepositoryBase, IMonitorHistoryRepository
{
    private readonly string _connstring;

    public MonitorHistoryRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndDays(int id, int days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @$"SELECT MonitorId, Status, TimeStamp, ResponseTime FROM [MonitorHistory] WHERE MonitorId=@id AND TimeStamp >= DATEADD(day, -@days, GETUTCDATE())  ORDER BY TimeStamp DESC";
        return await db.QueryAsync<MonitorHistory>(sql, new { id, days }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndMetrics(int id, string metric)
    {
        var metricDic = new Dictionary<string, string>()
        {
            {"uptime1Hr",  "DATEADD(HOUR, -1, GETUTCDATE())"},
            {"uptime24Hrs", "DATEADD(HOUR, -24, GETUTCDATE())" },
            {"uptime7Days", "DATEADD(DAY, -7, GETUTCDATE())" },
            {"uptime30Days", "DATEADD(DAY, -30, GETUTCDATE())" },
            {"uptime3Months", "DATEADD(MONTH, -3, GETUTCDATE())"},
            {"uptime6Months", "DATEADD(MONTH, -6, GETUTCDATE())" },
        };

        await using var db = new SqlConnection(_connstring);
        string sql = @$"
                        SELECT MonitorId
                             , Status
                             , TimeStamp
                             , StatusCode
                             , ResponseTime
                             , HttpVersion 
                        FROM [MonitorHistory] 
                        WHERE MonitorId = @id AND TimeStamp >= {metricDic.GetValueOrDefault(metric)}  
                        ORDER BY TimeStamp DESC";
        return await db.QueryAsync<MonitorHistory>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task<MonitorSettings?> GetMonitorHistoryRetention()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT HistoryDaysRetention FROM MonitorSettings";
        return await db.QueryFirstOrDefaultAsync<MonitorSettings>(sql, commandType: CommandType.Text);
    }

    public async Task SetMonitorHistoryRetention(int days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "UPDATE MonitorSettings SET HistoryDaysRetention = @days WHERE 1=1";
        await db.ExecuteAsync(sql, new { days }, commandType: CommandType.Text);
    }

    public async Task SaveMonitorHistory(MonitorHistory monitorHistory)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MonitorHistory] (MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion, ResponseMessage) VALUES (@MonitorId, @Status, @TimeStamp, @StatusCode, @ResponseTime, @HttpVersion, @ResponseMessage)";
        await db.ExecuteAsync(sql,
            new
            {
                monitorHistory.MonitorId,
                monitorHistory.Status,
                monitorHistory.TimeStamp,
                monitorHistory.StatusCode,
                monitorHistory.ResponseTime,
                monitorHistory.HttpVersion,
                monitorHistory.ResponseMessage
            }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT TOP 10000 MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion, ResponseMessage FROM [MonitorHistory] WHERE MonitorId=@id ORDER BY TimeStamp DESC";
        return await db.QueryAsync<MonitorHistory>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitorHistory(int days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"DELETE FROM [MonitorHistory] WHERE TimeStamp < DATEADD(DAY, -@days, GETDATE())";
        await db.QueryAsync<MonitorHistory>(sql, new { days }, commandType: CommandType.Text, commandTimeout: 3600);
    }

    public async Task<long> GetMonitorHistoryCount()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT COUNT(Id) FROM [MonitorHistory]";
        return await db.ExecuteScalarAsync<long>(sql, commandType: CommandType.Text, commandTimeout: 3600);
    }
}