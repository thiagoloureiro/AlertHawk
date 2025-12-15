using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Hangfire;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorRepository : RepositoryBase, IMonitorRepository
{
    public MonitorRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorList()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sql =
            $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment FROM {tableName}";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorRunningList()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sql =
            $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment FROM {tableName} WHERE Paused = 0";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor?>> GetFullMonitorList()
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var monitorHttpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        var monitorTcpTable = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        var concatExpr = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "CAST(IP AS VARCHAR(255)) + ':' + CAST(Port AS VARCHAR(10))",
            DatabaseProviderType.PostgreSQL => "CAST(IP AS VARCHAR(255)) || ':' || CAST(Port AS VARCHAR(10))",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        string sql =
            $"SELECT M.Id, M.Name, HTTP.UrlToCheck, {concatExpr} AS MonitorTcp, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag, HTTP.CheckCertExpiry, HTTP.HttpResponseCodeFrom, HTTP.HttpResponseCodeTo, HTTP.CheckMonitorHttpHeaders FROM {monitorTable} M " +
            $"LEFT JOIN {monitorHttpTable} HTTP on HTTP.MonitorId = M.Id " +
            $"LEFT JOIN {monitorTcpTable} TCP ON TCP.MonitorId = M.Id";
        return await db.QueryAsync<Monitor>(sql, new { }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHttp>> GetMonitorHttpList()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        string sql =
            $"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson, HttpResponseCodeFrom, HttpResponseCodeTo, CheckMonitorHttpHeaders FROM {tableName}";
        return await db.QueryAsync<MonitorHttp>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorTcp>> GetMonitorTcpList()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        string sql = $"SELECT MonitorId, Port, IP, Timeout, LastStatus FROM {tableName}";
        return await db.QueryAsync<MonitorTcp>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorK8s>> GetMonitorK8sList()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorK8s", DatabaseProvider);
        string sql = $"SELECT MonitorId, ClusterName, KubeConfig, LastStatus FROM {tableName}";
        return await db.QueryAsync<MonitorK8s>(sql, commandType: CommandType.Text);
    }

    public async Task<int> CreateMonitor(Monitor monitor)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sqlMonitor = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $@"INSERT INTO {tableName} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag); SELECT CAST(SCOPE_IDENTITY() as int)",
            DatabaseProviderType.PostgreSQL =>
                $@"INSERT INTO {tableName} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag) RETURNING Id",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        var id = await db.QuerySingleAsync<int>(sqlMonitor,
            new
            {
                monitor.Name,
                monitor.MonitorTypeId,
                monitor.HeartBeatInterval,
                monitor.Retries,
                monitor.Status,
                monitor.DaysToExpireCert,
                monitor.Paused,
                monitor.MonitorRegion,
                monitor.MonitorEnvironment,
                monitor.Tag
            }, commandType: CommandType.Text);
        return id;
    }

    public async Task WipeMonitorData()
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var tcpTable = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        var httpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        var agentTasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);
        var alertsTable = Helpers.DatabaseProvider.FormatTableName("MonitorAlert", DatabaseProvider);
        var historyTable = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        var notificationTable = Helpers.DatabaseProvider.FormatTableName("MonitorNotification", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        var groupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", DatabaseProvider);
        var sqlMonitor = $"TRUNCATE TABLE {monitorTable};";
        var sqlTcp = $"TRUNCATE TABLE {tcpTable};";
        var sqlHttp = $"TRUNCATE TABLE {httpTable};";
        var sqlAgentTasks = $"TRUNCATE TABLE {agentTasksTable};";
        var sqlAlerts = $"TRUNCATE TABLE {alertsTable};";
        var sqlHistory = $"TRUNCATE TABLE {historyTable};";
        var sqlNotification = $"TRUNCATE TABLE {notificationTable};";
        var sqlMonitorGroupItems = $"TRUNCATE TABLE {groupItemsTable};";
        var sqlMonitorGroup = $"TRUNCATE TABLE {groupTable};";
        await db.ExecuteAsync(sqlMonitor, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlTcp, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlHttp, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlAgentTasks, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlAlerts, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlHistory, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlNotification, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlMonitorGroupItems, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlMonitorGroup, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorList(MonitorEnvironment environment)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var monitorHttpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        var monitorTcpTable = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        var concatExpr = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer => "CAST(IP AS VARCHAR(255)) + ':' + CAST(Port AS VARCHAR(10))",
            DatabaseProviderType.PostgreSQL => "CAST(IP AS VARCHAR(255)) || ':' || CAST(Port AS VARCHAR(10))",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        string sql =
            $"SELECT M.Id, M.Name, HTTP.UrlToCheck, {concatExpr} AS MonitorTcp, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag, HTTP.CheckCertExpiry FROM {monitorTable} M " +
            $"LEFT JOIN {monitorHttpTable} HTTP on HTTP.MonitorId = M.Id " +
            $"LEFT JOIN {monitorTcpTable} TCP ON TCP.MonitorId = M.Id " +
            $"WHERE MonitorEnvironment = @environment";
        return await db.QueryAsync<Monitor>(sql, new { environment }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>?> GetMonitorListByMonitorGroupIds(List<int> groupMonitorIds,
        MonitorEnvironment environment)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM {monitorTable} M " +
                $"INNER JOIN {groupItemsTable} MGI ON MGI.MonitorId = M.Id WHERE MGI.MonitorGroupId IN @groupMonitorIds AND M.MonitorEnvironment = @environment",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM {monitorTable} M " +
                $"INNER JOIN {groupItemsTable} MGI ON MGI.MonitorId = M.Id WHERE MGI.MonitorGroupId = ANY(@groupMonitorIds) AND M.MonitorEnvironment = @environment",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        return await db.QueryAsync<Monitor>(sql, new { groupMonitorIds, environment }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorHttp(MonitorHttp monitorHttp)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var monitorHttpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);

        string sqlMonitor = $@"UPDATE {monitorTable}
                    SET [Name] = @Name
                    ,[HeartBeatInterval] = @HeartBeatInterval
                    ,[Retries] = @Retries
                    ,[Status] = @Status
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
            $@"UPDATE {monitorHttpTable} SET CheckCertExpiry = @CheckCertExpiry, IgnoreTlsSsl = @IgnoreTlsSsl,
            MaxRedirects = @MaxRedirects, UrlToCheck = @UrlToCheck, Timeout = @Timeout, MonitorHttpMethod = @MonitorHttpMethod,
            Body = @Body, HeadersJson = @HeadersJson, HttpResponseCodeFrom = @HttpResponseCodeFrom ,HttpResponseCodeTo = @HttpResponseCodeTo, CheckMonitorHttpHeaders = @CheckMonitorHttpHeaders
            WHERE MonitorId = @monitorId";

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
                monitorHttp.Timeout,
                monitorHttp.HttpResponseCodeFrom,
                monitorHttp.HttpResponseCodeTo,
                monitorHttp.CheckMonitorHttpHeaders
            }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitor(int id)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var agentTasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);
        var alertsTable = Helpers.DatabaseProvider.FormatTableName("MonitorAlert", DatabaseProvider);
        var httpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        var tcpTable = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        var k8sTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8s", DatabaseProvider);
        var notificationTable = Helpers.DatabaseProvider.FormatTableName("MonitorNotification", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        var httpHeadersTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttpHeaders", DatabaseProvider);
        string sql = $@"DELETE FROM {monitorTable} WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id }, commandType: CommandType.Text);

        string sqlTasks = $@"DELETE FROM {agentTasksTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlTasks, new { id }, commandType: CommandType.Text);

        string sqlAlerts = $@"DELETE FROM {alertsTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlAlerts, new { id }, commandType: CommandType.Text);

        string sqlHttp = $@"DELETE FROM {httpTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlHttp, new { id }, commandType: CommandType.Text);

        string sqlTcp = $@"DELETE FROM {tcpTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlTcp, new { id }, commandType: CommandType.Text);
        
        string sqlK8s = $@"DELETE FROM {k8sTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlK8s, new { id }, commandType: CommandType.Text);

        string sqlNotification = $@"DELETE FROM {notificationTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlNotification, new { id }, commandType: CommandType.Text);

        string sqlMonitorGroupItems = $@"DELETE FROM {groupItemsTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlMonitorGroupItems, new { id }, commandType: CommandType.Text);
        
        string sqlMonitorHttpHeaders = $@"DELETE FROM {httpHeadersTable} WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlMonitorHttpHeaders, new { id }, commandType: CommandType.Text);

        // Enqueue the deletion of history as a background job
        BackgroundJob.Enqueue(() => DeleteMonitorHistory(id));
    }

    public void DeleteMonitorHistory(int id)
    {
        // This method will be executed in the background
        using var db = CreateConnection();
        var historyTable = Helpers.DatabaseProvider.FormatTableName("MonitorHistory", DatabaseProvider);
        string sqlHistory = $@"DELETE FROM {historyTable} WHERE MonitorId=@id";
        db.Execute(sqlHistory, new { id }, commandType: CommandType.Text, commandTimeout: 1800);
    }

    public async Task<int> CreateMonitorTcp(MonitorTcp monitorTcp)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var tcpTable = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        string sqlMonitor = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $@"INSERT INTO {monitorTable} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag); SELECT CAST(SCOPE_IDENTITY() as int)",
            DatabaseProviderType.PostgreSQL =>
                $@"INSERT INTO {monitorTable} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag) RETURNING Id",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        var id = await db.QuerySingleAsync<int>(sqlMonitor,
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
                monitorTcp.MonitorEnvironment,
                monitorTcp.Tag
            }, commandType: CommandType.Text);

        string sqlMonitorTcp =
            $@"INSERT INTO {tcpTable} (MonitorId, Port, IP, Timeout, LastStatus) VALUES (@MonitorId, @Port, @IP, @Timeout, @LastStatus)";
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
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var tcpTable = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);

        string sqlMonitor = $@"UPDATE {monitorTable}
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
            $@"UPDATE {tcpTable} SET MonitorId = @MonitorId, Port = @Port, IP = @IP, Timeout = @Timeout, LastStatus = @LastStatus WHERE MonitorId = @MonitorId";
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
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT MonitorId, Port, IP, Timeout, LastStatus FROM {tableName} WHERE MonitorId IN @ids",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT MonitorId, Port, IP, Timeout, LastStatus FROM {tableName} WHERE MonitorId = ANY(@ids)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };

        return await db.QueryAsync<MonitorTcp>(sql, new { ids }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorK8s>> GetK8sMonitorByIds(List<int> ids)
    {
        using var db = CreateConnection();
        var k8sTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8s", DatabaseProvider);
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT MonitorId, ClusterName, KubeConfig, LastStatus, M.MonitorEnvironment FROM {k8sTable} MK " +
                $"inner join {monitorTable} M on M.Id = MK.MonitorId WHERE MonitorId IN @ids",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT MonitorId, ClusterName, KubeConfig, LastStatus, M.MonitorEnvironment FROM {k8sTable} MK " +
                $"inner join {monitorTable} M on M.Id = MK.MonitorId WHERE MonitorId = ANY(@ids)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };

        return await db.QueryAsync<MonitorK8s>(sql, new { ids }, commandType: CommandType.Text);
    }

    public async Task<int> CreateMonitorK8s(MonitorK8s monitorK8S)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var k8sTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8s", DatabaseProvider);
        string sqlMonitor = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $@"INSERT INTO {monitorTable} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag); SELECT CAST(SCOPE_IDENTITY() as int)",
            DatabaseProviderType.PostgreSQL =>
                $@"INSERT INTO {monitorTable} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag) RETURNING Id",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };

        var id = await db.QuerySingleAsync<int>(sqlMonitor,
            new
            {
                monitorK8S.Name,
                monitorK8S.MonitorTypeId,
                monitorK8S.HeartBeatInterval,
                monitorK8S.Retries,
                monitorK8S.Status,
                monitorK8S.DaysToExpireCert,
                monitorK8S.Paused,
                monitorK8S.MonitorRegion,
                monitorK8S.MonitorEnvironment,
                monitorK8S.Tag
            }, commandType: CommandType.Text);

        string sqlMonitorK8s =
            $@"INSERT INTO {k8sTable} (MonitorId, ClusterName, KubeConfig, LastStatus) VALUES (@MonitorId, @ClusterName, @KubeConfig, @LastStatus)";
        await db.ExecuteAsync(sqlMonitorK8s,
            new
            {
                MonitorId = id,
                monitorK8S.ClusterName,
                monitorK8S.KubeConfig,
                monitorK8S.LastStatus
            }, commandType: CommandType.Text);
        return id;
    }

    public async Task<IEnumerable<Monitor>> GetMonitorListByIds(List<int> ids)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM {tableName} WHERE Id IN @ids",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM {tableName} WHERE Id = ANY(@ids)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        return await db.QueryAsync<Monitor>(sql, new { ids }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>> GetMonitorListbyTag(string Tag)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);

        string sql =
            $"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM {tableName} WHERE Tag = @Tag";
        return await db.QueryAsync<Monitor>(sql, new { Tag }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<string?>> GetMonitorTagList()
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sql = $"SELECT DISTINCT(Tag) FROM {tableName} WHERE Tag IS NOT NULL";
        return await db.QueryAsync<string>(sql, commandType: CommandType.Text);
    }

    public async Task<Monitor> GetMonitorById(int id)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);

        string sql =
            $"SELECT M.Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag, MGI.MonitorGroupId as MonitorGroup " +
            $"FROM {monitorTable} M " +
            $"LEFT JOIN {groupItemsTable} MGI on MGI.MonitorId = M.Id WHERE M.Id=@id";

        var monitorHttp = await GetHttpMonitorByMonitorId(id);
        var monitorTcp = await GetTcpMonitorByMonitorId(id);
        var monitorK8s = await GetK8sMonitorByMonitorId(id);

        var monitor = await db.QueryFirstOrDefaultAsync<Monitor>(sql, new { id }, commandType: CommandType.Text);
        if (monitor != null)
        {
            monitor.MonitorTcpItem = monitorTcp;
            monitor.MonitorHttpItem = monitorHttp;
            monitor.MonitorK8sItem = monitorK8s;
        }

        return monitor;
    }

    public async Task<MonitorHttp> GetHttpMonitorByMonitorId(int monitorId)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var monitorHttpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);

        string sql =
            $"SELECT a.Id, a.Name, a.MonitorTypeId, a.HeartBeatInterval, a.Retries, a.Status, a.DaysToExpireCert, a.Paused, a.MonitorRegion, a.MonitorEnvironment, a.Tag, " +
            $"b.MonitorId, b.CheckCertExpiry, b.IgnoreTlsSsl, b.MaxRedirects, b.UrlToCheck, b.Timeout, b.MonitorHttpMethod, b.Body, b.HeadersJson, MGI.MonitorGroupId as MonitorGroup, b.HttpResponseCodeFrom, b.HttpResponseCodeTo, b.CheckMonitorHttpHeaders " +
            $"FROM {monitorTable} a " +
            $"inner join {monitorHttpTable} b on a.Id = b.MonitorId " +
            $"inner join {groupItemsTable} MGI on MGI.MonitorId = a.Id " +
            $"WHERE b.MonitorId = @monitorId";
        return await db.QueryFirstOrDefaultAsync<MonitorHttp>(sql, new { monitorId }, commandType: CommandType.Text);
    }

    public async Task<MonitorTcp> GetTcpMonitorByMonitorId(int monitorId)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var monitorTcpTable = Helpers.DatabaseProvider.FormatTableName("MonitorTcp", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);

        string sql =
            $"SELECT a.Id, a.Name, a.MonitorTypeId, a.HeartBeatInterval, a.Retries, a.Status, a.DaysToExpireCert, a.Paused, a.MonitorRegion, a.MonitorEnvironment, a.Tag, " +
            $"b.MonitorId, b.Port, b.IP, b.Timeout, b.LastStatus, MGI.MonitorGroupId as MonitorGroup " +
            $"FROM {monitorTable} a inner join " +
            $"{monitorTcpTable} b on a.Id = b.MonitorId " +
            $"inner join {groupItemsTable} MGI on MGI.MonitorId = b.MonitorId " +
            $"WHERE b.MonitorId = @monitorId";
        return await db.QueryFirstOrDefaultAsync<MonitorTcp>(sql, new { monitorId }, commandType: CommandType.Text);
    }

    public async Task<MonitorK8s?> GetK8sMonitorByMonitorId(int monitorId)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var monitorK8sTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8s", DatabaseProvider);
        var groupItemsTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroupItems", DatabaseProvider);
        var nodesTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8sNodes", DatabaseProvider);

        string sql =
            $"SELECT a.Id, a.Name, a.MonitorTypeId, a.HeartBeatInterval, a.Retries, a.Status, a.DaysToExpireCert, a.Paused, a.MonitorRegion, a.MonitorEnvironment, a.Tag, " +
            $"b.MonitorId, b.ClusterName, b.KubeConfig, b.LastStatus, MGI.MonitorGroupId as MonitorGroup " +
            $"FROM {monitorTable} a inner join " +
            $"{monitorK8sTable} b on a.Id = b.MonitorId " +
            $"inner join {groupItemsTable} MGI on MGI.MonitorId = b.MonitorId " +
            $"WHERE b.MonitorId = @monitorId";
        var monitorK8s = await db.QueryFirstOrDefaultAsync<MonitorK8s>(sql, new { monitorId }, commandType: CommandType.Text);
        
        // return the nodes status
        var sqlNodes = $"SELECT * FROM {nodesTable} WHERE MonitorK8sId = @monitorId";
        var nodes = await db.QueryAsync<K8sNodeStatusModel>(sqlNodes, new { monitorId }, commandType: CommandType.Text);
        if (monitorK8s != null)
        {
            monitorK8s.MonitorK8sNodes = nodes;

            return monitorK8s;
        }

        return null;
    }

    public async Task UpdateMonitorK8s(MonitorK8s monitorK8S)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var k8sTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8s", DatabaseProvider);
        string sql =
            $"UPDATE {monitorTable} SET Name=@Name, HeartBeatInterval=@HeartBeatInterval, Retries=@Retries, MonitorRegion=@MonitorRegion, MonitorEnvironment=@MonitorEnvironment WHERE Id=@MonitorId";

        await db.ExecuteAsync(sql, new
        {
            monitorK8S.Name,
            monitorK8S.HeartBeatInterval,
            monitorK8S.Retries,
            monitorK8S.Status,
            monitorK8S.MonitorRegion,
            monitorK8S.MonitorEnvironment,
            monitorK8S.MonitorId
        }, commandType: CommandType.Text);

        if (!String.IsNullOrEmpty(monitorK8S.KubeConfig))
        {
            string sqlMonitork8s =
                $"UPDATE {k8sTable} SET ClusterName=@ClusterName, KubeConfig=@KubeConfig WHERE MonitorId=@MonitorId";

            await db.ExecuteAsync(sqlMonitork8s, new { monitorK8S.MonitorId, monitorK8S.ClusterName, monitorK8S.KubeConfig }, commandType: CommandType.Text);
        }
        else
        {
            string sqlMonitork8s =
                $"UPDATE {k8sTable} SET ClusterName=@ClusterName WHERE MonitorId=@MonitorId";

            await db.ExecuteAsync(sqlMonitork8s, new { monitorK8S.MonitorId, monitorK8S.ClusterName }, commandType: CommandType.Text);
        }
    }
    
    public async Task UpdateK8sMonitorNodeStatus(MonitorK8s monitorK8S)
    {
        using var db = CreateConnection();
        var nodesTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8sNodes", DatabaseProvider);
        
        // Delete all existing nodes to avoid duplication
        var sqlDeleteNodes = $"DELETE FROM {nodesTable} WHERE MonitorK8sId = @MonitorId";
        await db.ExecuteAsync(sqlDeleteNodes, new { monitorK8S.MonitorId }, commandType: CommandType.Text);

        if (monitorK8S.MonitorK8sNodes != null)
        {
            foreach (var node in monitorK8S.MonitorK8sNodes)
            {
                var sql = $"INSERT INTO {nodesTable} (MonitorK8sId, NodeName, ContainerRuntimeProblem, KernelDeadlock, KubeletProblem, FrequentUnregisterNetDevice, FilesystemCorruptionProblem, ReadonlyFilesystem, FrequentKubeletRestart, FrequentDockerRestart, FrequentContainerdRestart, MemoryPressure, DiskPressure, PIDPressure, Ready) VALUES (@MonitorK8sId, @NodeName, @ContainerRuntimeProblem, @KernelDeadlock, @KubeletProblem, @FrequentUnregisterNetDevice, @FilesystemCorruptionProblem, @ReadonlyFilesystem, @FrequentKubeletRestart, @FrequentDockerRestart, @FrequentContainerdRestart, @MemoryPressure, @DiskPressure, @PIDPressure, @Ready)";
                await db.ExecuteAsync(sql, new {MonitorK8sId = monitorK8S.MonitorId, node.NodeName, node.ContainerRuntimeProblem, node.KernelDeadlock, node.KubeletProblem, node.FrequentUnregisterNetDevice, node.FilesystemCorruptionProblem, node.ReadonlyFilesystem, node.FrequentKubeletRestart, node.FrequentDockerRestart, node.FrequentContainerdRestart, node.MemoryPressure, node.DiskPressure, node.PIDPressure, node.Ready} , commandType: CommandType.Text);
            }
        }
    }

    public async Task UpdateMonitorStatus(int id, bool status, int daysToExpireCert)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        string sql = $@"UPDATE {tableName} SET Status=@status, DaysToExpireCert=@daysToExpireCert WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id, status, daysToExpireCert }, commandType: CommandType.Text);
    }

    public async Task PauseMonitor(int id, bool paused)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var agentTasksTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgentTasks", DatabaseProvider);
        string sql = $@"UPDATE {monitorTable} SET paused=@paused WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id, paused }, commandType: CommandType.Text);

        if (paused)
        {
            var sqlRemoveTasks = $@"DELETE FROM {agentTasksTable} WHERE MonitorId=@id";
            await db.ExecuteAsync(sqlRemoveTasks, new { id }, commandType: CommandType.Text);
        }
    }

    public async Task<int> CreateMonitorHttp(MonitorHttp monitorHttp)
    {
        using var db = CreateConnection();
        var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", DatabaseProvider);
        var monitorHttpTable = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        string sqlMonitor = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $@"INSERT INTO {monitorTable} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment) VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment); SELECT CAST(SCOPE_IDENTITY() as int)",
            DatabaseProviderType.PostgreSQL =>
                $@"INSERT INTO {monitorTable} (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment) VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment) RETURNING Id",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };
        var id = await db.QuerySingleAsync<int>(sqlMonitor,
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
            $@"INSERT INTO {monitorHttpTable} (MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson, HttpResponseCodeFrom, HttpResponseCodeTo, CheckMonitorHttpHeaders)
        VALUES (@MonitorId, @CheckCertExpiry, @IgnoreTlsSsl, @MaxRedirects, @UrlToCheck, @Timeout, @MonitorHttpMethod, @Body, @HeadersJson, @HttpResponseCodeFrom, @HttpResponseCodeTo, @CheckMonitorHttpHeaders)";
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
                monitorHttp.Timeout,
                monitorHttp.HttpResponseCodeFrom, 
                monitorHttp.HttpResponseCodeTo,
                monitorHttp.CheckMonitorHttpHeaders
            }, commandType: CommandType.Text);
        return id;
    }

    public async Task<IEnumerable<MonitorHttp>> GetHttpMonitorByIds(List<int> ids)
    {
        using var db = CreateConnection();
        var tableName = Helpers.DatabaseProvider.FormatTableName("MonitorHttp", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson, HttpResponseCodeFrom, HttpResponseCodeTo, CheckMonitorHttpHeaders FROM {tableName} WHERE MonitorId IN @ids",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson, HttpResponseCodeFrom, HttpResponseCodeTo, CheckMonitorHttpHeaders FROM {tableName} WHERE MonitorId = ANY(@ids)",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };

        return await db.QueryAsync<MonitorHttp>(sql, new { ids }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorFailureCount>> GetMonitorFailureCount(int days)
    {
        using var db = CreateConnection();
        var alertsTable = Helpers.DatabaseProvider.FormatTableName("MonitorAlert", DatabaseProvider);
        string sql = DatabaseProvider switch
        {
            DatabaseProviderType.SqlServer =>
                $"SELECT MonitorId, COUNT(Status) AS FailureCount " +
                $"FROM {alertsTable} " +
                $"WHERE Status = 'false' AND TimeStamp >= DATEADD(DAY, -@days, GETDATE()) " +
                $"GROUP BY MonitorId;",
            DatabaseProviderType.PostgreSQL =>
                $"SELECT MonitorId, COUNT(Status) AS FailureCount " +
                $"FROM {alertsTable} " +
                $"WHERE Status = 'false' AND TimeStamp >= CURRENT_TIMESTAMP - make_interval(days => @days) " +
                $"GROUP BY MonitorId;",
            _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
        };

        return await db.QueryAsync<MonitorFailureCount>(sql, new { days }, commandType: CommandType.Text);
    }
}