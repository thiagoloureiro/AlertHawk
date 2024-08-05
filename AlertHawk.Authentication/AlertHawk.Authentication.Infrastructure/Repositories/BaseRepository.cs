using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public abstract class BaseRepository
{
    protected readonly string ConnectionString;

    protected BaseRepository(IConfiguration configuration)
    {
        ConnectionString = configuration.GetConnectionString("SqlConnectionString")
                            ?? throw new InvalidOperationException("Connection string 'SqlConnectionString' not found.");
    }

    public async Task<T?> ExecuteQueryFirstOrDefaultAsync<T>(string sql, object parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<IEnumerable<T>?> ExecuteQueryAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.QueryAsync<T>(sql);
    }

    public async Task<IEnumerable<T>?> ExecuteQueryAsyncWithParameters<T>(string sql, object parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.QueryAsync<T>(sql, parameters);
    }

    public async Task ExecuteNonQueryAsync(string sql, object parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<int> Execute(string sql, object parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteAsync(sql, parameters);
    }
}