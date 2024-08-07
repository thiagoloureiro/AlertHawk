using AlertHawk.Monitoring.Controllers;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class VersionControllerTests
{
    [Fact]
    public void Should_Return_Version()
    {
        // Arrange
        var controller = new VersionController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.NotNull(result);
    }
}