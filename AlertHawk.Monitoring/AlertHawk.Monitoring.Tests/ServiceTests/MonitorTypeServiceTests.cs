using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Moq;

namespace AlertHawk.Monitoring.Tests.ServiceTests;

public class MonitorTypeServiceTests
{
    private readonly Mock<IMonitorTypeRepository> _monitorTypeRepositoryMock;
    private readonly MonitorTypeService _monitorTypeService;

    public MonitorTypeServiceTests()
    {
        _monitorTypeRepositoryMock = new Mock<IMonitorTypeRepository>();
        _monitorTypeService = new MonitorTypeService(_monitorTypeRepositoryMock.Object);
    }

    [Fact]
    public async Task GetMonitorType_ShouldReturnMonitorTypes()
    {
        // Arrange
        var monitorTypes = new List<MonitorType>
        {
            new MonitorType { Id = 1, Name = "MonitorType1" },
            new MonitorType { Id = 2, Name = "MonitorType2" }
        };

        _monitorTypeRepositoryMock.Setup(repo => repo.GetMonitorType()).ReturnsAsync(monitorTypes);

        // Act
        var result = await _monitorTypeService.GetMonitorType();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(monitorTypes, result);
    }
}