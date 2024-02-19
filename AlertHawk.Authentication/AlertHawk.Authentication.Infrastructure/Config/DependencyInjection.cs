using System.Diagnostics.CodeAnalysis;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AlertHawk.Authentication.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Authentication.Infrastructure.Config;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUsersMonitorGroupRepository, UsersMonitorGroupRepository>();
        return services;
    }
}