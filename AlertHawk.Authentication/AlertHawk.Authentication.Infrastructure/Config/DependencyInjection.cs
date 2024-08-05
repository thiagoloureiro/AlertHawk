using AlertHawk.Authentication.Infrastructure.Interfaces;
using AlertHawk.Authentication.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.Config;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IUsersMonitorGroupRepository, UsersMonitorGroupRepository>();
        services.AddTransient<IUserActionRepository, UserActionRepository>();
        return services;
    }
}