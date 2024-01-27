namespace AlertHawk.Authentication.Domain.Entities;

public class UserAuth
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}