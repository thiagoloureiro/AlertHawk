using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;
[ExcludeFromCodeCoverage]
public class UserCreationFromAzure(string username, string email)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = username;
    public string Email { get; set; } = email;
    public bool IsAdmin { get; set; }
}