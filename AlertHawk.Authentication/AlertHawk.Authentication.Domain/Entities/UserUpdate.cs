using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;
[ExcludeFromCodeCoverage]
public class UserUpdate
{
    [MinLength(6, ErrorMessage = "Username must be at least 6 characters long.")]
    public string? Username { get; set; }
    
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string? NewPassword { get; set; }
    
    public string? RepeatNewPassword { get; set; }
    
    [EmailAddress]
    public string? UserEmail { get; set; }
}