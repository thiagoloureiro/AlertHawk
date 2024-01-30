using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Microsoft.Extensions.Configuration;

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
}