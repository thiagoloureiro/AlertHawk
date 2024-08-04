using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities
{
    [ExcludeFromCodeCoverage]
    public class UsersMonitorGroup
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public int GroupMonitorId { get; set; }
    }
}