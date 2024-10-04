using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System.Data;
using System.Data.SqlClient;
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
                    WHERE MA.MonitorId = @monitorId AND MA.TimeStamp >= DATEADD(day, -@days, GETDATE()) AND MA.[Status] = 0
                    ORDER BY MA.TimeStamp DESC";
            }
            else
            {
                sql =
                    @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck
                    FROM MonitorAlert MA
                    INNER JOIN Monitor M on M.Id = MA.MonitorId
                    LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                    WHERE MA.MonitorId = @monitorId AND MA.TimeStamp >= DATEADD(day, -@days, GETDATE()) AND MA.[Status] = 0 AND MA.environment = @environment
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
                WHERE MA.TimeStamp >= DATEADD(day, -@days, GETDATE()) AND MA.[Status] = 0
                AND MGI.MonitorGroupId in @groupIds
                ORDER BY MA.TimeStamp DESC";
            Console.WriteLine(sql);
        }
        else
        {
            sql =
                @$"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment, MH.UrlToCheck
                FROM MonitorAlert MA
                INNER JOIN Monitor M on M.Id = MA.MonitorId
                INNER JOIN MonitorGroupItems MGI on MGI.MonitorId = M.Id
                LEFT JOIN MonitorHttp MH on MH.MonitorId = M.Id
                WHERE MA.TimeStamp >= DATEADD(day, -@days, GETDATE()) AND MA.[Status] = 0 AND MA.environment = @environment
                AND MGI.MonitorGroupId in @groupIds
                ORDER BY MA.TimeStamp DESC";
            Console.WriteLine(sql);
        }

        return await db.QueryAsync<MonitorAlert>(sql, new { days, groupIds, environment },
            commandType: CommandType.Text);
    }

    public async Task<MemoryStream> CreateExcelFileAsync(IEnumerable<MonitorAlert> alerts)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set license context
        var stream = new MemoryStream();

        using (var package = new ExcelPackage(stream))
        {
            // Add a new worksheet to the empty workbook
            var worksheet = package.Workbook.Worksheets.Add("Alerts");
            // Add column headers
            var col = 1;

            worksheet.Cells[1, col++].Value = "Timestamp";
            worksheet.Cells[1, col++].Value = "Monitor Name";
            worksheet.Cells[1, col++].Value = "Environment";
            worksheet.Cells[1, col++].Value = "Message";
            worksheet.Cells[1, col++].Value = "URL";

            var row = 2;
            foreach (var alert in alerts)
            {
                col = 1;
                worksheet.Cells[row, col++].Value = alert.TimeStamp.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cells[row, col++].Value = alert.MonitorName;
                worksheet.Cells[row, col++].Value = alert.Environment.ToString();
                worksheet.Cells[row, col++].Value = alert.Message;
                worksheet.Cells[row, col++].Value = alert.UrlToCheck;
                row++;
            }

            await package.SaveAsync();
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
}