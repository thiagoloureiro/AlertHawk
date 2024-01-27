namespace AlertHawk.Authentication.Domain.Entities;

public class UserCreation
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string RepeatPassword { get; set; }
    public required string UserEmail { get; set; }
    public bool IsAdmin { get; set; }
}