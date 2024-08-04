using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserAuth
{
    public string Username { get; set; }
    public string Password { get; set; }
}