using AlertHawk.Monitoring.Domain.Entities.Report;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorReportRepository : RepositoryBase, IMonitorReportRepository
{
    private readonly string _connstring;

    public MonitorReportRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, int hours)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"WITH StatusChanges AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    LEAD(TimeStamp) OVER (PARTITION BY MonitorId ORDER BY TimeStamp) AS NextTimeStamp
                                FROM
                                    MonitorHistory
                                WHERE
                                    MonitorId IN (select monitorid from MonitorGroupItems where MonitorGroupId = @groupId) AND
                                    TimeStamp >=DATEADD(hour, -@hours, DATEADD(minute, -1,GETUTCDATE()))
                            ),
                            Durations AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    NextTimeStamp,
                                    DATEDIFF(minute, TimeStamp, NextTimeStamp) AS DurationInMinutes
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
                                Monitor m ON d.MonitorId = m.id
                            GROUP BY
                                d.MonitorId, m.name";
        return await db.QueryAsync<MonitorReportUptime>(sqlAllMonitors, new { groupId, hours },
            commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorReportAlerts>> GetMonitorAlerts(int groupId, int hours)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = $@"SELECT m.Name AS MonitorName, ma.MonitorId, COUNT(*) AS NumAlerts
                                    FROM MonitorAlert ma
                                    JOIN Monitor m ON ma.MonitorId = m.id
                                    WHERE ma.Status = 'false'
                                    AND ma.MonitorId IN (select monitorid from MonitorGroupItems where MonitorGroupId = @groupId)
                                    AND ma.Timestamp >= DATEADD(HOUR, -@hours, DATEADD(minute, -1,GETDATE()))
                                    GROUP BY ma.MonitorId, m.Name
                                    ORDER BY m.Name;";
        return await db.QueryAsync<MonitorReportAlerts>(sqlAllMonitors, new { groupId, hours },
            commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorReponseTime>> GetMonitorResponseTime(int groupId, int hours)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = $@"SELECT
                                mh.MonitorId,
                                m.Name AS MonitorName,
                                AVG(mh.ResponseTime) AS AvgResponseTime,
                                MAX(mh.ResponseTime) AS MaxResponseTime,
                                MIN(mh.ResponseTime) AS MinResponseTime
                            FROM
                                MonitorHistory mh
                            JOIN
                                Monitor m ON mh.MonitorId = m.Id
                            WHERE
                                mh.TimeStamp >= DATEADD(HOUR, -@hours, GETDATE()) AND
                                mh.Status = 1 AND
                                mh.MonitorId IN (select MonitorId from MonitorGroupItems where MonitorGroupId = @groupId)
                            GROUP BY
                                mh.MonitorId, m.Name
                            ORDER BY
                                m.Name;";
        return await db.QueryAsync<MonitorReponseTime>(sqlAllMonitors, new { groupId, hours }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, DateTime startDate, DateTime endDate)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlAllMonitors = @"WITH StatusChanges AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    LEAD(TimeStamp) OVER (PARTITION BY MonitorId ORDER BY TimeStamp) AS NextTimeStamp
                                FROM
                                    MonitorHistory
                                WHERE
                                    MonitorId IN (SELECT monitorid FROM MonitorGroupItems WHERE MonitorGroupId = @groupId) AND
                                    TimeStamp >= @startDate AND
                                    TimeStamp < @endDate
                            ),
                            Durations AS (
                                SELECT
                                    MonitorId,
                                    Status,
                                    TimeStamp,
                                    NextTimeStamp,
                                    DATEDIFF(minute, TimeStamp, NextTimeStamp) AS DurationInMinutes
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
                                Monitor m ON d.MonitorId = m.id
                            GROUP BY
                                d.MonitorId, m.name";

        return await db.QueryAsync<MonitorReportUptime>(sqlAllMonitors, new { groupId, startDate, endDate },
            commandType: CommandType.Text);
    }
}