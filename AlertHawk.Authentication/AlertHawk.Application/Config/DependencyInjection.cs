using System.Diagnostics.CodeAnalysis;
using AlertHawk.Application.Interfaces;
using AlertHawk.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Application.Config;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        services.AddTransient<IUsersMonitorGroupService, UsersMonitorGroupService>();
        services.AddTransient<IUserActionService, UserActionService>();
        services.AddTransient<IGetOrCreateUserService, GetOrCreateUserService>();
        return services;
    }
}