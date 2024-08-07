using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserCreation
{
    [Required]
    [MinLength(3, ErrorMessage = "Username must be at least 6 characters long.")]
    public string Username { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; }

    [Required]
    public string RepeatPassword { get; set; }

    [Required]
    [EmailAddress]
    public string UserEmail { get; set; }
}