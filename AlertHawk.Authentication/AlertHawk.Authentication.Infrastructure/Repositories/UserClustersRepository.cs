using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AlertHawk.Authentication.Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using Dapper;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class UserClustersRepository : BaseRepository, IUserClustersRepository
{
    public UserClustersRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task EnsureTableExistsAsync()
    {
        var tableName = "UserClusters";
        var fullTableName = Helpers.DatabaseProvider.FormatTableName(tableName, DatabaseProvider);

        using var connection = CreateConnection();
        var exists = await Helpers.DatabaseProvider.TableExistsAsync(connection, tableName, DatabaseProvider);

        if (!exists)
        {
            string createTableSql = DatabaseProvider switch
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
                _ => throw new NotSupportedException($"Database provider '{DatabaseProvider}' is not supported.")
            };

            await connection.ExecuteAsync(createTableSql);
        }
    }

    public async Task CreateAsync(UserClusters userCluster)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UserClusters", DatabaseProvider);
        var sql = $"INSERT INTO {tableName} (UserId, ClusterName) VALUES (@UserId, @ClusterName)";
        await ExecuteNonQueryAsync(sql, new { userCluster.UserId, userCluster.ClusterName });
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UserClusters", DatabaseProvider);
        var sql = $"DELETE FROM {tableName} WHERE UserId = @userId";
        await ExecuteNonQueryAsync(sql, new { userId });
    }

    public async Task<IEnumerable<UserClusters>> GetByUserIdAsync(Guid userId)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UserClusters", DatabaseProvider);
        var sql = $"SELECT UserId, ClusterName FROM {tableName} WHERE UserId = @userId";
        return await ExecuteQueryAsyncWithParameters<UserClusters>(sql, new { userId }) ?? Array.Empty<UserClusters>();
    }
}