using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Authentication.Infrastructure.Helpers;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public abstract class BaseRepository
{
    protected readonly string ConnectionString;
    protected readonly DatabaseProviderType DatabaseProvider;

    protected BaseRepository(IConfiguration configuration)
    {
        ConnectionString = configuration.GetConnectionString("SqlConnectionString")
                           ?? throw new InvalidOperationException("Connection string 'SqlConnectionString' not found.");

        var providerString = configuration["DatabaseProvider"] ?? "SqlServer";
        DatabaseProvider = Enum.TryParse<DatabaseProviderType>(providerString, true, out var provider)
            ? provider
            : DatabaseProviderType.SqlServer;
    }

    protected IDbConnection CreateConnection()
    {
        return Helpers.DatabaseProvider.CreateConnection(ConnectionString, DatabaseProvider);
    }

    public async Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(string sql, object parameters)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<IEnumerable<T>?> ExecuteQueryAsync<T>(string sql)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<T>(sql);
    }

    public async Task<IEnumerable<T>?> ExecuteQueryAsyncWithParameters<T>(string sql, object parameters)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<T>(sql, parameters);
    }

    public async Task ExecuteNonQueryAsync(string sql, object parameters)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<int> Execute(string sql, object parameters)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, parameters);
    }
}