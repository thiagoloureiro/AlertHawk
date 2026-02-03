using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Entities;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Metrics.Tests;

public class MetricsNotificationControllerTests
{
    private readonly Mock<IMetricsNotificationService> _mockMetricsNotificationService;
    private readonly MetricsNotificationController _controller;

    public MetricsNotificationControllerTests()
    {
        _mockMetricsNotificationService = new Mock<IMetricsNotificationService>();

        _controller = new MetricsNotificationController(_mockMetricsNotificationService.Object);

        // Setup controller context for [Authorize]
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region GetMetricsNotification Tests

    [Fact]
    public async Task GetMetricsNotification_WithValidClusterName_ReturnsOkWithNotifications()
    {
        // Arrange
        var expectedNotifications = new List<MetricsNotification>
        {
            new MetricsNotification { ClusterName = "prod-cluster", NotificationId = 1 },
            new MetricsNotification { ClusterName = "prod-cluster", NotificationId = 2 }
        };

        _mockMetricsNotificationService
            .Setup(s => s.GetMetricsNotifications("prod-cluster"))
            .ReturnsAsync(expectedNotifications);

        // Act
        var result = await _controller.GetMetricsNotification("prod-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var notifications = Assert.IsAssignableFrom<IEnumerable<MetricsNotification>>(okResult.Value).ToList();
        Assert.Equal(2, notifications.Count);
        Assert.All(notifications, n => Assert.Equal("prod-cluster", n.ClusterName));
        _mockMetricsNotificationService.Verify(s => s.GetMetricsNotifications("prod-cluster"), Times.Once);
    }

    [Fact]
    public async Task GetMetricsNotification_WithNullClusterName_NormalizesToEmptyAndReturnsOk()
    {
        // Arrange
        var expectedNotifications = new List<MetricsNotification>();
        _mockMetricsNotificationService
            .Setup(s => s.GetMetricsNotifications(string.Empty))
            .ReturnsAsync(expectedNotifications);

        // Act
        var result = await _controller.GetMetricsNotification(null!);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var notifications = Assert.IsAssignableFrom<IEnumerable<MetricsNotification>>(okResult.Value).ToList();
        Assert.Empty(notifications);
        _mockMetricsNotificationService.Verify(s => s.GetMetricsNotifications(string.Empty), Times.Once);
    }

    [Fact]
    public async Task GetMetricsNotification_ReturnsEmptyList_WhenNoNotifications()
    {
        // Arrange
        _mockMetricsNotificationService
            .Setup(s => s.GetMetricsNotifications("staging-cluster"))
            .ReturnsAsync(new List<MetricsNotification>());

        // Act
        var result = await _controller.GetMetricsNotification("staging-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var notifications = Assert.IsAssignableFrom<IEnumerable<MetricsNotification>>(okResult.Value).ToList();
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task GetMetricsNotification_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockMetricsNotificationService
            .Setup(s => s.GetMetricsNotifications("cluster"))
            .ThrowsAsync(new InvalidOperationException("Repository error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetMetricsNotification("cluster"));
    }

    #endregion

    #region AddMetricsNotification Tests

    [Fact]
    public async Task AddMetricsNotification_WithValidData_ReturnsOkAndCallsService()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "prod-cluster", NotificationId = 42 };
        _mockMetricsNotificationService
            .Setup(s => s.GetMetricsNotifications("prod-cluster"))
            .ReturnsAsync(new List<MetricsNotification>());
        _mockMetricsNotificationService
            .Setup(s => s.AddMetricsNotification(It.IsAny<MetricsNotification>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddMetricsNotification(notification);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        _mockMetricsNotificationService.Verify(s => s.GetMetricsNotifications("prod-cluster"), Times.Once);
        _mockMetricsNotificationService.Verify(
            s => s.AddMetricsNotification(It.Is<MetricsNotification>(n =>
                n.ClusterName == "prod-cluster" && n.NotificationId == 42)),
            Times.Once);
    }

    [Fact]
    public async Task AddMetricsNotification_WithMissingClusterName_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "", NotificationId = 1 };

        // Act
        var result = await _controller.AddMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ClusterName is required", badRequest.Value);
        _mockMetricsNotificationService.Verify(s => s.GetMetricsNotifications(It.IsAny<string>()), Times.Never);
        _mockMetricsNotificationService.Verify(s => s.AddMetricsNotification(It.IsAny<MetricsNotification>()), Times.Never);
    }

    [Fact]
    public async Task AddMetricsNotification_WithWhitespaceClusterName_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "   ", NotificationId = 1 };

        // Act
        var result = await _controller.AddMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ClusterName is required", badRequest.Value);
    }

    [Fact]
    public async Task AddMetricsNotification_WithNotificationIdZero_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "cluster", NotificationId = 0 };

