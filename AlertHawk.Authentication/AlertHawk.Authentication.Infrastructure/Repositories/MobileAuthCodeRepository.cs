using AlertHawk.Authentication.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class MobileAuthCodeRepository : BaseRepository, IMobileAuthCodeRepository
{
    public MobileAuthCodeRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task EnsureTableExistsAsync()
    {
        const string checkTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MobileAuthCodes]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[MobileAuthCodes] (
                    [Code] NVARCHAR(16) NOT NULL PRIMARY KEY,
                    [UserId] UNIQUEIDENTIFIER NOT NULL,
                    [ExpiresAt] DATETIME2 NOT NULL,
                    [Consumed] BIT NOT NULL CONSTRAINT DF_MobileAuthCodes_Consumed DEFAULT 0,
                    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_MobileAuthCodes_CreatedAt DEFAULT SYSUTCDATETIME()
                );
                CREATE INDEX IX_MobileAuthCodes_UserId ON [dbo].[MobileAuthCodes] ([UserId]);
            END";

        await ExecuteNonQueryAsync(checkTableSql, new { });
    }

    public async Task CreateAsync(string code, Guid userId, DateTime expiresAt)
    {
        const string sql = @"
            DELETE FROM [dbo].[MobileAuthCodes]
            WHERE [ExpiresAt] < DATEADD(day, -1, SYSUTCDATETIME());

            UPDATE [dbo].[MobileAuthCodes]
            SET [Consumed] = 1
            WHERE [UserId] = @UserId AND [Consumed] = 0;

            INSERT INTO [dbo].[MobileAuthCodes] ([Code], [UserId], [ExpiresAt], [Consumed])
            VALUES (@Code, @UserId, @ExpiresAt, 0);";

        await ExecuteNonQueryAsync(sql, new { Code = code, UserId = userId, ExpiresAt = expiresAt });
    }

    public async Task<Guid?> ConsumeAsync(string code)
    {
        const string sql = @"
            UPDATE [dbo].[MobileAuthCodes]
            SET [Consumed] = 1
            OUTPUT INSERTED.[UserId]
            WHERE [Code] = @Code AND [Consumed] = 0 AND [ExpiresAt] > SYSUTCDATETIME();";

        return await ExecuteQueryFirstOrDefaultAsync<Guid?>(sql, new { Code = code });
    }
}
