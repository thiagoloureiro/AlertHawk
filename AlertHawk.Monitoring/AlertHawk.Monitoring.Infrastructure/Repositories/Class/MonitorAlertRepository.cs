using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorAlertRepository : RepositoryBase, IMonitorAlertRepository
{
    public MonitorAlertRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days,
        MonitorEnvironment? environment, List<int>? groupIds)
    {
        using var db = CreateConnection();
        var alertTable = Helpers.DatabaseProvider.FormatTableName("MonitorAlert", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var httpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        
        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(day, -@days, GETDATE())",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(days => @days)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        var inClause = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "IN @groupIds",
            DatabaseProviderType.PostgreSQL => "= ANY(@groupIds)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        string sql;

        if (monitorId > 0)
        {
            if (environment == MonitorEnvironment.All)
            {
                sql =
                    $"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck " +
                    $"FROM {alertTable} MA " +
                    $"INNER JOIN {monitorTable} M on M.Id = MA.MonitorId " +
                    $"LEFT JOIN {httpTable} MH on MH.MonitorId = M.Id " +
                    $"WHERE MA.MonitorId = @monitorId AND MA.TimeStamp >= {dateFilter} " +
                    $"ORDER BY MA.TimeStamp DESC";
            }
            else
            {
                sql =
                    $"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck " +
                    $"FROM {alertTable} MA " +
                    $"INNER JOIN {monitorTable} M on M.Id = MA.MonitorId " +
                    $"LEFT JOIN {httpTable} MH on MH.MonitorId = M.Id " +
                    $"WHERE MA.MonitorId = @monitorId AND MA.TimeStamp >= {dateFilter} AND MA.environment = @environment " +
                    $"ORDER BY MA.TimeStamp DESC";
            }

            return await db.QueryAsync<MonitorAlert>(sql, new { monitorId, days, environment },
                commandType: CommandType.Text);
        }

        if (environment == MonitorEnvironment.All)
        {
            sql =
                $"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment, MH.UrlToCheck " +
                $"FROM {alertTable} MA " +
                $"INNER JOIN {monitorTable} M on M.Id = MA.MonitorId " +
                $"INNER JOIN {groupItemsTable} MGI on MGI.MonitorId = M.Id " +
                $"LEFT JOIN {httpTable} MH on MH.MonitorId = M.Id " +
                $"WHERE MA.TimeStamp >= {dateFilter} " +
                $"AND MGI.MonitorGroupId {inClause} " +
                $"ORDER BY MA.TimeStamp DESC";
        }
        else
        {
            sql =
                $"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment, MH.UrlToCheck " +
                $"FROM {alertTable} MA " +
                $"INNER JOIN {monitorTable} M on M.Id = MA.MonitorId " +
                $"INNER JOIN {groupItemsTable} MGI on MGI.MonitorId = M.Id " +
                $"LEFT JOIN {httpTable} MH on MH.MonitorId = M.Id " +
                $"WHERE MA.TimeStamp >= {dateFilter} AND MA.environment = @environment " +
                $"AND MGI.MonitorGroupId {inClause} " +
                $"ORDER BY MA.TimeStamp DESC";
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

            worksheet.Cells[1, col++].Value = "Timestamp (UTC)";
            worksheet.Cells[1, col++].Value = "Monitor Name";
            worksheet.Cells[1, col++].Value = "Environment";
            worksheet.Cells[1, col++].Value = "Message";
            worksheet.Cells[1, col++].Value = "URL";
            worksheet.Cells[1, col++].Value = "Period Offline";

            var row = 2;
            foreach (var alert in alerts)
            {
                col = 1;
                worksheet.Cells[row, col++].Value = alert.TimeStamp.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cells[row, col++].Value = alert.MonitorName;
                worksheet.Cells[row, col++].Value = alert.Environment.ToString();
                worksheet.Cells[row, col++].Value = alert.Message;
                worksheet.Cells[row, col++].Value = alert.UrlToCheck;
                worksheet.Cells[row, col++].Value = alert.PeriodOffline;
                row++;
            }

            await package.SaveAsync();
        }

        stream.Position = 0;
        return stream;
    }

    public async Task SaveMonitorAlert(MonitorHistory monitorHistory, MonitorEnvironment environment)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorAlert", DatabaseProvider);
        string sql =
            $"INSERT INTO {tableName} (MonitorId, TimeStamp, Status, Message, Environment) VALUES (@MonitorId, @TimeStamp, @Status, @Message, @Environment)";
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
        using var db = CreateConnection();
        var alertTable = Helpers.DatabaseProvider.FormatTableName("MonitorAlert", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var httpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        
        var dateFilter = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "DATEADD(day, -@days, GETDATE())",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP - make_interval(days => @days)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        var inClause = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "IN @monitorListIds",
            DatabaseProviderType.PostgreSQL => "= ANY(@monitorListIds)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        
        string sql;

        if (environment == MonitorEnvironment.All)
        {
            sql =
                $"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck " +
                $"FROM {alertTable} MA " +
                $"INNER JOIN {monitorTable} M on M.Id = MA.MonitorId " +
                $"LEFT JOIN {httpTable} MH on MH.MonitorId = M.Id " +
                $"WHERE MA.MonitorId {inClause} AND MA.TimeStamp >= {dateFilter} " +
                $"ORDER BY MA.TimeStamp DESC";
        }
        else
        {
            sql =
                $"SELECT M.Name as MonitorName, MA.Id, MA.MonitorId, MA.TimeStamp, MA.Status, MA.Message, MA.Environment,  MH.UrlToCheck " +
                $"FROM {alertTable} MA " +
                $"INNER JOIN {monitorTable} M on M.Id = MA.MonitorId " +
                $"LEFT JOIN {httpTable} MH on MH.MonitorId = M.Id " +
                $"WHERE MA.MonitorId {inClause} AND MA.TimeStamp >= {dateFilter} AND MA.environment = @environment " +
                $"ORDER BY MA.TimeStamp DESC";
        }

        return await db.QueryAsync<MonitorAlert>(sql, new { monitorListIds, days, environment },
            commandType: CommandType.Text);
    }
}