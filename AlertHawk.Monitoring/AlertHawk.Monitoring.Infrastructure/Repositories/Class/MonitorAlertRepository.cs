using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

public class MonitorAlertRepository : RepositoryBase, IMonitorAlertRepository
{
    private readonly string _connstring;

    public MonitorAlertRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days, List<int>? groupIds)
    {
        await using var db = new SqlConnection(_connstring);
        string sql;

        if (monitorId > 0)
        {
            sql =
                @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.ScreenShotUrl 
                    FROM MonitorAlert MA 
                    INNER JOIN Monitor M on M.Id = MA.MonitorId 
                    WHERE MA.MonitorId = {monitorId} AND MA.TimeStamp >= DATEADD(day, -{days}, GETDATE()) AND MA.[Status] = 0 
                    ORDER BY MA.TimeStamp DESC";
            return await db.QueryAsync<MonitorAlert>(sql, commandType: CommandType.Text);
        }

        sql =
            @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.ScreenShotUrl
                FROM MonitorAlert MA
                INNER JOIN Monitor M on M.Id = MA.MonitorId
                INNER JOIN MonitorGroupItems MGI on MGI.MonitorId = M.Id
                WHERE MA.TimeStamp >= DATEADD(day, -{days}, GETDATE()) AND MA.[Status] = 0
                AND MGI.MonitorGroupId in ({string.Join(",", groupIds)})
                ORDER BY MA.TimeStamp DESC";

        return await db.QueryAsync<MonitorAlert>(sql, commandType: CommandType.Text);
    }
}