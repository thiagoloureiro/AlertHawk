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
        string sql =
            @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion FROM [Monitor]";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorTcp>> GetTcpMonitorByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);

        string whereClause = $"WHERE MonitorId IN ({string.Join(",", ids)})";

        string sql =
            $@"SELECT MonitorId, Port, IP, Timeout, LastStatus  FROM [MonitorTcp] {whereClause}";

        return await db.QueryAsync<MonitorTcp>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>> GetMonitorListByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string whereClause = $"WHERE Id IN ({string.Join(",", ids)})";

        string sql =
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion FROM [Monitor] {whereClause}";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<Monitor> GetMonitorById(int id)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion  FROM [Monitor] WHERE Id=@id";
        return await db.QueryFirstOrDefaultAsync<Monitor>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT MonitorId, NotificationId FROM [MonitorNotification] WHERE MonitorId=@id";
        return await db.QueryAsync<MonitorNotification>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorStatus(int id, bool status, int daysToExpireCert)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"UPDATE [Monitor] SET Status=@status, DaysToExpireCert=@daysToExpireCert WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id, status, daysToExpireCert }, commandType: CommandType.Text);
    }

    public async Task PauseMonitor(int id, bool paused)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"UPDATE [Monitor] SET paused=@paused WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id, paused }, commandType: CommandType.Text);
    }

    public async Task CreateMonitor(MonitorHttp monitorHttp)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlMonitor =
            @"INSERT INTO [Monitor] (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion) VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion); SELECT CAST(SCOPE_IDENTITY() as int)";
        var id = await db.ExecuteScalarAsync<int>(sqlMonitor,
            new
            {
                monitorHttp.Name, monitorHttp.MonitorTypeId, monitorHttp.HeartBeatInterval, monitorHttp.Retries,
                monitorHttp.Status,
                monitorHttp.DaysToExpireCert, monitorHttp.Paused, monitorHttp.MonitorRegion
            }, commandType: CommandType.Text);

        string sqlMonitorHttp =
            @"INSERT INTO [MonitorHttp] (MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson) VALUES (@MonitorId, @CheckCertExpiry, @IgnoreTlsSsl, @MaxRedirects, @UrlToCheck, @Timeout, @MonitorHttpMethod, @Body, @HeadersJson)";
        await db.ExecuteAsync(sqlMonitorHttp,
            new
            {
                MonitorId = id, monitorHttp.CheckCertExpiry, monitorHttp.IgnoreTlsSsl,
                monitorHttp.MaxRedirects,
                monitorHttp.MonitorHttpMethod, monitorHttp.Body, monitorHttp.HeadersJson
            }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id, int days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT TOP 10000 MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion FROM [MonitorHistory] WHERE MonitorId=@id AND TimeStamp >= DATEADD(day, -30, GETUTCDATE())  ORDER BY TimeStamp DESC";
        return await db.QueryAsync<MonitorHistory>(sql, new { id, days }, commandType: CommandType.Text);
    }

    public async Task SaveMonitorHistory(MonitorHistory monitorHistory)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MonitorHistory] (MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion) VALUES (@MonitorId, @Status, @TimeStamp, @StatusCode, @ResponseTime, @HttpVersion)";
        await db.ExecuteAsync(sql,
            new
            {
                monitorHistory.MonitorId, monitorHistory.Status, monitorHistory.TimeStamp, monitorHistory.StatusCode,
                monitorHistory.ResponseTime, monitorHistory.HttpVersion
            }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT TOP 10000 MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion FROM [MonitorHistory] WHERE MonitorId=@id ORDER BY TimeStamp DESC";
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
            $@"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, LastStatus, ResponseTime, MonitorHttpMethod, Body, HeadersJson FROM [MonitorHttp] {whereClause}";

        return await db.QueryAsync<MonitorHttp>(sql, commandType: CommandType.Text);
    }
}