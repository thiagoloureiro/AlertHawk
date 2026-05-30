using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class AzureAppSecretRepository : RepositoryBase, IAzureAppSecretRepository
{
    private readonly string _connstring;

    public AzureAppSecretRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task InitializeTableIfNotExists()
    {
        await using var db = new SqlConnection(_connstring);
        const string sql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AzureAppSecret')
            BEGIN
                CREATE TABLE [dbo].[AzureAppSecret](
                    [Id] [int] IDENTITY(1,1) NOT NULL,
                    [ApplicationObjectId] [nvarchar](100) NOT NULL,
                    [ApplicationDisplayName] [nvarchar](255) NULL,
                    [AppId] [nvarchar](100) NULL,
                    [KeyId] [uniqueidentifier] NOT NULL,
                    [SecretDisplayName] [nvarchar](255) NULL,
                    [EndDateTime] [datetimeoffset] NOT NULL,
                    [DaysUntilExpiry] [int] NOT NULL,
                    [IsExpiring] [bit] NOT NULL,
                    [LastChecked] [datetime] NOT NULL,
                    CONSTRAINT [PK_AzureAppSecret] PRIMARY KEY CLUSTERED ([Id] ASC),
                    CONSTRAINT [UQ_AzureAppSecret_App_Key] UNIQUE ([ApplicationObjectId], [KeyId])
                );
            END
            """;
        await db.ExecuteAsync(sql, commandType: CommandType.Text);
    }

    public async Task UpsertAsync(AzureAppSecret secret)
    {
        await using var db = new SqlConnection(_connstring);
        const string sql = """
            MERGE [AzureAppSecret] AS target
            USING (SELECT @ApplicationObjectId AS ApplicationObjectId, @KeyId AS KeyId) AS source
            ON target.ApplicationObjectId = source.ApplicationObjectId AND target.KeyId = source.KeyId
            WHEN MATCHED THEN
                UPDATE SET
                    ApplicationDisplayName = @ApplicationDisplayName,
                    AppId = @AppId,
                    SecretDisplayName = @SecretDisplayName,
                    EndDateTime = @EndDateTime,
                    DaysUntilExpiry = @DaysUntilExpiry,
                    IsExpiring = @IsExpiring,
                    LastChecked = @LastChecked
            WHEN NOT MATCHED THEN
                INSERT (ApplicationObjectId, ApplicationDisplayName, AppId, KeyId, SecretDisplayName,
                    EndDateTime, DaysUntilExpiry, IsExpiring, LastChecked)
                VALUES (@ApplicationObjectId, @ApplicationDisplayName, @AppId, @KeyId, @SecretDisplayName,
                    @EndDateTime, @DaysUntilExpiry, @IsExpiring, @LastChecked);
            """;
        await db.ExecuteAsync(sql, secret, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<AzureAppSecret>> GetAllAsync()
    {
        await using var db = new SqlConnection(_connstring);
        const string sql = """
            SELECT Id, ApplicationObjectId, ApplicationDisplayName, AppId, KeyId, SecretDisplayName,
                EndDateTime, DaysUntilExpiry, IsExpiring, LastChecked
            FROM [AzureAppSecret]
            """;
        return await db.QueryAsync<AzureAppSecret>(sql, commandType: CommandType.Text);
    }
}
