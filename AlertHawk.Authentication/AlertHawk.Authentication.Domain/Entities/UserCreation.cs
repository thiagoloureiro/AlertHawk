using System.ComponentModel.DataAnnotations;

namespace AlertHawk.Authentication.Domain.Entities;

public class UserCreation
{
    [Required]
    [MinLength(6, ErrorMessage = "Username must be at least 6 characters long.")]
    public required string Username { get; set; }
    
    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public required string Password { get; set; }
    
    [Required]
    public required string RepeatPassword { get; set; }
    
    [Required]
    [EmailAddress]
    public required string UserEmail { get; set; }

    public bool IsAdmin { get; set; } = false;
}