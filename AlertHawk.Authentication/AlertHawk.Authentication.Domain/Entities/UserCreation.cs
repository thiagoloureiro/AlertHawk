namespace AlertHawk.Authentication.Domain.Entities;

public class UserCreation
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string RepeatPassword { get; set; }
    public string UserEmail { get; set; }
    public bool IsAdmin { get; set; }
}