using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities.Report;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Moq;

namespace AlertHawk.Monitoring.Tests.ServiceTests;

public class MonitorReportServiceTests
{
    private readonly Mock<IMonitorReportRepository> _monitorReportRepositoryMock;
    private readonly MonitorReportService _monitorReportService;

    public MonitorReportServiceTests()
    {
        _monitorReportRepositoryMock = new Mock<IMonitorReportRepository>();
        _monitorReportService = new MonitorReportService(_monitorReportRepositoryMock.Object);
    }

    [Fact]
    public async Task GetMonitorReportUptime_ShouldReturnMonitorReportUptime()
    {
        // Arrange
        var monitorReportUptimes = new List<MonitorReportUptime>
        {
            new MonitorReportUptime { MonitorName = "Monitor1", TotalOnlineMinutes = 100, TotalOfflineMinutes = 50 },
            new MonitorReportUptime { MonitorName = "Monitor2", TotalOnlineMinutes = 200, TotalOfflineMinutes = 100 }
        };

        _monitorReportRepositoryMock.Setup(repo => repo.GetMonitorReportUptime(1, 24)).ReturnsAsync(monitorReportUptimes);

        // Act
        var result = await _monitorReportService.GetMonitorReportUptime(1, 24, "");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(monitorReportUptimes, result);
    }

    [Fact]
    public async Task GetMonitorAlerts_ShouldReturnMonitorAlerts()
    {
        // Arrange
        var monitorAlerts = new List<MonitorReportAlerts>
        {
            new MonitorReportAlerts { MonitorName = "Monitor1", NumAlerts = 1 },
            new MonitorReportAlerts { MonitorName = "Monitor2", NumAlerts = 2 }
        };

        _monitorReportRepositoryMock.Setup(repo => repo.GetMonitorAlerts(1, 24)).ReturnsAsync(monitorAlerts);

        // Act
        var result = await _monitorReportService.GetMonitorAlerts(1, 24);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(monitorAlerts, result);
    }

    [Fact]
    public async Task GetMonitorResponseTime_ShouldReturnMonitorResponseTime()
    {
        // Arrange
        var monitorResponseTimes = new List<MonitorReponseTime>
        {
            new MonitorReponseTime { MonitorName = "Monitor1", MaxResponseTime = 100, AvgResponseTime = 50, MinResponseTime = 1},
            new MonitorReponseTime { MonitorName = "Monitor2", MaxResponseTime = 100, AvgResponseTime = 50, MinResponseTime = 1}
        };

        _monitorReportRepositoryMock.Setup(repo => repo.GetMonitorResponseTime(1, 24)).ReturnsAsync(monitorResponseTimes);

        // Act
        var result = await _monitorReportService.GetMonitorResponseTime(1, 24, "");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(monitorResponseTimes, result);
    }

    [Fact]
    public async Task GetMonitorReportUptime_ShouldReturnMonitorReportUptimeWithFilter()
    {
        // Arrange
        var monitorReportUptimes = new List<MonitorReportUptime>
        {
            new MonitorReportUptime { MonitorName = "Monitor1", TotalOnlineMinutes = 100, TotalOfflineMinutes = 50 },
            new MonitorReportUptime { MonitorName = "Monitor2", TotalOnlineMinutes = 200, TotalOfflineMinutes = 100 }
        };

        _monitorReportRepositoryMock.Setup(repo => repo.GetMonitorReportUptime(1, 24)).ReturnsAsync(monitorReportUptimes);

        // Act
        var result = await _monitorReportService.GetMonitorReportUptime(1, 24, "Monitor1");

        // Assert
        Assert.Single(result);
        Assert.Equal("Monitor1", result.First().MonitorName);
    }

    [Fact]
    public async Task GetMonitorReportUptimeStartEndDate_ShouldReturnMonitorReportUptime()
    {
        // Arrange
        var monitorReportUptimes = new List<MonitorReportUptime>
        {
            new MonitorReportUptime { MonitorName = "Monitor1", TotalOnlineMinutes = 100, TotalOfflineMinutes = 50 },
            new MonitorReportUptime { MonitorName = "Monitor2", TotalOnlineMinutes = 200, TotalOfflineMinutes = 100 }
        };

        _monitorReportRepositoryMock.Setup(repo => repo.GetMonitorReportUptime(1, It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(monitorReportUptimes);

        // Act
        var result = await _monitorReportService.GetMonitorReportUptime(1, It.IsAny<DateTime>(), It.IsAny<DateTime>());

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(monitorReportUptimes, result);
    }
}