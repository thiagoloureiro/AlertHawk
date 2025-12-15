using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace AlertHawk.Monitoring.Infrastructure.Helpers;

[ExcludeFromCodeCoverage]
public class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly DatabaseProviderType _databaseProvider;
    private string _connectionStringWithDatabase;

    public DatabaseInitializer(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlConnectionString")
                            ?? throw new InvalidOperationException(
                                "Connection string 'SqlConnectionString' not found.");

        var providerString = configuration["DatabaseProvider"] ?? "SqlServer";
        _databaseProvider = Enum.TryParse<DatabaseProviderType>(providerString, true, out var provider)
            ? provider
            : DatabaseProviderType.SqlServer;
        
        _connectionStringWithDatabase = _connectionString;
    }

    public async Task EnsureAllTablesExistAsync()
    {
        // First, ensure the database exists (this will update _connectionStringWithDatabase if needed)
        await EnsureDatabaseExistsAsync();

        using var connection = Helpers.DatabaseProvider.CreateConnection(_connectionStringWithDatabase, _databaseProvider);

        // Ensure MonitorType table exists (should be created first as it's referenced by Monitor)
        await EnsureMonitorTypeTableExistsAsync(connection);

        // Ensure Monitor table exists
        await EnsureMonitorTableExistsAsync(connection);

        // Ensure MonitorHttp table exists
        await EnsureMonitorHttpTableExistsAsync(connection);

        // Ensure MonitorTcp table exists
        await EnsureMonitorTcpTableExistsAsync(connection);

        // Ensure MonitorK8s table exists
        await EnsureMonitorK8sTableExistsAsync(connection);

        // Ensure MonitorK8sNodes table exists
        await EnsureMonitorK8sNodesTableExistsAsync(connection);

        // Ensure MonitorHistory table exists
        await EnsureMonitorHistoryTableExistsAsync(connection);

        // Ensure MonitorHttpHeaders table exists
        await EnsureMonitorHttpHeadersTableExistsAsync(connection);

        // Ensure MonitorAlert table exists
        await EnsureMonitorAlertTableExistsAsync(connection);

        // Ensure MonitorNotification table exists
        await EnsureMonitorNotificationTableExistsAsync(connection);

        // Ensure MonitorGroup table exists
        await EnsureMonitorGroupTableExistsAsync(connection);

        // Ensure MonitorGroupItems table exists
        await EnsureMonitorGroupItemsTableExistsAsync(connection);

        // Ensure MonitorAgent table exists
        await EnsureMonitorAgentTableExistsAsync(connection);

        // Ensure MonitorAgentTasks table exists
        await EnsureMonitorAgentTasksTableExistsAsync(connection);
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        const string defaultDatabaseName = "monitoring";
        
        // Extract database name from connection string
        string dbName;
        string connectionStringWithoutDb;
        
        if (_databaseProvider == DatabaseProviderType.SqlServer)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connectionString);
            dbName = builder.InitialCatalog;
            builder.InitialCatalog = ""; // Remove database to connect to master
            connectionStringWithoutDb = builder.ConnectionString;
        }
        else
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
            dbName = builder.Database;
            builder.Database = ""; // Remove database to connect to postgres
            connectionStringWithoutDb = builder.ConnectionString;
        }

        // Use "monitoring" if database name is empty, otherwise use the one from connection string
        var targetDatabaseName = string.IsNullOrWhiteSpace(dbName) ? defaultDatabaseName : dbName;

        var exists = await Helpers.DatabaseProvider.DatabaseExistsAsync(connectionStringWithoutDb, _databaseProvider, targetDatabaseName);
        
        if (!exists)
        {
            await Helpers.DatabaseProvider.CreateDatabaseAsync(connectionStringWithoutDb, _databaseProvider, targetDatabaseName);
        }
        
        // Update the connection string to use the target database for subsequent connections
        if (_databaseProvider == DatabaseProviderType.SqlServer)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connectionString);
            builder.InitialCatalog = targetDatabaseName;
            _connectionStringWithDatabase = builder.ConnectionString;
        }
        else
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
            builder.Database = targetDatabaseName;
            _connectionStringWithDatabase = builder.ConnectionString;
        }
    }

    private async Task EnsureMonitorTypeTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorType";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            Id INT PRIMARY KEY,
                            Name NVARCHAR(255) NOT NULL
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id INT PRIMARY KEY,
                        Name VARCHAR(255) NOT NULL
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorTableExistsAsync(IDbConnection connection)
    {
        var tableName = "Monitor";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTypeTable = Helpers.DatabaseProvider.FormatTableName("MonitorType", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(255) NOT NULL,
                            MonitorTypeId INT NOT NULL,
                            HeartBeatInterval INT NOT NULL,
                            Retries INT NOT NULL,
                            Status BIT NOT NULL DEFAULT 0,
                            DaysToExpireCert INT NOT NULL DEFAULT 0,
                            Paused BIT NOT NULL DEFAULT 0,
                            MonitorRegion INT NOT NULL DEFAULT 0,
                            MonitorEnvironment INT NOT NULL DEFAULT 0,
                            Tag NVARCHAR(255),
                            FOREIGN KEY (MonitorTypeId) REFERENCES {monitorTypeTable}(Id)
                        );
                        CREATE INDEX IX_Monitor_MonitorTypeId ON {fullTableName} (MonitorTypeId);
                        CREATE INDEX IX_Monitor_Paused ON {fullTableName} (Paused);
                        CREATE INDEX IX_Monitor_MonitorEnvironment ON {fullTableName} (MonitorEnvironment);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        Name VARCHAR(255) NOT NULL,
                        MonitorTypeId INT NOT NULL,
                        HeartBeatInterval INT NOT NULL,
                        Retries INT NOT NULL,
                        Status BOOLEAN NOT NULL DEFAULT FALSE,
                        DaysToExpireCert INT NOT NULL DEFAULT 0,
                        Paused BOOLEAN NOT NULL DEFAULT FALSE,
                        MonitorRegion INT NOT NULL DEFAULT 0,
                        MonitorEnvironment INT NOT NULL DEFAULT 0,
                        Tag VARCHAR(255),
                        FOREIGN KEY (MonitorTypeId) REFERENCES {monitorTypeTable}(Id)
                    );
                    CREATE INDEX IF NOT EXISTS IX_Monitor_MonitorTypeId ON {fullTableName} (MonitorTypeId);
                    CREATE INDEX IF NOT EXISTS IX_Monitor_Paused ON {fullTableName} (Paused);
                    CREATE INDEX IF NOT EXISTS IX_Monitor_MonitorEnvironment ON {fullTableName} (MonitorEnvironment);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorHttpTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorHttp";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            MonitorId INT PRIMARY KEY,
                            CheckCertExpiry BIT NOT NULL DEFAULT 0,
                            IgnoreTlsSsl BIT NOT NULL DEFAULT 0,
                            MaxRedirects INT NOT NULL DEFAULT 0,
                            UrlToCheck NVARCHAR(MAX),
                            Timeout INT NOT NULL DEFAULT 0,
                            MonitorHttpMethod INT NOT NULL DEFAULT 0,
                            Body NVARCHAR(MAX),
                            HeadersJson NVARCHAR(MAX),
                            HttpResponseCodeFrom INT,
                            HttpResponseCodeTo INT,
                            CheckMonitorHttpHeaders BIT NOT NULL DEFAULT 0,
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        MonitorId INT PRIMARY KEY,
                        CheckCertExpiry BOOLEAN NOT NULL DEFAULT FALSE,
                        IgnoreTlsSsl BOOLEAN NOT NULL DEFAULT FALSE,
                        MaxRedirects INT NOT NULL DEFAULT 0,
                        UrlToCheck TEXT,
                        Timeout INT NOT NULL DEFAULT 0,
                        MonitorHttpMethod INT NOT NULL DEFAULT 0,
                        Body TEXT,
                        HeadersJson TEXT,
                        HttpResponseCodeFrom INT,
                        HttpResponseCodeTo INT,
                        CheckMonitorHttpHeaders BOOLEAN NOT NULL DEFAULT FALSE,
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorTcpTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorTcp";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            MonitorId INT PRIMARY KEY,
                            Port INT NOT NULL,
                            IP NVARCHAR(255) NOT NULL,
                            Timeout INT NOT NULL DEFAULT 0,
                            LastStatus BIT NOT NULL DEFAULT 0,
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        MonitorId INT PRIMARY KEY,
                        Port INT NOT NULL,
                        IP VARCHAR(255) NOT NULL,
                        Timeout INT NOT NULL DEFAULT 0,
                        LastStatus BOOLEAN NOT NULL DEFAULT FALSE,
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorK8sTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorK8s";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            MonitorId INT PRIMARY KEY,
                            ClusterName NVARCHAR(255) NOT NULL,
                            KubeConfig NVARCHAR(MAX),
                            LastStatus BIT NOT NULL DEFAULT 0,
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        MonitorId INT PRIMARY KEY,
                        ClusterName VARCHAR(255) NOT NULL,
                        KubeConfig TEXT,
                        LastStatus BOOLEAN NOT NULL DEFAULT FALSE,
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorK8sNodesTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorK8sNodes";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorK8sTable = Helpers.DatabaseProvider.FormatTableName("MonitorK8s", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            MonitorK8sId INT NOT NULL,
                            NodeName NVARCHAR(255) NOT NULL,
                            ContainerRuntimeProblem BIT NOT NULL DEFAULT 0,
                            KernelDeadlock BIT NOT NULL DEFAULT 0,
                            KubeletProblem BIT NOT NULL DEFAULT 0,
                            FrequentUnregisterNetDevice BIT NOT NULL DEFAULT 0,
                            FilesystemCorruptionProblem BIT NOT NULL DEFAULT 0,
                            ReadonlyFilesystem BIT NOT NULL DEFAULT 0,
                            FrequentKubeletRestart BIT NOT NULL DEFAULT 0,
                            FrequentDockerRestart BIT NOT NULL DEFAULT 0,
                            FrequentContainerdRestart BIT NOT NULL DEFAULT 0,
                            MemoryPressure BIT NOT NULL DEFAULT 0,
                            DiskPressure BIT NOT NULL DEFAULT 0,
                            PIDPressure BIT NOT NULL DEFAULT 0,
                            Ready BIT NOT NULL DEFAULT 0,
                            FOREIGN KEY (MonitorK8sId) REFERENCES {monitorK8sTable}(MonitorId) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_MonitorK8sNodes_MonitorK8sId ON {fullTableName} (MonitorK8sId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        MonitorK8sId INT NOT NULL,
                        NodeName VARCHAR(255) NOT NULL,
                        ContainerRuntimeProblem BOOLEAN NOT NULL DEFAULT FALSE,
                        KernelDeadlock BOOLEAN NOT NULL DEFAULT FALSE,
                        KubeletProblem BOOLEAN NOT NULL DEFAULT FALSE,
                        FrequentUnregisterNetDevice BOOLEAN NOT NULL DEFAULT FALSE,
                        FilesystemCorruptionProblem BOOLEAN NOT NULL DEFAULT FALSE,
                        ReadonlyFilesystem BOOLEAN NOT NULL DEFAULT FALSE,
                        FrequentKubeletRestart BOOLEAN NOT NULL DEFAULT FALSE,
                        FrequentDockerRestart BOOLEAN NOT NULL DEFAULT FALSE,
                        FrequentContainerdRestart BOOLEAN NOT NULL DEFAULT FALSE,
                        MemoryPressure BOOLEAN NOT NULL DEFAULT FALSE,
                        DiskPressure BOOLEAN NOT NULL DEFAULT FALSE,
                        PIDPressure BOOLEAN NOT NULL DEFAULT FALSE,
                        Ready BOOLEAN NOT NULL DEFAULT FALSE,
                        FOREIGN KEY (MonitorK8sId) REFERENCES {monitorK8sTable}(MonitorId) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_MonitorK8sNodes_MonitorK8sId ON {fullTableName} (MonitorK8sId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorHistoryTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorHistory";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                            MonitorId INT NOT NULL,
                            Status BIT NOT NULL,
                            TimeStamp DATETIME2 NOT NULL,
                            StatusCode INT NOT NULL DEFAULT 0,
                            ResponseTime INT NOT NULL DEFAULT 0,
                            HttpVersion NVARCHAR(50),
                            ResponseMessage NVARCHAR(MAX),
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_MonitorHistory_MonitorId ON {fullTableName} (MonitorId);
                        CREATE INDEX IX_MonitorHistory_TimeStamp ON {fullTableName} (TimeStamp);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id BIGSERIAL PRIMARY KEY,
                        MonitorId INT NOT NULL,
                        Status BOOLEAN NOT NULL,
                        TimeStamp TIMESTAMP NOT NULL,
                        StatusCode INT NOT NULL DEFAULT 0,
                        ResponseTime INT NOT NULL DEFAULT 0,
                        HttpVersion VARCHAR(50),
                        ResponseMessage TEXT,
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_MonitorHistory_MonitorId ON {fullTableName} (MonitorId);
                    CREATE INDEX IF NOT EXISTS IX_MonitorHistory_TimeStamp ON {fullTableName} (TimeStamp);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorHttpHeadersTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorHttpHeaders";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            MonitorId INT PRIMARY KEY,
                            CacheControl NVARCHAR(255),
                            StrictTransportSecurity NVARCHAR(255),
                            PermissionsPolicy NVARCHAR(255),
                            XFrameOptions NVARCHAR(255),
                            XContentTypeOptions NVARCHAR(255),
                            ReferrerPolicy NVARCHAR(255),
                            ContentSecurityPolicy NVARCHAR(MAX),
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        MonitorId INT PRIMARY KEY,
                        CacheControl VARCHAR(255),
                        StrictTransportSecurity VARCHAR(255),
                        PermissionsPolicy VARCHAR(255),
                        XFrameOptions VARCHAR(255),
                        XContentTypeOptions VARCHAR(255),
                        ReferrerPolicy VARCHAR(255),
                        ContentSecurityPolicy TEXT,
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorAlertTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorAlert";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            MonitorId INT NOT NULL,
                            TimeStamp DATETIME2 NOT NULL,
                            Status BIT NOT NULL,
                            Message NVARCHAR(MAX),
                            Environment INT NOT NULL DEFAULT 0,
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_MonitorAlert_MonitorId ON {fullTableName} (MonitorId);
                        CREATE INDEX IX_MonitorAlert_TimeStamp ON {fullTableName} (TimeStamp);
                        CREATE INDEX IX_MonitorAlert_Status ON {fullTableName} (Status);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        MonitorId INT NOT NULL,
                        TimeStamp TIMESTAMP NOT NULL,
                        Status BOOLEAN NOT NULL,
                        Message TEXT,
                        Environment INT NOT NULL DEFAULT 0,
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_MonitorAlert_MonitorId ON {fullTableName} (MonitorId);
                    CREATE INDEX IF NOT EXISTS IX_MonitorAlert_TimeStamp ON {fullTableName} (TimeStamp);
                    CREATE INDEX IF NOT EXISTS IX_MonitorAlert_Status ON {fullTableName} (Status);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorNotificationTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorNotification";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            MonitorId INT NOT NULL,
                            NotificationId INT NOT NULL,
                            PRIMARY KEY (MonitorId, NotificationId),
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_MonitorNotification_NotificationId ON {fullTableName} (NotificationId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        MonitorId INT NOT NULL,
                        NotificationId INT NOT NULL,
                        PRIMARY KEY (MonitorId, NotificationId),
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_MonitorNotification_NotificationId ON {fullTableName} (NotificationId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorGroupTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorGroup";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(255) NOT NULL
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        Name VARCHAR(255) NOT NULL
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorGroupItemsTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorGroupItems";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            var monitorGroupTable = Helpers.DatabaseProvider.FormatTableName("MonitorGroup", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            MonitorId INT NOT NULL,
                            MonitorGroupId INT NOT NULL,
                            PRIMARY KEY (MonitorId, MonitorGroupId),
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE,
                            FOREIGN KEY (MonitorGroupId) REFERENCES {monitorGroupTable}(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_MonitorGroupItems_MonitorGroupId ON {fullTableName} (MonitorGroupId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        MonitorId INT NOT NULL,
                        MonitorGroupId INT NOT NULL,
                        PRIMARY KEY (MonitorId, MonitorGroupId),
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE,
                        FOREIGN KEY (MonitorGroupId) REFERENCES {monitorGroupTable}(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_MonitorGroupItems_MonitorGroupId ON {fullTableName} (MonitorGroupId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorAgentTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorAgent";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Hostname NVARCHAR(255) NOT NULL,
                            TimeStamp DATETIME2 NOT NULL,
                            IsMaster BIT NOT NULL DEFAULT 0,
                            ListTasks INT NOT NULL DEFAULT 0,
                            Version NVARCHAR(50),
                            MonitorRegion INT
                        );
                        CREATE INDEX IX_MonitorAgent_Hostname ON {fullTableName} (Hostname);
                        CREATE INDEX IX_MonitorAgent_IsMaster ON {fullTableName} (IsMaster);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        Hostname VARCHAR(255) NOT NULL,
                        TimeStamp TIMESTAMP NOT NULL,
                        IsMaster BOOLEAN NOT NULL DEFAULT FALSE,
                        ListTasks INT NOT NULL DEFAULT 0,
                        Version VARCHAR(50),
                        MonitorRegion INT
                    );
                    CREATE INDEX IF NOT EXISTS IX_MonitorAgent_Hostname ON {fullTableName} (Hostname);
                    CREATE INDEX IF NOT EXISTS IX_MonitorAgent_IsMaster ON {fullTableName} (IsMaster);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureMonitorAgentTasksTableExistsAsync(IDbConnection connection)
    {
        var tableName = "MonitorAgentTasks";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var monitorTable = Helpers.DatabaseProvider.FormatTableName("Monitor", _databaseProvider);
            var monitorAgentTable = Helpers.DatabaseProvider.FormatTableName("MonitorAgent", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            MonitorId INT NOT NULL,
                            MonitorAgentId INT NOT NULL,
                            PRIMARY KEY (MonitorId, MonitorAgentId),
                            FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE,
                            FOREIGN KEY (MonitorAgentId) REFERENCES {monitorAgentTable}(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_MonitorAgentTasks_MonitorAgentId ON {fullTableName} (MonitorAgentId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        MonitorId INT NOT NULL,
                        MonitorAgentId INT NOT NULL,
                        PRIMARY KEY (MonitorId, MonitorAgentId),
                        FOREIGN KEY (MonitorId) REFERENCES {monitorTable}(Id) ON DELETE CASCADE,
                        FOREIGN KEY (MonitorAgentId) REFERENCES {monitorAgentTable}(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_MonitorAgentTasks_MonitorAgentId ON {fullTableName} (MonitorAgentId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }
}

