namespace AlertHawk.Authentication.Domain.Entities;

public class UserCreationFromAzure(string username, string email)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = username;
    public string Email { get; set; } = email;
}