
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Application.Interfaces;
using AlertHawk.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Authentication.Tests;
[ExcludeFromCodeCoverage]
public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, false)
            .AddEnvironmentVariables()
           .Build();
        services.AddTransient<IGetOrCreateUserService, GetOrCreateUserService>();
    }
}