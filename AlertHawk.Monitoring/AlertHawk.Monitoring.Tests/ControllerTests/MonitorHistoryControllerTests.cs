using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSubstitute;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class MonitorHistoryControllerTests
{
    private readonly Mock<IMonitorService> _mockMonitorService;
    private readonly Mock<IMonitorHistoryService> _mockMonitorHistoryService;
    private readonly MonitorHistoryController _controller;

    public MonitorHistoryControllerTests()
    {
        _mockMonitorService = new Mock<IMonitorService>();
        _mockMonitorHistoryService = new Mock<IMonitorHistoryService>();
        _controller = new MonitorHistoryController(_mockMonitorService.Object, _mockMonitorHistoryService.Object);
    }

    [Fact]
    public async Task GetMonitorHistory_ReturnsOk()
    {
        // Arrange
        var monitorHistory = new List<MonitorHistory> { new MonitorHistory() };
        _mockMonitorHistoryService.Setup(service => service.GetMonitorHistory(It.IsAny<int>()))
            .ReturnsAsync(monitorHistory);

        // Act
        var result = await _controller.GetMonitorHistory(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorHistory, okResult.Value);
    }

    [Fact]
    public async Task GetMonitorHistoryByIdDays_ReturnsOk()
    {
        // Arrange
        var monitorHistory = new List<MonitorHistory> { new MonitorHistory() };
        _mockMonitorHistoryService.Setup(service => service.GetMonitorHistory(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(monitorHistory);

        // Act
        var result = await _controller.GetMonitorHistory(1, 7);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorHistory, okResult.Value);
    }

    [Fact]
    public async Task GetMonitorDashboardData_ReturnsOk()
    {
        // Arrange
        var monitorDashboard = new MonitorDashboard();
        _mockMonitorService.Setup(service => service.GetMonitorDashboardData(It.IsAny<int>(), It.IsAny<Monitor>()))
            .ReturnsAsync(monitorDashboard);

        // Act
        var result = await _controller.GetMonitorDashboardData(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorDashboard, okResult.Value);
    }

    [Fact]
    public async Task GetMonitorDashboardDataList_ReturnsOk()
    {
        // Arrange
        var monitorDashboardList = new List<MonitorDashboard> { new MonitorDashboard() };
        var ids = new List<int> { 1, 2, 3 };

        _mockMonitorService.Setup(service => service.GetMonitorDashboardDataList(It.IsAny<List<int>>()))
            .ReturnsAsync(monitorDashboardList);

        // Act
        var result = await _controller.GetMonitorDashboardDataList(ids);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorDashboardList, okResult.Value);
    }

    [Fact]
    public async Task DeleteMonitorHistory_ReturnsOk()
    {
        // Arrange
        var jwtToken = "Bearer valid.token.here";
        var monitorServiceMock = new Mock<IMonitorService>();
        var monitorHistoryServiceMock = new Mock<IMonitorHistoryService>();
        var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: true);

        var controller = new MonitorHistoryController(monitorServiceMock.Object, monitorHistoryServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["Authorization"] = jwtToken;

        monitorServiceMock.Setup(x => x.GetUserDetailsByToken(It.IsAny<string>())).ReturnsAsync(user);

        // Act
        var result = await controller.DeleteMonitorHistory(7);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetMonitorHistoryCount_ReturnsOk()
    {
        // Arrange
        var count = 100L;
        _mockMonitorHistoryService.Setup(service => service.GetMonitorHistoryCount())
            .ReturnsAsync(count);

        // Act
        var result = await _controller.GetMonitorHistoryCount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(count, okResult.Value);
    }

    [Fact]
    public async Task GetMonitorHistoryRetention_ReturnsOk()
    {
        // Arrange
        var monitorSettings = new MonitorSettings();
        _mockMonitorHistoryService.Setup(service => service.GetMonitorHistoryRetention())
            .ReturnsAsync(monitorSettings);

        // Act
        var result = await _controller.GetMonitorHistoryRetention();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorSettings, okResult.Value);
    }

    [Fact]
    public async Task SetMonitorHistoryRetention_ReturnsOk()
    {
        // Arrange
        var monitorSettings = new MonitorSettings { HistoryDaysRetention = 7 };

        var jwtToken = "Bearer valid.token.here";
        var monitorServiceMock = new Mock<IMonitorService>();
        var monitorHistoryServiceMock = new Mock<IMonitorHistoryService>();
        var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: true);

        var controller = new MonitorHistoryController(monitorServiceMock.Object, monitorHistoryServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["Authorization"] = jwtToken;

        monitorServiceMock.Setup(x => x.GetUserDetailsByToken(It.IsAny<string>())).ReturnsAsync(user);

        // Act
        var result = await controller.SetMonitorHistoryRetention(monitorSettings);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }
}