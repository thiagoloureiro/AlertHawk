using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace AlertHawk.Authentication.Infrastructure.Helpers;

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

        // Ensure Users table exists
        await EnsureUsersTableExistsAsync(connection);

        // Ensure UserAction table exists
        await EnsureUserActionTableExistsAsync(connection);

        // Ensure UserClusters table exists (this is also handled by UserClustersRepository, but we'll ensure it here too)
        await EnsureUserClustersTableExistsAsync(connection);

        // Ensure UsersMonitorGroup table exists
        await EnsureUsersMonitorGroupTableExistsAsync(connection);

        // Ensure UserDeviceToken table exists
        await EnsureUserDeviceTokenTableExistsAsync(connection);
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        const string defaultDatabaseName = "authentication";
        
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

        // Use "authentication" if database name is empty, otherwise use the one from connection string
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

    private async Task EnsureUsersTableExistsAsync(IDbConnection connection)
    {
        var tableName = "Users";
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
                            Id UNIQUEIDENTIFIER PRIMARY KEY,
                            Username NVARCHAR(255) NOT NULL,
                            Email NVARCHAR(255) NOT NULL,
                            Password NVARCHAR(255),
                            Salt NVARCHAR(255),
                            IsAdmin BIT NOT NULL DEFAULT 0,
                            Token NVARCHAR(MAX),
                            CreatedAt DATETIME2,
                            UpdatedAt DATETIME2,
                            LastLogon DATETIME2
                        );
                        CREATE INDEX IX_Users_Email ON {fullTableName} (Email);
                        CREATE INDEX IX_Users_Username ON {fullTableName} (Username);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id UUID PRIMARY KEY,
                        Username VARCHAR(255) NOT NULL,
                        Email VARCHAR(255) NOT NULL,
                        Password VARCHAR(255),
                        Salt VARCHAR(255),
                        IsAdmin BOOLEAN NOT NULL DEFAULT FALSE,
                        Token TEXT,
                        CreatedAt TIMESTAMP,
                        UpdatedAt TIMESTAMP,
                        LastLogon TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS IX_Users_Email ON {fullTableName} (Email);
                    CREATE INDEX IF NOT EXISTS IX_Users_Username ON {fullTableName} (Username);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureUserActionTableExistsAsync(IDbConnection connection)
    {
        var tableName = "UserAction";
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
                            UserId UNIQUEIDENTIFIER NOT NULL,
                            Action NVARCHAR(MAX),
                            TimeStamp DATETIME2 NOT NULL
                        );
                        CREATE INDEX IX_UserAction_UserId ON {fullTableName} (UserId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id SERIAL PRIMARY KEY,
                        UserId UUID NOT NULL,
                        Action TEXT,
                        TimeStamp TIMESTAMP NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_UserAction_UserId ON {fullTableName} (UserId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureUserClustersTableExistsAsync(IDbConnection connection)
    {
        var tableName = "UserClusters";
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
                            UserId UNIQUEIDENTIFIER NOT NULL,
                            ClusterName NVARCHAR(255) NOT NULL,
                            PRIMARY KEY (UserId, ClusterName)
                        );
                        CREATE INDEX IX_UserClusters_UserId ON {fullTableName} (UserId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        UserId UUID NOT NULL,
                        ClusterName VARCHAR(255) NOT NULL,
                        PRIMARY KEY (UserId, ClusterName)
                    );
                    CREATE INDEX IF NOT EXISTS IX_UserClusters_UserId ON {fullTableName} (UserId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureUsersMonitorGroupTableExistsAsync(IDbConnection connection)
    {
        var tableName = "UsersMonitorGroup";
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
                            Id UNIQUEIDENTIFIER PRIMARY KEY,
                            UserId UNIQUEIDENTIFIER NOT NULL,
                            GroupMonitorId INT NOT NULL
                        );
                        CREATE INDEX IX_UsersMonitorGroup_UserId ON {fullTableName} (UserId);
                        CREATE INDEX IX_UsersMonitorGroup_GroupMonitorId ON {fullTableName} (GroupMonitorId);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id UUID PRIMARY KEY,
                        UserId UUID NOT NULL,
                        GroupMonitorId INT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_UsersMonitorGroup_UserId ON {fullTableName} (UserId);
                    CREATE INDEX IF NOT EXISTS IX_UsersMonitorGroup_GroupMonitorId ON {fullTableName} (GroupMonitorId);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    private async Task EnsureUserDeviceTokenTableExistsAsync(IDbConnection connection)
    {
        var tableName = "UserDeviceToken";
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
                            Id UNIQUEIDENTIFIER NOT NULL,
                            DeviceToken NVARCHAR(MAX) NOT NULL,
                            PRIMARY KEY (Id, DeviceToken)
                        );
                        CREATE INDEX IX_UserDeviceToken_Id ON {fullTableName} (Id);
                    END",
                DatabaseProviderType.PostgreSQL => $@"
                    CREATE TABLE IF NOT EXISTS {fullTableName} (
                        Id UUID NOT NULL,
                        DeviceToken TEXT NOT NULL,
                        PRIMARY KEY (Id, DeviceToken)
                    );
                    CREATE INDEX IF NOT EXISTS IX_UserDeviceToken_Id ON {fullTableName} (Id);",
                _ => throw new NotSupportedException($"Database provider '{_databaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }
}