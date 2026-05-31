using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class AzureAppRegistrationWatchRepository : RepositoryBase, IAzureAppRegistrationWatchRepository
{
    private readonly string _connstring;

    public AzureAppRegistrationWatchRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task InitializeTableIfNotExists()
    {
        await using var db = new SqlConnection(_connstring);
        const string sql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AzureAppRegistrationWatch')
            BEGIN
                CREATE TABLE [dbo].[AzureAppRegistrationWatch](
                    [Id] [int] IDENTITY(1,1) NOT NULL,
                    [ApplicationObjectId] [nvarchar](100) NOT NULL,
                    [ApplicationDisplayName] [nvarchar](255) NOT NULL,
                    [AppId] [nvarchar](100) NOT NULL,
                    [IsEnabled] [bit] NOT NULL CONSTRAINT [DF_AzureAppRegistrationWatch_IsEnabled] DEFAULT (1),
                    [CreatedAt] [datetime] NOT NULL,
                    CONSTRAINT [PK_AzureAppRegistrationWatch] PRIMARY KEY CLUSTERED ([Id] ASC),
                    CONSTRAINT [UQ_AzureAppRegistrationWatch_ObjectId] UNIQUE ([ApplicationObjectId])
                );
            END
            """;
        await db.ExecuteAsync(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<AzureAppRegistrationWatch>> GetAllAsync(bool enabledOnly = true)
    {
        await using var db = new SqlConnection(_connstring);
        var sql = enabledOnly
            ? "SELECT Id, ApplicationObjectId, ApplicationDisplayName, AppId, IsEnabled, CreatedAt FROM [AzureAppRegistrationWatch] WHERE IsEnabled = 1 ORDER BY ApplicationDisplayName"
            : "SELECT Id, ApplicationObjectId, ApplicationDisplayName, AppId, IsEnabled, CreatedAt FROM [AzureAppRegistrationWatch] ORDER BY ApplicationDisplayName";
        return await db.QueryAsync<AzureAppRegistrationWatch>(sql, commandType: CommandType.Text);
    }

    public async Task<AzureAppRegistrationWatch?> GetByObjectIdAsync(string applicationObjectId)
    {
        await using var db = new SqlConnection(_connstring);
        const string sql = """
            SELECT Id, ApplicationObjectId, ApplicationDisplayName, AppId, IsEnabled, CreatedAt
            FROM [AzureAppRegistrationWatch] WHERE ApplicationObjectId = @applicationObjectId
            """;
        return await db.QueryFirstOrDefaultAsync<AzureAppRegistrationWatch>(
            sql, new { applicationObjectId }, commandType: CommandType.Text);
    }

    public async Task<int> AddAsync(AzureAppRegistrationWatch watch)
    {
        await using var db = new SqlConnection(_connstring);
        const string sql = """
            INSERT INTO [AzureAppRegistrationWatch]
                (ApplicationObjectId, ApplicationDisplayName, AppId, IsEnabled, CreatedAt)
            VALUES
                (@ApplicationObjectId, @ApplicationDisplayName, @AppId, @IsEnabled, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);
            """;
        return await db.ExecuteScalarAsync<int>(sql, watch, commandType: CommandType.Text);
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = new SqlConnection(_connstring);
        await db.ExecuteAsync(
            "DELETE FROM [AzureAppRegistrationWatch] WHERE Id = @id",
            new { id },
            commandType: CommandType.Text);
    }

    public async Task DeleteAsync(string applicationObjectId)
    {
        await using var db = new SqlConnection(_connstring);
        await db.ExecuteAsync(
            "DELETE FROM [AzureAppRegistrationWatch] WHERE ApplicationObjectId = @applicationObjectId",
            new { applicationObjectId },
            commandType: CommandType.Text);
    }
}
