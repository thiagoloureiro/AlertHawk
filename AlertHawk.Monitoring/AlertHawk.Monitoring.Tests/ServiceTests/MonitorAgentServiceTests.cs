using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Domain.Classes;
using Moq;

namespace AlertHawk.Monitoring.Tests.ServiceTests;

public class MonitorAgentServiceTests
{
    private readonly Mock<IMonitorAgentRepository> _monitorAgentRepositoryMock;
    private readonly IMonitorAgentService _monitorAgentService;

    public MonitorAgentServiceTests()
    {
        _monitorAgentRepositoryMock = new Mock<IMonitorAgentRepository>();
        _monitorAgentService = new MonitorAgentService(_monitorAgentRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllMonitorAgents_ShouldReturnAgentsWithTasksCount()
    {
        // Arrange
        var agents = new List<MonitorAgent>
        {
            new MonitorAgent
            {
                Id = 2,
                MonitorRegion = MonitorRegion.Oceania,
                Hostname = null,
                TimeStamp = default
            },
            new MonitorAgent()
            {
                Id = 1,
                MonitorRegion = MonitorRegion.Europe,
                Hostname = null,
                TimeStamp = default
            }
        };


        var agentTasks = new List<MonitorAgentTasks>
        {
            new MonitorAgentTasks { MonitorAgentId = 1 },
            new MonitorAgentTasks { MonitorAgentId = 1 },
            new MonitorAgentTasks { MonitorAgentId = 2 }
        };

        _monitorAgentRepositoryMock.Setup(repo => repo.GetAllMonitorAgents())
            .ReturnsAsync(agents);
        _monitorAgentRepositoryMock.Setup(repo => repo.GetAllMonitorAgentTasks(It.IsAny<List<int>>()))
            .ReturnsAsync(agentTasks);

        // Act
        var result = await _monitorAgentService.GetAllMonitorAgents();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(2, result.First(a => a.Id == 1).ListTasks);
        Assert.Equal(1, result.First(a => a.Id == 2).ListTasks);
    }

    [Fact]
    public async Task GetAllMonitorAgents_ShouldReturnEmptyList_WhenNoAgentsFound()
    {
        // Arrange
        _monitorAgentRepositoryMock.Setup(repo => repo.GetAllMonitorAgents())
            .ReturnsAsync(new List<MonitorAgent>());

        // Act
        var result = await _monitorAgentService.GetAllMonitorAgents();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllMonitorAgents_ShouldReturnAgentsWithZeroTasks_WhenNoTasksFound()
    {
        // Arrange
        var agents = new List<MonitorAgent>
        {
            new MonitorAgent
            {
                Id = 2,
                MonitorRegion = MonitorRegion.Oceania,
                Hostname = null,
                TimeStamp = default
            },
            new MonitorAgent()
            {
                Id = 1,
                MonitorRegion = MonitorRegion.Europe,
                Hostname = null,
                TimeStamp = default
            }
        };

        _monitorAgentRepositoryMock.Setup(repo => repo.GetAllMonitorAgents())
            .ReturnsAsync(agents);
        _monitorAgentRepositoryMock.Setup(repo => repo.GetAllMonitorAgentTasks(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MonitorAgentTasks>());

        // Act
        var result = await _monitorAgentService.GetAllMonitorAgents();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(0, result.First(a => a.Id == 1).ListTasks);
        Assert.Equal(0, result.First(a => a.Id == 2).ListTasks);
    }
}