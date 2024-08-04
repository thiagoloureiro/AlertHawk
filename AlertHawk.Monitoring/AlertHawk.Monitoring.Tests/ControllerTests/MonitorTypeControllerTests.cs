using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using EasyMemoryCache.Configuration;
using Microsoft.AspNetCore.Mvc;
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
        var controller = new MonitorTypeController(_monitorTypeServiceMock.Object, _cachingMock.Object);

        // Act
        var result = await controller.GetMonitorType();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_Return_Data_From_Health_Check_From_Service()
    {
        // Arrange
        var monitorTypes = new List<MonitorType> { new MonitorType { Id = 1, Name = "Type1" } };
        _cachingMock.Setup(c => c.SetValueToCacheAsync("monitorTypeList", monitorTypes, 60, CacheTimeInterval.Minutes));
        _cachingMock.Setup(c => c.GetValueFromCacheAsync<string>("monitorTypeList")).ReturnsAsync("monitorTypeList");

        _monitorTypeServiceMock.Setup(service => service.GetMonitorType())
            .ReturnsAsync(monitorTypes);

        // Act
        var controller = new MonitorTypeController(_monitorTypeServiceMock.Object, _cachingMock.Object);

        var result = await controller.GetMonitorType();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}