using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
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
            @$"SELECT MonitorId, Status, TimeStamp, ResponseTime FROM [MonitorHistory] WHERE MonitorId = @id AND TimeStamp >= DATEADD(day, -@days, GETUTCDATE()) ORDER BY TimeStamp DESC;";
        return await db.QueryAsync<MonitorHistory>(sql, new { id, days }, commandType: CommandType.Text,
            commandTimeout: 120);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndHours(int id, int hours)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @$"SELECT MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion FROM [MonitorHistory] WHERE MonitorId=@id AND TimeStamp >= DATEADD(hour, -@hours, GETUTCDATE())  ORDER BY TimeStamp DESC";
        return await db.QueryAsync<MonitorHistory>(sql, new { id, hours }, commandType: CommandType.Text);
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
        // if days == 0 we truncate
        await using var db = new SqlConnection(_connstring);

        if (days == 0)
        {
            string sqlTruncate = "TRUNCATE TABLE [MonitorHistory]";
            await db.ExecuteAsync(sqlTruncate, commandType: CommandType.Text);
            return;
        }

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