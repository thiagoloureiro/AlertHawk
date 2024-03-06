using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorRepository : RepositoryBase, IMonitorRepository
{
    private readonly string _connstring;

    public MonitorRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment FROM [Monitor]";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>?> GetMonitorListByMonitorGroupIds(List<int> groupMonitorIds,
        MonitorEnvironment environment)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @$"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment FROM [Monitor] M
            INNER JOIN MonitorGroupItems MGI ON MGI.MonitorId = M.Id WHERE MGI.MonitorGroupId IN @groupMonitorIds AND M.MonitorEnvironment = @environment";
        return await db.QueryAsync<Monitor>(sql, new { groupMonitorIds, environment }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorHttp(MonitorHttp monitorHttp)
    {
        await using var db = new SqlConnection(_connstring);

        string sqlMonitor = @"UPDATE [dbo].[Monitor]
                    SET [Name] = @Name
                    ,[HeartBeatInterval] = @HeartBeatInterval
                    ,[Retries] = @Retries
                    ,[Status] = @Status
                    ,[DaysToExpireCert] = @DaysToExpireCert
                    ,[Paused] = @Paused
                    ,[MonitorRegion] = @MonitorRegion
                    ,[MonitorEnvironment] = @MonitorEnvironment
                    WHERE Id = @MonitorId";
        await db.ExecuteAsync(sqlMonitor,
            new
            {
                monitorHttp.MonitorId,
                monitorHttp.Name,
                monitorHttp.HeartBeatInterval,
                monitorHttp.Retries,
                monitorHttp.Status,
                monitorHttp.DaysToExpireCert,
                monitorHttp.Paused,
                monitorHttp.MonitorRegion,
                monitorHttp.MonitorEnvironment
            }, commandType: CommandType.Text);

        string sqlMonitorHttp =
            @"UPDATE [MonitorHttp] SET CheckCertExpiry = @CheckCertExpiry, IgnoreTlsSsl = @IgnoreTlsSsl, 
            MaxRedirects = @MaxRedirects, UrlToCheck = @UrlToCheck, Timeout = @Timeout, MonitorHttpMethod = @MonitorHttpMethod, 
            Body = @Body, HeadersJson = @HeadersJson WHERE MonitorId = @monitorId";

        await db.ExecuteAsync(sqlMonitorHttp,
            new
            {
                monitorHttp.MonitorId,
                monitorHttp.CheckCertExpiry,
                monitorHttp.IgnoreTlsSsl,
                monitorHttp.MaxRedirects,
                monitorHttp.MonitorHttpMethod,
                monitorHttp.Body,
                monitorHttp.HeadersJson,
                monitorHttp.UrlToCheck,
                monitorHttp.Timeout
            }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitor(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"DELETE FROM [Monitor] WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id }, commandType: CommandType.Text);

        string sqlTasks = @"DELETE FROM [MonitorAgentTasks] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlTasks, new { id }, commandType: CommandType.Text);

        string sqlAlerts = @"DELETE FROM [MonitorAlert] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlAlerts, new { id }, commandType: CommandType.Text);

        string sqlHttp = @"DELETE FROM [MonitorHttp] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlHttp, new { id }, commandType: CommandType.Text);

        string sqlTcp = @"DELETE FROM [MonitorTcp] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlTcp, new { id }, commandType: CommandType.Text);

        string sqlHistory = @"DELETE FROM [MonitorHistory] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlHistory, new { id }, commandType: CommandType.Text);
    }

    public async Task<int> CreateMonitorTcp(MonitorTcp monitorTcp)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlMonitor =
            @"INSERT INTO [Monitor] (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment) 
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment); SELECT CAST(SCOPE_IDENTITY() as int)";
        var id = await db.ExecuteScalarAsync<int>(sqlMonitor,
            new
            {
                monitorTcp.Name,
                monitorTcp.MonitorTypeId,
                monitorTcp.HeartBeatInterval,
                monitorTcp.Retries,
                monitorTcp.Status,
                monitorTcp.DaysToExpireCert,
                monitorTcp.Paused,
                monitorTcp.MonitorRegion,
                monitorTcp.MonitorEnvironment
            }, commandType: CommandType.Text);

        string sqlMonitorTcp =
            @"INSERT INTO [MonitorTcp] (MonitorId, Port, IP, Timeout, LastStatus) VALUES (@MonitorId, @Port, @IP, @Timeout, @LastStatus)";
        await db.ExecuteAsync(sqlMonitorTcp,
            new
            {
                MonitorId = id,
                monitorTcp.Port,
                monitorTcp.IP,
                monitorTcp.Timeout,
                monitorTcp.LastStatus
            }, commandType: CommandType.Text);
        return id;
    }

    public async Task UpdateMonitorTcp(MonitorTcp monitorTcp)
    {
        await using var db = new SqlConnection(_connstring);

        string sqlMonitor = @"UPDATE [dbo].[Monitor]
                    SET [Name] = @Name
                    ,[HeartBeatInterval] = @HeartBeatInterval
                    ,[Retries] = @Retries
                    ,[Status] = @Status
                    ,[DaysToExpireCert] = @DaysToExpireCert
                    ,[Paused] = @Paused
                    ,[MonitorRegion] = @MonitorRegion
                    ,[MonitorEnvironment] = @MonitorEnvironment
                    WHERE Id = @MonitorId";
        await db.ExecuteAsync(sqlMonitor,
            new
            {
                monitorTcp.MonitorId,
                monitorTcp.Name,
                monitorTcp.HeartBeatInterval,
                monitorTcp.Retries,
                monitorTcp.Status,
                monitorTcp.DaysToExpireCert,
                monitorTcp.Paused,
                monitorTcp.MonitorRegion,
                monitorTcp.MonitorEnvironment
            }, commandType: CommandType.Text);

        string sqlMonitorTcp =
            @"UPDATE [MonitorTcp] SET MonitorId = @MonitorId, Port = @Port, IP = @IP, Timeout = @Timeout, LastStatus = @LastStatus WHERE MonitorId = @MonitorId";
        await db.ExecuteAsync(sqlMonitorTcp,
            new
            {
                monitorTcp.MonitorId,
                monitorTcp.Port,
                monitorTcp.IP,
                monitorTcp.Timeout,
                monitorTcp.LastStatus
            }, commandType: CommandType.Text);
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
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment FROM [Monitor] {whereClause}";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<Monitor> GetMonitorById(int id)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment  FROM [Monitor] WHERE Id=@id";
        return await db.QueryFirstOrDefaultAsync<Monitor>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task<MonitorHttp> GetHttpMonitorByMonitorId(int monitorId)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT a.Id, a.Name, a.MonitorTypeId, a.HeartBeatInterval, a.Retries, a.Status, a.DaysToExpireCert, a.Paused, a.MonitorRegion, a.MonitorEnvironment, 
               b.MonitorId, b.CheckCertExpiry, b.IgnoreTlsSsl, b.MaxRedirects, b.UrlToCheck, b.Timeout, b.MonitorHttpMethod, b.Body, b.HeadersJson  
                FROM [Monitor] a inner join
                [MonitorHttp] b on a.Id = b.MonitorId
             WHERE MonitorId = @monitorId";
        return await db.QueryFirstOrDefaultAsync<MonitorHttp>(sql, new { monitorId }, commandType: CommandType.Text);
    }

    public async Task<MonitorTcp> GetTcpMonitorByMonitorId(int monitorId)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT a.Id, a.Name, a.MonitorTypeId, a.HeartBeatInterval, a.Retries, a.Status, a.DaysToExpireCert, a.Paused, a.MonitorRegion, a.MonitorEnvironment, 
               b.MonitorId, b.Port, b.IP, b.Timeout, b.LastStatus                  
                FROM [Monitor] a inner join
                [MonitorTcp] b on a.Id = b.MonitorId
            WHERE MonitorId = @monitorId";
        return await db.QueryFirstOrDefaultAsync<MonitorTcp>(sql, new { monitorId }, commandType: CommandType.Text);
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

    public async Task<int> CreateMonitorHttp(MonitorHttp monitorHttp)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlMonitor =
            @"INSERT INTO [Monitor] (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment) VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment); SELECT CAST(SCOPE_IDENTITY() as int)";
        var id = await db.ExecuteScalarAsync<int>(sqlMonitor,
            new
            {
                monitorHttp.Name,
                monitorHttp.MonitorTypeId,
                monitorHttp.HeartBeatInterval,
                monitorHttp.Retries,
                monitorHttp.Status,
                monitorHttp.DaysToExpireCert,
                monitorHttp.Paused,
                monitorHttp.MonitorRegion,
                monitorHttp.MonitorEnvironment
            }, commandType: CommandType.Text);

        string sqlMonitorHttp =
            @"INSERT INTO [MonitorHttp] (MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson) 
        VALUES (@MonitorId, @CheckCertExpiry, @IgnoreTlsSsl, @MaxRedirects, @UrlToCheck, @Timeout, @MonitorHttpMethod, @Body, @HeadersJson)";
        await db.ExecuteAsync(sqlMonitorHttp,
            new
            {
                MonitorId = id,
                monitorHttp.CheckCertExpiry,
                monitorHttp.IgnoreTlsSsl,
                monitorHttp.MaxRedirects,
                monitorHttp.MonitorHttpMethod,
                monitorHttp.Body,
                monitorHttp.HeadersJson,
                monitorHttp.UrlToCheck,
                monitorHttp.Timeout
            }, commandType: CommandType.Text);
        return id;
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
        await db.QueryAsync<MonitorHistory>(sql, new { days }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHttp>> GetHttpMonitorByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);

        string whereClause = $"WHERE MonitorId IN ({string.Join(",", ids)})";

        string sql =
            $@"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson FROM [MonitorHttp] {whereClause}";

        return await db.QueryAsync<MonitorHttp>(sql, commandType: CommandType.Text);
    }

    public async Task SaveMonitorAlert(MonitorHistory monitorHistory)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MonitorAlert] (MonitorId, TimeStamp, Status, Message, ScreenShotUrl) VALUES (@MonitorId, @TimeStamp, @Status, @Message, @ScreenShotUrl)";
        await db.ExecuteAsync(sql,
            new
            {
                monitorHistory.MonitorId,
                monitorHistory.TimeStamp,
                monitorHistory.Status,
                Message = monitorHistory.ResponseMessage,
                monitorHistory.ScreenShotUrl
            }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorFailureCount>> GetMonitorFailureCount(int days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @$"SELECT MonitorId, COUNT(Status) AS FailureCount
            FROM MonitorAlert
            WHERE Status = 'false' AND TimeStamp >= DATEADD(DAY, -{days}, GETDATE())
            GROUP BY MonitorId;";

        return await db.QueryAsync<MonitorFailureCount>(sql, new { days }, commandType: CommandType.Text);
    }
}