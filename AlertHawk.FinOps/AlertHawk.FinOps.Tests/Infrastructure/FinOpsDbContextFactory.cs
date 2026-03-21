using FinOpsToolSample.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertHawk.FinOps.Tests.Infrastructure;

internal static class FinOpsDbContextFactory
{
    public static FinOpsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<FinOpsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinOpsDbContext(options);
    }
}
