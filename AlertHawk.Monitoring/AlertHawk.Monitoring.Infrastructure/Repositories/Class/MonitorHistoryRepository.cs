using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorHistoryRepository : RepositoryBase, IMonitorHistoryRepository
{
    public MonitorHistoryRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndDays(int id, int days)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(day, -@days, GETUTCDATE())",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(days => @days)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        string sql =
            $"SELECT MonitorId, Status, TimeStamp, ResponseTime FROM {tableName} WHERE MonitorId = @id AND TimeStamp >= {dateFilter} ORDER BY TimeStamp DESC;";
        return await db.QueryAsync<MonitorHistory>(sql, new { id, days }, commandType: CommandType.Text,
            commandTimeout: 120);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndHours(int id, int hours)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(hour, -@hours, GETUTCDATE())",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(hours => @hours)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        string sql =
            $"SELECT MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion FROM {tableName} WHERE MonitorId=@id AND TimeStamp >= {dateFilter} ORDER BY TimeStamp DESC";
        return await db.QueryAsync<MonitorHistory>(sql, new { id, hours }, commandType: CommandType.Text);
    }

    public async Task<MonitorSettings?> GetMonitorHistoryRetention()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorSettings", DatabaseProvider);
        string sql = $"SELECT HistoryDaysRetention FROM {tableName}";
        return await db.QueryFirstOrDefaultAsync<MonitorSettings>(sql, commandType: CommandType.Text);
    }

    public async Task SetMonitorHistoryRetention(int days)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorSettings", DatabaseProvider);
        string sql = $"UPDATE {tableName} SET HistoryDaysRetention = @days";
        await db.ExecuteAsync(sql, new { days }, commandType: CommandType.Text);
    }

    public async Task<MonitorHttpHeaders> GetMonitorSecurityHeaders(int id)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHttpHeaders", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => $"SELECT TOP 1 MonitorId, CacheControl, StrictTransportSecurity, PermissionsPolicy, XFrameOptions, XContentTypeOptions, ReferrerPolicy, ContentSecurityPolicy FROM {tableName} WHERE MonitorId=@id",
            DatabaseProviderType.PostgreSQL => $"SELECT MonitorId, CacheControl, StrictTransportSecurity, PermissionsPolicy, XFrameOptions, XContentTypeOptions, ReferrerPolicy, ContentSecurityPolicy FROM {tableName} WHERE MonitorId=@id LIMIT 1",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        return await db.QueryFirstOrDefaultAsync<MonitorHttpHeaders>(sql, new { id }, commandType: CommandType.Text);
    }
    
    public async Task<MonitorHttpHeaders> SaveMonitorSecurityHeaders(MonitorHttpHeaders monitorHttpHeaders)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHttpHeaders", DatabaseProvider);
        string sqlCheck = $"SELECT COUNT(1) FROM {tableName} WHERE MonitorId=@MonitorId";
        int count = await db.ExecuteScalarAsync<int>(sqlCheck, new { monitorHttpHeaders.MonitorId }, commandType: CommandType.Text);

        if (count > 0)
        {
            string sqlUpdate = $"UPDATE {tableName} SET CacheControl=@CacheControl, StrictTransportSecurity=@StrictTransportSecurity, PermissionsPolicy=@PermissionsPolicy, XFrameOptions=@XFrameOptions, XContentTypeOptions=@XContentTypeOptions, ReferrerPolicy=@ReferrerPolicy, ContentSecurityPolicy=@ContentSecurityPolicy WHERE MonitorId=@MonitorId";
            await db.ExecuteAsync(sqlUpdate,
                new
                {
                    monitorHttpHeaders.CacheControl,
                    monitorHttpHeaders.StrictTransportSecurity,
                    monitorHttpHeaders.PermissionsPolicy,
                    monitorHttpHeaders.XFrameOptions,
                    monitorHttpHeaders.XContentTypeOptions,
                    monitorHttpHeaders.ReferrerPolicy,
                    monitorHttpHeaders.ContentSecurityPolicy,
                    monitorHttpHeaders.MonitorId
                }, commandType: CommandType.Text);
        }
        else
        {
            string sqlInsert = $"INSERT INTO {tableName} (MonitorId, CacheControl, StrictTransportSecurity, PermissionsPolicy, XFrameOptions, XContentTypeOptions, ReferrerPolicy, ContentSecurityPolicy) VALUES (@MonitorId, @CacheControl, @StrictTransportSecurity, @PermissionsPolicy, @XFrameOptions, @XContentTypeOptions, @ReferrerPolicy, @ContentSecurityPolicy)";
            await db.ExecuteAsync(sqlInsert,
                new
                {
                    monitorHttpHeaders.MonitorId,
                    monitorHttpHeaders.CacheControl,
                    monitorHttpHeaders.StrictTransportSecurity,
                    monitorHttpHeaders.PermissionsPolicy,
                    monitorHttpHeaders.XFrameOptions,
                    monitorHttpHeaders.XContentTypeOptions,
                    monitorHttpHeaders.ReferrerPolicy,
                    monitorHttpHeaders.ContentSecurityPolicy
                }, commandType: CommandType.Text);
        }

        return monitorHttpHeaders;
    }

    public async Task SaveMonitorHistory(MonitorHistory monitorHistory)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        string sql =
            $"INSERT INTO {tableName} (MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion, ResponseMessage) VALUES (@MonitorId, @Status, @TimeStamp, @StatusCode, @ResponseTime, @HttpVersion, @ResponseMessage)";
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
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT TOP 10000 MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion, ResponseMessage FROM {tableName} WHERE MonitorId=@id ORDER BY TimeStamp DESC",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT MonitorId, Status, TimeStamp, StatusCode, ResponseTime, HttpVersion, ResponseMessage FROM {tableName} WHERE MonitorId=@id ORDER BY TimeStamp DESC LIMIT 10000",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        return await db.QueryAsync<MonitorHistory>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitorHistory(int days)
    {
        // if days == 0 we truncate
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);

        if (days == 0)
        {
            string sqlTruncate = $"TRUNCATE TABLE {tableName}";
            await db.ExecuteAsync(sqlTruncate, commandType: CommandType.Text);
            return;
        }

        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(DAY, -@days, GETDATE())",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(days => @days)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        string sql = $"DELETE FROM {tableName} WHERE TimeStamp < {dateFilter}";
        await db.QueryAsync<MonitorHistory>(sql, new { days }, commandType: CommandType.Text, commandTimeout: 3600);
    }

    public async Task<long> GetMonitorHistoryCount()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        string sql = $"SELECT COUNT(Id) FROM {tableName}";
        return await db.ExecuteScalarAsync<long>(sql, commandType: CommandType.Text, commandTimeout: 3600);
    }
}