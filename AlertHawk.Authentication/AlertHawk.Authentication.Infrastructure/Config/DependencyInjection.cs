using AlertHawk.Authentication.Infrastructure.Interfaces;
using AlertHawk.Authentication.Infrastructure.Repositories;
using AlertHawk.Authentication.Infrastructure.Helpers;
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
        services.AddTransient<IUserClustersRepository, UserClustersRepository>();
        services.AddTransient<DatabaseInitializer>();
        return services;
    }
}