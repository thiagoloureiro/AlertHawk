using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AlertHawk.Application.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly string? _secret;
    private readonly string? _issuer;
    private readonly string? _audience;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Key"];
        _issuer = configuration["Jwt:Issuer"];
        _audience = configuration["Jwt:Audience"];
    }

    public string GenerateToken(UserDto user)
    {
        var claims = new[]
        {
            new Claim("id", user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim("emailaddress", user.Email),
            new Claim("isAdmin", user.IsAdmin.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret ?? throw new InvalidOperationException("Secret key is undefined.")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}