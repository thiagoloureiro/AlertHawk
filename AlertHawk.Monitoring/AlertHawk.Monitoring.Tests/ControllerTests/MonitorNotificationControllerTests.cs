using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class MonitorNotificationControllerTests
{
    private readonly Mock<IMonitorService> _mockMonitorService;
    private readonly MonitorNotificationController _controller;

    public MonitorNotificationControllerTests()
    {
        _mockMonitorService = new Mock<IMonitorService>();
        _controller = new MonitorNotificationController(_mockMonitorService.Object);
    }

    [Fact]
    public async Task GetMonitorNotification_ReturnsOk()
    {
        // Arrange
        var notifications = new List<MonitorNotification> { new MonitorNotification() };
        _mockMonitorService.Setup(service => service.GetMonitorNotifications(It.IsAny<int>()))
            .ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetMonitorNotification(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(notifications, okResult.Value);
    }

    [Fact]
    public async Task AddMonitorNotification_ReturnsOk()
    {
        // Arrange
        var monitorNotification = new MonitorNotification { MonitorId = 1, NotificationId = 1 };
        var notifications = new List<MonitorNotification>();
        _mockMonitorService.Setup(service => service.GetMonitorNotifications(It.IsAny<int>()))
            .ReturnsAsync(notifications);
        _mockMonitorService.Setup(service => service.AddMonitorNotification(It.IsAny<MonitorNotification>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddMonitorNotification(monitorNotification);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        _mockMonitorService.Verify(service => service.AddMonitorNotification(monitorNotification), Times.Once);
    }

    [Fact]
    public async Task AddMonitorNotification_ReturnsBadRequest_WhenNotificationExists()
    {
        // Arrange
        var monitorNotification = new MonitorNotification { MonitorId = 1, NotificationId = 1 };
        var notifications = new List<MonitorNotification> { new MonitorNotification { MonitorId = 1, NotificationId = 1 } };
        _mockMonitorService.Setup(service => service.GetMonitorNotifications(It.IsAny<int>()))
            .ReturnsAsync(notifications);

        // Act
        var result = await _controller.AddMonitorNotification(monitorNotification);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Notification already exists for this monitor", badRequestResult.Value);
        _mockMonitorService.Verify(service => service.AddMonitorNotification(It.IsAny<MonitorNotification>()), Times.Never);
    }

    [Fact]
    public async Task RemoveMonitorNotification_ReturnsOk()
    {
        // Arrange
        var monitorNotification = new MonitorNotification { MonitorId = 1, NotificationId = 1 };
        _mockMonitorService.Setup(service => service.RemoveMonitorNotification(It.IsAny<MonitorNotification>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveMonitorNotification(monitorNotification);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        _mockMonitorService.Verify(service => service.RemoveMonitorNotification(monitorNotification), Times.Once);
    }

    [Fact]
    public async Task AddMonitorGroupNotification_ReturnsOk()
    {
        // Arrange
        var monitorGroupNotification = new MonitorGroupNotification();
        _mockMonitorService.Setup(service => service.AddMonitorGroupNotification(It.IsAny<MonitorGroupNotification>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddMonitorGroupNotification(monitorGroupNotification);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        _mockMonitorService.Verify(service => service.AddMonitorGroupNotification(monitorGroupNotification), Times.Once);
    }

    [Fact]
    public async Task RemoveMonitorGroupNotification_ReturnsOk()
    {
        // Arrange
        var monitorGroupNotification = new MonitorGroupNotification();
        _mockMonitorService.Setup(service => service.RemoveMonitorGroupNotification(It.IsAny<MonitorGroupNotification>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveMonitorGroupNotification(monitorGroupNotification);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        _mockMonitorService.Verify(service => service.RemoveMonitorGroupNotification(monitorGroupNotification), Times.Once);
    }
}