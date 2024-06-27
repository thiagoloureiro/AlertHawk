using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class MonitorAlertControllerTests
{
    private readonly Mock<IMonitorAlertService> _mockMonitorAlertService;
    private readonly MonitorAlertController _controller;

    public MonitorAlertControllerTests()
    {
        _mockMonitorAlertService = new Mock<IMonitorAlertService>();
        _controller = new MonitorAlertController(_mockMonitorAlertService.Object);
    }

    [Fact]
    public async Task GetMonitorAlerts_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var invalidToken = string.Empty;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = invalidToken;

        // Act
        var result = await _controller.GetMonitorAlerts(0, 30);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", (result as BadRequestObjectResult)?.Value);
    }

    [Fact]
    public async Task GetMonitorAlerts_ValidToken_ReturnsOk()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var monitorAlerts = new List<MonitorAlert> { new MonitorAlert() };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;
        //TokenUtils.SetJwtToken(validToken); // Mock this method if necessary

        _mockMonitorAlertService.Setup(service =>
                service.GetMonitorAlerts(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<MonitorEnvironment?>(),
                    It.IsAny<string>()))
            .ReturnsAsync(monitorAlerts);

        // Act
        var result = await _controller.GetMonitorAlerts(0, 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorAlerts, okResult.Value);
    }

    [Fact]
    public async Task GetMonitorAlertsReport_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var invalidToken = string.Empty;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = invalidToken;

        // Act
        var result = await _controller.GetMonitorAlertsReport(0, 30, MonitorEnvironment.All, ReportType.Excel);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", (result as BadRequestObjectResult)?.Value);
    }

    [Fact]
    public async Task GetMonitorAlertsReport_InvalidReportType_ReturnsBadRequest()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;
        // TokenUtils.SetJwtToken(validToken); // Mock this method if necessary

        // Act
        var result = await _controller.GetMonitorAlertsReport(0, 30, MonitorEnvironment.All, (ReportType)99); // Invalid ReportType

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Report Type", badRequestResult.Value);
    }

    [Fact]
    public async Task GetMonitorAlertsReport_ValidRequest_ReturnsFileResult()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var reportStream = new MemoryStream();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;
        // TokenUtils.SetJwtToken(validToken); // Mock this method if necessary

        _mockMonitorAlertService.Setup(service =>
                service.GetMonitorAlertsReport(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(),
                    MonitorEnvironment.All,
                    ReportType.Excel))
            .ReturnsAsync(reportStream);

        // Act
        var result = await _controller.GetMonitorAlertsReport(0, 30, MonitorEnvironment.All, ReportType.Excel);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Equal($"MonitorAlerts_{DateTime.UtcNow:yyyyMMdd}.xlsx", fileResult.FileDownloadName);
    }
}