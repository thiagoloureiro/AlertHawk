using System.Data;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Infrastructure.Helpers;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class RepositoryBase
{
    private readonly IConfiguration _configuration;
    protected readonly string ConnectionString;
    protected readonly DatabaseProviderType DatabaseProvider;

    public RepositoryBase(IConfiguration configuration)
    {
        _configuration = configuration;
        ConnectionString = _configuration.GetConnectionString("SqlConnectionString")
                          ?? throw new InvalidOperationException("Connection string 'SqlConnectionString' not found.");

        var providerString = _configuration["DatabaseProvider"] ?? "SqlServer";
        DatabaseProvider = Enum.TryParse<DatabaseProviderType>(providerString, true, out var provider)
            ? provider
            : DatabaseProviderType.SqlServer;
    }

    protected string GetConnectionString()
    {
        return ConnectionString;
    }

    protected IDbConnection CreateConnection()
    {
        return Helpers.DatabaseProvider.CreateConnection(ConnectionString, DatabaseProvider);
    }
}