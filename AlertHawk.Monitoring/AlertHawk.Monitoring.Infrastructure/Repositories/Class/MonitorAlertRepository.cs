using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorAlertRepository : RepositoryBase, IMonitorAlertRepository
{
    private readonly string _connstring;

    public MonitorAlertRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days,
        MonitorEnvironment? environment, List<int>? groupIds)
    {
        await using var db = new SqlConnection(_connstring);
        string sql;

        if (monitorId > 0)
        {
            if (environment == MonitorEnvironment.All)
            {
                sql =
                    @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck
                    FROM MonitorAlert MA
                    INNER JOIN Monitor M on M.Id = MA.MonitorId
                    LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                    WHERE MA.MonitorId = @monitorId AND MA.TimeStamp >= DATEADD(day, -@days, GETDATE())
                    ORDER BY MA.TimeStamp DESC";
            }
            else
            {
                sql =
                    @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck
                    FROM MonitorAlert MA
                    INNER JOIN Monitor M on M.Id = MA.MonitorId
                    LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                    WHERE MA.MonitorId = @monitorId AND MA.TimeStamp >= DATEADD(day, -@days, GETDATE()) AND MA.environment = @environment
                    ORDER BY MA.TimeStamp DESC";
            }

            return await db.QueryAsync<MonitorAlert>(sql, new { monitorId, days, environment },
                commandType: CommandType.Text);
        }

        if (environment == MonitorEnvironment.All)
        {
            sql =
                @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment, MH.UrlToCheck
                    FROM MonitorAlert MA
                    INNER JOIN Monitor M on M.Id = MA.MonitorId
                    INNER JOIN MonitorGroupItems MGI on MGI.MonitorId = M.Id
                    LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                WHERE MA.TimeStamp >= DATEADD(day, -@days, GETDATE())
                AND MGI.MonitorGroupId in @groupIds
                ORDER BY MA.TimeStamp DESC";
        }
        else
        {
            sql =
                @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment, MH.UrlToCheck
                FROM MonitorAlert MA
                INNER JOIN Monitor M on M.Id = MA.MonitorId
                INNER JOIN MonitorGroupItems MGI on MGI.MonitorId = M.Id
                LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                WHERE MA.TimeStamp >= DATEADD(day, -@days, GETDATE()) AND MA.environment = @environment
                AND MGI.MonitorGroupId in @groupIds
                ORDER BY MA.TimeStamp DESC";
        }

        return await db.QueryAsync<MonitorAlert>(sql, new { days, groupIds, environment },
            commandType: CommandType.Text);
    }

    public async Task<MemoryStream> CreateExcelFileAsync(IEnumerable<MonitorAlert> alerts)
    {
        var stream = new MemoryStream();

        using (var workbook = new XLWorkbook())
        {
            // Add a new worksheet to the empty workbook
            var worksheet = workbook.Worksheets.Add("Alerts");
            
            // Add column headers
            worksheet.Cell(1, 1).Value = "Timestamp (UTC)";
            worksheet.Cell(1, 2).Value = "Monitor Name";
            worksheet.Cell(1, 3).Value = "Environment";
            worksheet.Cell(1, 4).Value = "Message";
            worksheet.Cell(1, 5).Value = "URL";
            worksheet.Cell(1, 6).Value = "Period Offline";

            // Style the header row
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;
            foreach (var alert in alerts)
            {
                worksheet.Cell(row, 1).Value = alert.TimeStamp.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cell(row, 2).Value = alert.MonitorName;
                worksheet.Cell(row, 3).Value = alert.Environment.ToString();
                worksheet.Cell(row, 4).Value = alert.Message;
                worksheet.Cell(row, 5).Value = alert.UrlToCheck;
                worksheet.Cell(row, 6).Value = alert.PeriodOffline;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }

    public async Task SaveMonitorAlert(MonitorHistory monitorHistory, MonitorEnvironment environment)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [MonitorAlert] (MonitorId, TimeStamp, Status, Message, Environment) VALUES (@MonitorId, @TimeStamp, @Status, @Message, @Environment)";
        await db.ExecuteAsync(sql,
            new
            {
                monitorHistory.MonitorId,
                monitorHistory.TimeStamp,
                monitorHistory.Status,
                Message = monitorHistory.ResponseMessage,
                Environment = environment
            }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlertsByMonitorGroup(List<int> monitorListIds, int? days,
        MonitorEnvironment? environment)
    {
        await using var db = new SqlConnection(_connstring);
        string sql;


        if (environment == MonitorEnvironment.All)
        {
            sql =
                @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck
                    FROM MonitorAlert MA
                    INNER JOIN Monitor M on M.Id = MA.MonitorId
                    LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                    WHERE MA.MonitorId IN @monitorListIds AND MA.TimeStamp >= DATEADD(day, -@days, GETDATE())
                    ORDER BY MA.TimeStamp DESC";
        }
        else
        {
            sql =
                @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck
                    FROM MonitorAlert MA
                    INNER JOIN Monitor M on M.Id = MA.MonitorId
                    LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                    WHERE MA.MonitorId in @monitorListIds AND MA.TimeStamp >= DATEADD(day, -@days, GETDATE()) AND MA.environment = @environment
                    ORDER BY MA.TimeStamp DESC";
        }

        return await db.QueryAsync<MonitorAlert>(sql, new { monitorListIds, days, environment },
            commandType: CommandType.Text);
    }
}