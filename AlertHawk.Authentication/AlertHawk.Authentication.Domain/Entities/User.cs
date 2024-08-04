using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;

[ExcludeFromCodeCoverage]
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Salt { get; set; }
    public bool IsAdmin { get; set; }
    public string? Token { get; set; }
}