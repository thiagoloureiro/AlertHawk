using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;
[ExcludeFromCodeCoverage]
public class UserAction
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? Action { get; set; }
    public DateTime TimeStamp { get; set; }
}