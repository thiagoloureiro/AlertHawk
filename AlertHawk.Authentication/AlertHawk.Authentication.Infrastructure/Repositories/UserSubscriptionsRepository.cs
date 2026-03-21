using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class UserSubscriptionsRepository : BaseRepository, IUserSubscriptionsRepository
{
    public UserSubscriptionsRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task EnsureTableExistsAsync()
    {
        const string checkTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserSubscriptions]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[UserSubscriptions] (
                    [UserId] UNIQUEIDENTIFIER NOT NULL,
                    [SubscriptionId] UNIQUEIDENTIFIER NOT NULL,
                    PRIMARY KEY ([UserId], [SubscriptionId])
                );
                CREATE INDEX IX_UserSubscriptions_UserId ON [dbo].[UserSubscriptions] ([UserId]);
            END
            ELSE IF EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[UserSubscriptions]') AND name = N'SubscriptionName')
            BEGIN
                ALTER TABLE [dbo].[UserSubscriptions] DROP COLUMN [SubscriptionName];
            END";

        await ExecuteNonQueryAsync(checkTableSql, new { });
    }

    public async Task CreateAsync(UserSubscriptions userSubscription)
    {
        const string sql =
            "INSERT INTO [dbo].[UserSubscriptions] ([UserId], [SubscriptionId]) VALUES (@UserId, @SubscriptionId)";
        await ExecuteNonQueryAsync(sql,
            new { userSubscription.UserId, userSubscription.SubscriptionId });
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        const string sql = "DELETE FROM [dbo].[UserSubscriptions] WHERE [UserId] = @userId";
        await ExecuteNonQueryAsync(sql, new { userId });
    }

    public async Task<IEnumerable<UserSubscriptions>> GetByUserIdAsync(Guid userId)
    {
        const string sql =
            "SELECT [UserId], [SubscriptionId] FROM [dbo].[UserSubscriptions] WHERE [UserId] = @userId";
        return await ExecuteQueryAsyncWithParameters<UserSubscriptions>(sql, new { userId }) ??
               Array.Empty<UserSubscriptions>();
    }
}
