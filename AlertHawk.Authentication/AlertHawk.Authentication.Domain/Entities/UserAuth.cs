using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserAuth
{
    public string Email { get; set; }
    public string Password { get; set; }
}