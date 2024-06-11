using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Entities.Report;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class MonitorReportControllerTests
{
    private readonly Mock<IMonitorReportService> _mockMonitorReportService;
    private readonly MonitorReportController _controller;
'e'
    public MonitorReportControllerTests()
    {
        _mockMonitorReportService = new Mock<IMonitorReportService>();
        _controller = new MonitorReportController(_mockMonitorReportService.Object);
    }

    [Fact]
    public async Task GetMonitorReportUptime_ReturnsOk()
    {
        // Arrange
        var uptimeReports = new List<MonitorReportUptime> { new MonitorReportUptime() };
        _mockMonitorReportService.Setup(service => service.GetMonitorReportUptime(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(uptimeReports);

        // Act
        var result = await _controller.GetMonitorReportUptime(1, 24);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(uptimeReports, okResult.Value);
    }
    [Fact]
    public async Task GetMonitorReportUptimeByDate_ReturnsOk()
    {
        // Arrange
        var uptimeReports = new List<MonitorReportUptime> { new MonitorReportUptime() };
        _mockMonitorReportService.Setup(service => service.GetMonitorReportUptime(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(uptimeReports);

        // Act
        var result = await _controller.GetMonitorReportUptime(1, 24);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(uptimeReports, okResult.Value);
    }
    [Fact]
    public async Task GetMonitorAlerts_ReturnsOk()
    {
        // Arrange
        var alertReports = new List<MonitorReportAlerts> { new MonitorReportAlerts() };
        _mockMonitorReportService.Setup(service => service.GetMonitorAlerts(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(alertReports);

        // Act
        var result = await _controller.GetMonitorAlerts(1, 24);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(alertReports, okResult.Value);
    }

    [Fact]
    public async Task GetMonitorResponseTime_ReturnsOk()
    {
        // Arrange
        var responseTimeReports = new List<MonitorReponseTime> { new MonitorReponseTime() };
        _mockMonitorReportService.Setup(service => service.GetMonitorResponseTime(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(responseTimeReports);

        // Act
        var result = await _controller.GetMonitorResponseTime(1, 24);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(responseTimeReports, okResult.Value);
    }
}