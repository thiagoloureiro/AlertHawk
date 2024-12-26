using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AlertHawk.Application.Services;

[ExcludeFromCodeCoverage]
public class JwtTokenService : IJwtTokenService
{
    private readonly string? _secret;
    private readonly string? _issuers;
    private readonly string? _audiences;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Key"];
        _issuers = configuration["Jwt:Issuers"];
        _audiences = configuration["Jwt:Audiences"];
    }

    public string GenerateToken(UserDto? user)
    {
        var claims = new[]
        {
            new Claim("id", user?.Id.ToString() ?? string.Empty),
            new Claim("givenname", user.Username),
            new Claim("surname", user.Username),
            new Claim("emailaddress", user.Email),
            new Claim("isAdmin", user.IsAdmin.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret ?? throw new InvalidOperationException("Secret key is undefined.")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _issuers?.Split(",")[0],
            _audiences?.Split(",")[0],
            claims,
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}