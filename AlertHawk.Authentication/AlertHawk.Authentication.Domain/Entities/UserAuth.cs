using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;
[ExcludeFromCodeCoverage]
public class UserAuth
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}