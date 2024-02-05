using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

public class MonitorRepository : RepositoryBase, IMonitorRepository
{
    private readonly string _connstring;

    public MonitorRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<Monitor>> GetMonitorList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status FROM [Monitor]";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>> GetMonitorListByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string whereClause = $"WHERE Id IN ({string.Join(",", ids)})";

        string sql =
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status FROM [Monitor] {whereClause}";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT MonitorId, NotificationId FROM [MonitorNotification] WHERE MonitorId=@id";
        return await db.QueryAsync<MonitorNotification>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorStatus(int id, bool status)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"UPDATE [Monitor] SET Status=@status WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id, status }, commandType: CommandType.Text);
    }

    public async Task SaveMonitorHistory(MonitorHistory monitorHistory)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MonitorHistory] (MonitorId, Status, TimeStamp, StatusCode, ResponseTime) VALUES (@MonitorId, @Status, @TimeStamp, @StatusCode, @ResponseTime)";
        await db.ExecuteAsync(sql,
            new
            {
                monitorHistory.MonitorId, monitorHistory.Status, monitorHistory.TimeStamp, monitorHistory.StatusCode,
                monitorHistory.ResponseTime
            }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT MonitorId, Status, TimeStamp, StatusCode, ResponseTime FROM [MonitorHistory] WHERE MonitorId=@id ORDER BY TimeStamp ASC";
        return await db.QueryAsync<MonitorHistory>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitorHistory(int days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"DELETE FROM [MonitorHistory] WHERE TimeStamp < DATEADD(DAY, -@days, GETDATE())";
        await db.QueryAsync<MonitorHistory>(sql, new { days }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHttp>> GetHttpMonitorByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);

        string whereClause = $"WHERE MonitorId IN ({string.Join(",", ids)})";

        string sql =
            $@"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, UpsideDownMode, MaxRedirects, UrlToCheck, Timeout FROM [MonitorHttp] {whereClause}";

        return await db.QueryAsync<MonitorHttp>(sql, commandType: CommandType.Text);
    }
}