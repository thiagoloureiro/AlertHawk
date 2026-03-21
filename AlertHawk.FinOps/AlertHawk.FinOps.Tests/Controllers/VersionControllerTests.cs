using AlertHawk.FinOps.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.FinOps.Tests.Controllers;

public class VersionControllerTests
{
    [Fact]
    public void Get_ReturnsNonEmptyVersionString()
    {
        var controller = new VersionController();
        var version = controller.Get();
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.Contains('.', version);
    }
}
