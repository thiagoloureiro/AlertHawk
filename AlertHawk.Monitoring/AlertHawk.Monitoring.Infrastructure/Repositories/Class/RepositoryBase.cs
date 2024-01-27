using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class RepositoryBase
{
    private readonly IConfiguration _configuration;

    public RepositoryBase(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected string GetConnectionString()
    {
        var strConnection = _configuration.GetSection("ConnectionStrings").GetSection("SqlConnectionString").Value;
        return strConnection ?? "";
    }
}