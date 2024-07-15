namespace AlertHawk.Authentication.Domain.Entities;

public class UserPassword
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string Email { get; set; }
}