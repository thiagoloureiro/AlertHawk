using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace AlertHawk.Notification.Infrastructure.Helpers;

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

        // Ensure NotificationType table exists (should be created first as it's referenced by NotificationItem)
        await EnsureNotificationTypeTableExistsAsync(connection);

        // Ensure NotificationItem table exists
        await EnsureNotificationItemTableExistsAsync(connection);

        // Ensure NotificationEmailSmtp table exists
        await EnsureNotificationEmailSmtpTableExistsAsync(connection);

        // Ensure NotificationTeams table exists
        await EnsureNotificationTeamsTableExistsAsync(connection);

        // Ensure NotificationSlack table exists
        await EnsureNotificationSlackTableExistsAsync(connection);

        // Ensure NotificationTelegram table exists
        await EnsureNotificationTelegramTableExistsAsync(connection);

        // Ensure NotificationWebHook table exists
        await EnsureNotificationWebHookTableExistsAsync(connection);

        // Ensure NotificationLog table exists
        await EnsureNotificationLogTableExistsAsync(connection);

        // Ensure NotificationMonitorGroup table exists
        await EnsureNotificationMonitorGroupTableExistsAsync(connection);
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        const string defaultDatabaseName = "notification";
        
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

        // Use "notification" if database name is empty, otherwise use the one from connection string
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

    private async Task EnsureNotificationTypeTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationType";
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
                            Name NVARCHAR(255) NOT NULL,
                            Description NVARCHAR(MAX)
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id INT PRIMARY KEY,
                        Name VARCHAR(255) NOT NULL,
                        Description TEXT
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationItemTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationItem";
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
                            MonitorGroupId INT NOT NULL,
                            Name NVARCHAR(255) NOT NULL,
                            Description NVARCHAR(MAX),
                            NotificationTypeId INT NOT NULL,
                            FOREIGN KEY (NotificationTypeId) REFERENCES {Helpers.DatabaseProvider.FormatTableName("NotificationType", _databaseProvider)}(Id)
                        );
                        CREATE INDEX IX_NotificationItem_NotificationTypeId ON {fullTableName} (NotificationTypeId);
                        CREATE INDEX IX_NotificationItem_MonitorGroupId ON {fullTableName} (MonitorGroupId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        MonitorGroupId INT NOT NULL,
                        Name VARCHAR(255) NOT NULL,
                        Description TEXT,
                        NotificationTypeId INT NOT NULL,
                        FOREIGN KEY (NotificationTypeId) REFERENCES {Helpers.DatabaseProvider.FormatTableName("NotificationType", _databaseProvider)}(Id)
                    );
                    CREATE INDEX IF NOT EXISTS IX_NotificationItem_NotificationTypeId ON {fullTableName} (NotificationTypeId);
                    CREATE INDEX IF NOT EXISTS IX_NotificationItem_MonitorGroupId ON {fullTableName} (MonitorGroupId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationEmailSmtpTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationEmailSmtp";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var notificationItemTable = Helpers.DatabaseProvider.FormatTableName("NotificationItem", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            NotificationId INT PRIMARY KEY,
                            FromEmail NVARCHAR(255) NOT NULL,
                            ToEmail NVARCHAR(255),
                            HostName NVARCHAR(255),
                            Port INT,
                            Username NVARCHAR(255),
                            Password NVARCHAR(MAX),
                            ToCCEmail NVARCHAR(255),
                            ToBCCEmail NVARCHAR(255),
                            EnableSSL BIT NOT NULL DEFAULT 0,
                            Subject NVARCHAR(MAX),
                            Body NVARCHAR(MAX),
                            IsHtmlBody BIT NOT NULL DEFAULT 0,
                            FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        NotificationId INT PRIMARY KEY,
                        FromEmail VARCHAR(255) NOT NULL,
                        ToEmail VARCHAR(255),
                        HostName VARCHAR(255),
                        Port INT,
                        Username VARCHAR(255),
                        Password TEXT,
                        ToCCEmail VARCHAR(255),
                        ToBCCEmail VARCHAR(255),
                        EnableSSL BOOLEAN NOT NULL DEFAULT FALSE,
                        Subject TEXT,
                        Body TEXT,
                        IsHtmlBody BOOLEAN NOT NULL DEFAULT FALSE,
                        FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationTeamsTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationTeams";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var notificationItemTable = Helpers.DatabaseProvider.FormatTableName("NotificationItem", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            NotificationId INT PRIMARY KEY,
                            WebHookUrl NVARCHAR(MAX) NOT NULL,
                            FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        NotificationId INT PRIMARY KEY,
                        WebHookUrl TEXT NOT NULL,
                        FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationSlackTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationSlack";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var notificationItemTable = Helpers.DatabaseProvider.FormatTableName("NotificationItem", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            NotificationId INT PRIMARY KEY,
                            WebHookUrl NVARCHAR(MAX) NOT NULL,
                            Channel NVARCHAR(255),
                            FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        NotificationId INT PRIMARY KEY,
                        WebHookUrl TEXT NOT NULL,
                        Channel VARCHAR(255),
                        FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationTelegramTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationTelegram";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var notificationItemTable = Helpers.DatabaseProvider.FormatTableName("NotificationItem", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            NotificationId INT PRIMARY KEY,
                            ChatId BIGINT NOT NULL,
                            TelegramBotToken NVARCHAR(MAX) NOT NULL,
                            FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        NotificationId INT PRIMARY KEY,
                        ChatId BIGINT NOT NULL,
                        TelegramBotToken TEXT NOT NULL,
                        FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationWebHookTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationWebHook";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var notificationItemTable = Helpers.DatabaseProvider.FormatTableName("NotificationItem", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            NotificationId INT PRIMARY KEY,
                            Message NVARCHAR(MAX),
                            WebHookUrl NVARCHAR(MAX),
                            Body NVARCHAR(MAX),
                            HeadersJson NVARCHAR(MAX),
                            FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                        );
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        NotificationId INT PRIMARY KEY,
                        Message TEXT,
                        WebHookUrl TEXT,
                        Body TEXT,
                        HeadersJson TEXT,
                        FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                    );",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationLogTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationLog";
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
                            TimeStamp DATETIME2 NOT NULL,
                            NotificationTypeId INT NOT NULL,
                            Message NVARCHAR(MAX)
                        );
                        CREATE INDEX IX_NotificationLog_TimeStamp ON {fullTableName} (TimeStamp);
                        CREATE INDEX IX_NotificationLog_NotificationTypeId ON {fullTableName} (NotificationTypeId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        TimeStamp TIMESTAMP NOT NULL,
                        NotificationTypeId INT NOT NULL,
                        Message TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_NotificationLog_TimeStamp ON {fullTableName} (TimeStamp);
                    CREATE INDEX IF NOT EXISTS IX_NotificationLog_NotificationTypeId ON {fullTableName} (NotificationTypeId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureNotificationMonitorGroupTableExistsAsync(IDbConnection connection)
    {
        var tableName = "NotificationMonitorGroup";
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, _databaseProvider);

        if (!exists)
        {
            var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, _databaseProvider);
            var notificationItemTable = Helpers.DatabaseProvider.FormatTableName("NotificationItem", _databaseProvider);
            string createTableSql = _databaseProvider switch
            {
                DatabaseProviderType.SqlServer => $@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{fullTableName}') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE {fullTableName} (
                            NotificationId INT NOT NULL,
                            MonitorGroupId INT NOT NULL,
                            PRIMARY KEY (NotificationId, MonitorGroupId),
                            FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_NotificationMonitorGroup_MonitorGroupId ON {fullTableName} (MonitorGroupId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        NotificationId INT NOT NULL,
                        MonitorGroupId INT NOT NULL,
                        PRIMARY KEY (NotificationId, MonitorGroupId),
                        FOREIGN KEY (NotificationId) REFERENCES {notificationItemTable}(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_NotificationMonitorGroup_MonitorGroupId ON {fullTableName} (MonitorGroupId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }
}

