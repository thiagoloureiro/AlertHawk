using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace AlertHawk.Monitoring.Infrastructure.Helpers;

public enum DatabaseProviderType
{
    SqlServer,
    PostgreSQL
}

[ExcludeFromCodeCoverage]
public static class DatabaseProvider
{
    public static IDbConnection CreateConnection(string connectionString, DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => new SqlConnection(connectionString),
            DatabaseProviderType.PostgreSQL => new NpgsqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database provider '{providerType}' is not supported.")
        };
    }

    public static async Task<bool> TableExistsAsync(IDbConnection connection, string tableName, DatabaseProviderType providerType)
    {
        string sql;
        object parameters;
        
        switch (providerType)
        {
            case DatabaseProviderType.SqlServer:
                sql = @"
                    SELECT COUNT(*) 
                    FROM sys.objects 
                    WHERE object_id = OBJECT_ID(@tableName) 
                    AND type in (N'U')";
                parameters = new { tableName = $"[dbo].[{tableName}]" };
                break;
            case DatabaseProviderType.PostgreSQL:
                // PostgreSQL: Use case-insensitive comparison since table names might be stored in different cases
                // When using quoted identifiers, PostgreSQL preserves case; unquoted are lowercased
                sql = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND LOWER(table_name) = LOWER(@tableName)";
                parameters = new { tableName = tableName };
                break;
            default:
                throw new NotSupportedException($"Database provider '{providerType}' is not supported.");
        }

        var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
        return count > 0;
    }

    public static async Task<bool> DatabaseExistsAsync(string connectionString, DatabaseProviderType providerType, string databaseName)
    {
        string masterConnectionString;
        string sql;
        
        switch (providerType)
        {
            case DatabaseProviderType.SqlServer:
                // Connect to master database
                var sqlBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                sqlBuilder.InitialCatalog = "master";
                masterConnectionString = sqlBuilder.ConnectionString;
                
                sql = @"
                    SELECT COUNT(*) 
                    FROM sys.databases 
                    WHERE name = @databaseName";
                break;
            case DatabaseProviderType.PostgreSQL:
                // Connect to postgres database
                var pgBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                pgBuilder.Database = "postgres";
                masterConnectionString = pgBuilder.ConnectionString;
                
                sql = @"
                    SELECT COUNT(*) 
                    FROM pg_database 
                    WHERE datname = @databaseName";
                break;
            default:
                throw new NotSupportedException($"Database provider '{providerType}' is not supported.");
        }
        using var connection = CreateConnection(masterConnectionString, providerType);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { databaseName });
        return count > 0;
    }

    public static async Task CreateDatabaseAsync(string connectionString, DatabaseProviderType providerType, string databaseName)
    {
        string masterConnectionString;
        string sql;
        
        switch (providerType)
        {
            case DatabaseProviderType.SqlServer:
                // Connect to master database
                var sqlBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                sqlBuilder.InitialCatalog = "master";
                masterConnectionString = sqlBuilder.ConnectionString;
                
                sql = $"CREATE DATABASE [{databaseName}]";
                break;
            case DatabaseProviderType.PostgreSQL:
                // Connect to postgres database
                var pgBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                pgBuilder.Database = "postgres";
                masterConnectionString = pgBuilder.ConnectionString;
                
                sql = $"CREATE DATABASE \"{databaseName}\"";
                break;
            default:
                throw new NotSupportedException($"Database provider '{providerType}' is not supported.");
        }
        using var connection = CreateConnection(masterConnectionString, providerType);
        await connection.ExecuteAsync(sql);
    }

    public static string GetNewGuidFunction(DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => "NEWID()",
            DatabaseProviderType.PostgreSQL => "gen_random_uuid()",
            _ => throw new NotSupportedException($"Database provider '{providerType}' is not supported.")
        };
    }

    public static string FormatTableName(string tableName, DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => $"[dbo].[{tableName}]",
            DatabaseProviderType.PostgreSQL => $"\"{tableName}\"",
            _ => tableName
        };
    }

    public static string FormatColumnName(string columnName, DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => $"[{columnName}]",
            DatabaseProviderType.PostgreSQL => $"\"{columnName}\"",
            _ => columnName
        };
    }

    public static string FormatStringConcatenation(string left, string right, DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => $"{left} + {right}",
            DatabaseProviderType.PostgreSQL => $"{left} || {right}",
            _ => throw new NotSupportedException($"Database provider '{providerType}' is not supported.")
        };
    }

    public static string FormatDateAdd(string interval, string number, string date, DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => $"DATEADD({interval}, -{number}, {date})",
            DatabaseProviderType.PostgreSQL => $"{date} - INTERVAL '{number} {interval}'",
            _ => throw new NotSupportedException($"Database provider '{providerType}' is not supported.")
        };
    }

    public static string GetCurrentDateFunction(DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => "GETDATE()",
            DatabaseProviderType.PostgreSQL => "CURRENT_TIMESTAMP",
            _ => throw new NotSupportedException($"Database provider '{providerType}' is not supported.")
        };
    }
}

