using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class UserClustersRepository : BaseRepository, IUserClustersRepository
{
    public UserClustersRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task EnsureTableExistsAsync()
    {
        const string checkTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserClusters]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[UserClusters] (
                    [UserId] UNIQUEIDENTIFIER NOT NULL,
                    [ClusterName] NVARCHAR(255) NOT NULL,
                    PRIMARY KEY ([UserId], [ClusterName])
                );
                CREATE INDEX IX_UserClusters_UserId ON [dbo].[UserClusters] ([UserId]);
            END";

        await ExecuteNonQueryAsync(checkTableSql, new { });
    }

    public async Task CreateAsync(UserClusters userCluster)
    {
        const string sql = "INSERT INTO [dbo].[UserClusters] ([UserId], [ClusterName]) VALUES (@UserId, @ClusterName)";
        await ExecuteNonQueryAsync(sql, new { userCluster.UserId, userCluster.ClusterName });
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        const string sql = "DELETE FROM [dbo].[UserClusters] WHERE [UserId] = @userId";
        await ExecuteNonQueryAsync(sql, new { userId });
    }

    public async Task<IEnumerable<UserClusters>> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT [UserId], [ClusterName] FROM [dbo].[UserClusters] WHERE [UserId] = @userId";
        return await ExecuteQueryAsyncWithParameters<UserClusters>(sql, new { userId }) ?? Array.Empty<UserClusters>();
    }
}

