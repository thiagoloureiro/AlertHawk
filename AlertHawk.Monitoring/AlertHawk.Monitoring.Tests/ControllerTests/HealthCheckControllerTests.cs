using AlertHawk.Monitoring.Controllers;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class HealthCheckControllerTests
{
    [Fact]
    public void Should_Return_Data_From_Health_Check()
    {
        // Arrange
        var controller = new HealthCheckController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.NotNull(result);
    }
}