        // Act
        var result = await _controller.AddMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("NotificationId must be greater than 0", badRequest.Value);
    }

    [Fact]
    public async Task AddMetricsNotification_WithNegativeNotificationId_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "cluster", NotificationId = -1 };

        // Act
        var result = await _controller.AddMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("NotificationId must be greater than 0", badRequest.Value);
    }

    [Fact]
    public async Task AddMetricsNotification_WhenNotificationAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "prod-cluster", NotificationId = 10 };
        var existing = new List<MetricsNotification>
        {
            new MetricsNotification { ClusterName = "prod-cluster", NotificationId = 10 }
        };
        _mockMetricsNotificationService
            .Setup(s => s.GetMetricsNotifications("prod-cluster"))
            .ReturnsAsync(existing);

        // Act
        var result = await _controller.AddMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Notification already exists for this cluster", badRequest.Value);
        _mockMetricsNotificationService.Verify(s => s.GetMetricsNotifications("prod-cluster"), Times.Once);
        _mockMetricsNotificationService.Verify(s => s.AddMetricsNotification(It.IsAny<MetricsNotification>()), Times.Never);
    }

    [Fact]
    public async Task AddMetricsNotification_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "cluster", NotificationId = 1 };
        _mockMetricsNotificationService
            .Setup(s => s.GetMetricsNotifications("cluster"))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _controller.AddMetricsNotification(notification));
    }

    #endregion

    #region RemoveMetricsNotification Tests

    [Fact]
    public async Task RemoveMetricsNotification_WithValidData_ReturnsOkAndCallsService()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "prod-cluster", NotificationId = 5 };
        _mockMetricsNotificationService
            .Setup(s => s.RemoveMetricsNotification(It.IsAny<MetricsNotification>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveMetricsNotification(notification);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        _mockMetricsNotificationService.Verify(
            s => s.RemoveMetricsNotification(It.Is<MetricsNotification>(n =>
                n.ClusterName == "prod-cluster" && n.NotificationId == 5)),
            Times.Once);
    }

    [Fact]
    public async Task RemoveMetricsNotification_WithMissingClusterName_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "", NotificationId = 1 };

        // Act
        var result = await _controller.RemoveMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ClusterName is required", badRequest.Value);
        _mockMetricsNotificationService.Verify(s => s.RemoveMetricsNotification(It.IsAny<MetricsNotification>()), Times.Never);
    }

    [Fact]
    public async Task RemoveMetricsNotification_WithWhitespaceClusterName_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "   ", NotificationId = 1 };

        // Act
        var result = await _controller.RemoveMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ClusterName is required", badRequest.Value);
    }

    [Fact]
    public async Task RemoveMetricsNotification_WithNotificationIdZero_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "cluster", NotificationId = 0 };

        // Act
        var result = await _controller.RemoveMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("NotificationId must be greater than 0", badRequest.Value);
    }

    [Fact]
    public async Task RemoveMetricsNotification_WithNegativeNotificationId_ReturnsBadRequest()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "cluster", NotificationId = -5 };

        // Act
        var result = await _controller.RemoveMetricsNotification(notification);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("NotificationId must be greater than 0", badRequest.Value);
    }

    [Fact]
    public async Task RemoveMetricsNotification_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var notification = new MetricsNotification { ClusterName = "cluster", NotificationId = 1 };
        _mockMetricsNotificationService
            .Setup(s => s.RemoveMetricsNotification(It.IsAny<MetricsNotification>()))
            .ThrowsAsync(new InvalidOperationException("Remove failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.RemoveMetricsNotification(notification));
    }

    #endregion
}
