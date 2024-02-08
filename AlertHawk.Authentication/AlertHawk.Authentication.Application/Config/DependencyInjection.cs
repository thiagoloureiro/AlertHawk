using System.Diagnostics.CodeAnalysis;
using AlertHawk.Authentication.Application.Interfaces;
using AlertHawk.Authentication.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Authentication.Application.Config;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        return services;
    }
}