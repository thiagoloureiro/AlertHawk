namespace AlertHawk.Authentication.Domain.Entities;

public class UserAuth
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}