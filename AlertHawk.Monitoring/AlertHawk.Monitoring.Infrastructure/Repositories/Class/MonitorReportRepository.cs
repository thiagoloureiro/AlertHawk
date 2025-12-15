using AlertHawk.Monitoring.Domain.Entities.Report;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorReportRepository : RepositoryBase, IMonitorReportRepository
{
    public MonitorReportRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, int hours)
    {
        using var db = CreateConnection();
        var historyTable = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        
        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(hour, -@hours, DATEADD(minute, -1, GETUTCDATE()))",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(hours => @hours) - INTERVAL '1 minute'",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        var inClause = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "IN (SELECT monitorid FROM MonitorGroupItems WHERE MonitorGroupId = @groupId)",
            DatabaseProviderType.PostgreSQL => "= ANY(SELECT monitorid FROM MonitorGroupItems WHERE MonitorGroupId = @groupId)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        var dateDiffExpr = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEDIFF(minute, TimeStamp, NextTimeStamp)",
            DatabaseProviderType.PostgreSQL => "EXTRACT(EPOCH FROM (NextTimeStamp - TimeStamp)) / 60",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        string sqlAllMonitors = $@"WITH StatusChanges AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    LEAD(TimeStamp) OVER (PARTITION BY MonitorId ORDER BY TimeStamp) AS NextTimeStamp
                                FROM
                                    {historyTable}
                                WHERE
                                    MonitorId {inClause} AND
                                    TimeStamp >= {dateFilter}
                            ),
                            Durations AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    NextTimeStamp,
                                    {dateDiffExpr} AS DurationInMinutes
                                FROM
                                    StatusChanges
                                WHERE
                                    NextTimeStamp IS NOT NULL
                            )
                            SELECT
                                m.name AS MonitorName,
                                d.MonitorId,
                                m.Status AS MonitorStatus,
                                SUM(CASE WHEN d.Status = 'true' THEN d.DurationInMinutes ELSE 0 END) AS TotalOnlineMinutes,
                                SUM(CASE WHEN d.Status = 'false' THEN d.DurationInMinutes ELSE 0 END) AS TotalOfflineMinutes
                            FROM
                                Durations d
                            JOIN
                                {monitorTable} m ON d.MonitorId = m.id
                            GROUP BY
                                d.MonitorId, m.name, m.Status;";
        return await db.QueryAsync<MonitorReportUptime>(sqlAllMonitors, new { groupId, hours },
            commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorReportAlerts>> GetMonitorAlerts(int groupId, int hours)
    {
        using var db = CreateConnection();
        var alertTable = Helpers.DatabaseProvider.FormatTableName("MonitorAlert", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        
        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(HOUR, -@hours, DATEADD(minute, -1,GETDATE()))",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(hours => @hours) - INTERVAL '1 minute'",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        var inClause = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "IN (select monitorid from MonitorGroupItems where MonitorGroupId = @groupId)",
            DatabaseProviderType.PostgreSQL => "= ANY(select monitorid from MonitorGroupItems where MonitorGroupId = @groupId)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        string sqlAllMonitors = $@"SELECT m.Name AS MonitorName, ma.MonitorId, COUNT(*) AS NumAlerts
                                    FROM {alertTable} ma
                                    JOIN {monitorTable} m ON ma.MonitorId = m.id
                                    WHERE ma.Status = 'false'
                                    AND ma.MonitorId {inClause}
                                    AND ma.Timestamp >= {dateFilter}
                                    GROUP BY ma.MonitorId, m.Name
                                    ORDER BY m.Name;";
        return await db.QueryAsync<MonitorReportAlerts>(sqlAllMonitors, new { groupId, hours },
            commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorReponseTime>> GetMonitorResponseTime(int groupId, int hours)
    {
        using var db = CreateConnection();
        var historyTable = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        
        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(HOUR, -@hours, GETDATE())",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(hours => @hours)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        var inClause = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "IN (select MonitorId from MonitorGroupItems where MonitorGroupId = @groupId)",
            DatabaseProviderType.PostgreSQL => "= ANY(select MonitorId from MonitorGroupItems where MonitorGroupId = @groupId)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        string sqlAllMonitors = $@"SELECT
                                mh.MonitorId,
                                m.Name AS MonitorName,
                                AVG(mh.ResponseTime) AS AvgResponseTime,
                                MAX(mh.ResponseTime) AS MaxResponseTime,
                                MIN(mh.ResponseTime) AS MinResponseTime
                            FROM
                                {historyTable} mh
                            JOIN
                                {monitorTable} m ON mh.MonitorId = m.Id
                            WHERE
                                mh.TimeStamp >= {dateFilter} AND
                                mh.Status = 1 AND
                                mh.MonitorId {inClause}
                            GROUP BY
                                mh.MonitorId, m.Name
                            ORDER BY
                                m.Name;";
        return await db.QueryAsync<MonitorReponseTime>(sqlAllMonitors, new { groupId, hours }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, DateTime startDate, DateTime endDate)
    {
        using var db = CreateConnection();
        var historyTable = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        
        var inClause = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "IN (SELECT monitorid FROM MonitorGroupItems WHERE MonitorGroupId = @groupId)",
            DatabaseProviderType.PostgreSQL => "= ANY(SELECT monitorid FROM MonitorGroupItems WHERE MonitorGroupId = @groupId)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        var dateDiffExpr = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEDIFF(minute, TimeStamp, NextTimeStamp)",
            DatabaseProviderType.PostgreSQL => "EXTRACT(EPOCH FROM (NextTimeStamp - TimeStamp)) / 60",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        string sqlAllMonitors = $@"WITH StatusChanges AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    LEAD(TimeStamp) OVER (PARTITION BY MonitorId ORDER BY TimeStamp) AS NextTimeStamp
                                FROM
                                    {historyTable}
                                WHERE
                                    MonitorId {inClause} AND
                                    TimeStamp >= @startDate AND
                                    TimeStamp < @endDate
                            ),
                            Durations AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    NextTimeStamp,
                                    {dateDiffExpr} AS DurationInMinutes
                                FROM
                                    StatusChanges
                                WHERE
                                    NextTimeStamp IS NOT NULL
                            )
                            SELECT
                                m.name AS MonitorName,
                                d.MonitorId,
                                SUM(CASE WHEN d.Status = 'true' THEN d.DurationInMinutes ELSE 0 END) AS TotalOnlineMinutes,
                                SUM(CASE WHEN d.Status = 'false' THEN d.DurationInMinutes ELSE 0 END) AS TotalOfflineMinutes
                            FROM
                                Durations d
                            JOIN
                                {monitorTable} m ON d.MonitorId = m.id
                            GROUP BY
                                d.MonitorId, m.name";

        return await db.QueryAsync<MonitorReportUptime>(sqlAllMonitors, new { groupId, startDate, endDate },
            commandType: CommandType.Text);
    }
}