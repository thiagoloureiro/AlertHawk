using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Moq;

namespace AlertHawk.Monitoring.Tests.ServiceTests;

public class MonitorAlertServiceTests
{
    private readonly Mock<IMonitorAlertRepository> _monitorAlertRepositoryMock;
    private readonly Mock<IMonitorGroupService> _monitorGroupServiceMock;
    private readonly MonitorAlertService _monitorAlertService;

    public MonitorAlertServiceTests()
    {
        _monitorAlertRepositoryMock = new Mock<IMonitorAlertRepository>();
        _monitorGroupServiceMock = new Mock<IMonitorGroupService>();
        _monitorAlertService =
            new MonitorAlertService(_monitorAlertRepositoryMock.Object, _monitorGroupServiceMock.Object);
    }

    [Fact]
    public async Task GetMonitorAlerts_ShouldReturnAlerts_WhenGroupIdsAreAvailable()
    {
        // Arrange
        var groupIds = new List<int> { 1, 2, 3 };
        var monitorAlerts = new List<MonitorAlert>
        {
            new MonitorAlert { Id = 1, MonitorId = 1 },
            new MonitorAlert { Id = 2, MonitorId = 2 }
        };

        _monitorGroupServiceMock.Setup(service => service.GetUserGroupMonitorListIds(It.IsAny<string>()))
            .ReturnsAsync(groupIds);
        _monitorAlertRepositoryMock.Setup(repo =>
                repo.GetMonitorAlerts(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<MonitorEnvironment?>(), groupIds))
            .ReturnsAsync(monitorAlerts);

        // Act
        var result = await _monitorAlertService.GetMonitorAlerts(1, 7, MonitorEnvironment.Production, "jwtToken");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Equal(monitorAlerts, result);
    }

    [Fact]
    public async Task GetMonitorAlerts_ShouldReturnEmptyList_WhenNoGroupIdsAreAvailable()
    {
        // Arrange
        _monitorGroupServiceMock.Setup(service => service.GetUserGroupMonitorListIds(It.IsAny<string>()))
            .ReturnsAsync(new List<int>());

        // Act
        var result = await _monitorAlertService.GetMonitorAlerts(1, 7, MonitorEnvironment.Production, "jwtToken");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMonitorAlertsReport_ShouldReturnExcelFile_WhenReportTypeIsExcel()
    {
        // Arrange
        var monitorAlerts = new List<MonitorAlert>
        {
            new MonitorAlert { Id = 1, MonitorId = 1 },
            new MonitorAlert { Id = 2, MonitorId = 2 }
        };

        var excelMemoryStream = new MemoryStream();

        _monitorAlertRepositoryMock.Setup(repo => repo.GetMonitorAlerts(It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<MonitorEnvironment?>(), It.IsAny<List<int>>()))
            .ReturnsAsync(monitorAlerts);
        _monitorAlertRepositoryMock.Setup(repo => repo.CreateExcelFileAsync(monitorAlerts))
            .ReturnsAsync(excelMemoryStream);

        _monitorGroupServiceMock.Setup(service => service.GetUserGroupMonitorListIds(It.IsAny<string>()))
            .ReturnsAsync(new List<int> { 1, 2, 3 });

        // Act
        var result =
            await _monitorAlertService.GetMonitorAlertsReport(1, 7, "jwtToken", MonitorEnvironment.All,
                ReportType.Excel);

        // Assert
        Assert.Equal(excelMemoryStream, result);
    }

    [Fact]
    public async Task GetMonitorAlertsReport_ShouldReturnEmptyMemoryStream_WhenReportTypeIsNotExcel()
    {
        // Arrange
        _monitorGroupServiceMock.Setup(service => service.GetUserGroupMonitorListIds(It.IsAny<string>()))
            .ReturnsAsync(new List<int> { 1, 2, 3 });

        // Act
        var result =
            await _monitorAlertService.GetMonitorAlertsReport(1, 7, "jwtToken", MonitorEnvironment.All, ReportType.Pdf);

        // Assert
        Assert.Empty(result.ToArray());
    }
}