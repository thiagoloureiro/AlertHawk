using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using Moq;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class MonitorTypeControllerTests
{
    private readonly Mock<IMonitorTypeService> _monitorTypeServiceMock;
    private readonly Mock<ICaching> _cachingMock;

    public MonitorTypeControllerTests()
    {
        _monitorTypeServiceMock = new Mock<IMonitorTypeService>();
        _cachingMock = new Mock<ICaching>();
    }
    
    [Fact]
    public async Task Should_Return_Data_From_Health_Check()
    {
        // Arrange
        var controller = new MonitorTypeController(_monitorTypeServiceMock.Object,_cachingMock.Object);

        // Act
        var result = await controller.GetMonitorType();

        // Assert
        Assert.NotNull(result);
    }
}