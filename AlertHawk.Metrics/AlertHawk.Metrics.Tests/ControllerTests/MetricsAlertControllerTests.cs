using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Entities;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Metrics.Tests;

public class MetricsAlertControllerTests
{
    private readonly Mock<IMetricsAlertService> _mockMetricsAlertService;
    private readonly MetricsAlertController _controller;

    public MetricsAlertControllerTests()
    {
        _mockMetricsAlertService = new Mock<IMetricsAlertService>();

        _controller = new MetricsAlertController(_mockMetricsAlertService.Object);

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

    #region GetMetricsAlerts Tests

    [Fact]
    public async Task GetMetricsAlerts_WithAllFilters_ReturnsOkWithAlerts()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>
        {
            new MetricsAlert
            {
                Id = 1,
                NodeName = "node-1",
                ClusterName = "prod-cluster",
                TimeStamp = DateTime.UtcNow.AddDays(-1),
                Status = false,
                Message = "High memory pressure"
            }
        };

        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("prod-cluster", "node-1", 7))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlerts("prod-cluster", "node-1", 7);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var alerts = Assert.IsAssignableFrom<List<MetricsAlert>>(okResult.Value);
        Assert.Single(alerts);
        Assert.Equal("node-1", alerts[0].NodeName);
        Assert.Equal("prod-cluster", alerts[0].ClusterName);
        Assert.False(alerts[0].Status);
        Assert.Equal("High memory pressure", alerts[0].Message);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("prod-cluster", "node-1", 7), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlerts_WithNoFilters_ReturnsOkWithAlerts()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>
        {
            new MetricsAlert
            {
                Id = 1,
                NodeName = "node-1",
                ClusterName = "cluster-a",
                TimeStamp = DateTime.UtcNow.AddDays(-5),
                Status = true,
                Message = "Recovered"
            },
            new MetricsAlert
            {
                Id = 2,
                NodeName = "node-2",
                ClusterName = "cluster-b",
                TimeStamp = DateTime.UtcNow.AddDays(-10),
                Status = false,
                Message = "Disk pressure"
            }
        };

        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts(null, null, 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlerts(null, null, 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var alerts = Assert.IsAssignableFrom<List<MetricsAlert>>(okResult.Value);
        Assert.Equal(2, alerts.Count);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts(null, null, 30), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlerts_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>();
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts(null, null, 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlerts();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var alerts = Assert.IsAssignableFrom<List<MetricsAlert>>(okResult.Value);
        Assert.Empty(alerts);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts(null, null, 30), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlerts_WithClusterNameOnly_CallsServiceWithNodeNameNull()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>();
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("my-cluster", null, 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlerts("my-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("my-cluster", null, 30), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlerts_WithCustomDays_CallsServiceWithCorrectDays()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>();
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts(null, null, 90))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlerts(days: 90);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts(null, null, 90), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlerts_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts(null, null, 30))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetMetricsAlerts());
    }

    [Fact]
    public async Task GetMetricsAlerts_ReturnsEmptyList_WhenNoAlerts()
    {
        // Arrange
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("cluster", null, 30))
            .ReturnsAsync(new List<MetricsAlert>());

        // Act
        var result = await _controller.GetMetricsAlerts("cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var alerts = Assert.IsAssignableFrom<List<MetricsAlert>>(okResult.Value);
        Assert.Empty(alerts);
    }

    #endregion

    #region GetMetricsAlertsByCluster Tests

    [Fact]
    public async Task GetMetricsAlertsByCluster_WithValidClusterName_ReturnsOkWithAlerts()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>
        {
            new MetricsAlert
            {
                Id = 1,
                NodeName = "node-1",
                ClusterName = "prod-cluster",
                TimeStamp = DateTime.UtcNow.AddDays(-2),
                Status = false,
                Message = "Memory pressure"
            }
        };

        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("prod-cluster", null, 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlertsByCluster("prod-cluster", 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var alerts = Assert.IsAssignableFrom<List<MetricsAlert>>(okResult.Value);
        Assert.Single(alerts);
        Assert.Equal("prod-cluster", alerts[0].ClusterName);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("prod-cluster", null, 30), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlertsByCluster_WithDefaultDays_UsesDefault()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>();
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("staging-cluster", null, 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlertsByCluster("staging-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("staging-cluster", null, 30), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlertsByCluster_WithCustomDays_CallsServiceWithCorrectDays()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>();
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("prod-cluster", null, 7))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlertsByCluster("prod-cluster", 7);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("prod-cluster", null, 7), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlertsByCluster_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("cluster", null, 30))
            .ThrowsAsync(new Exception("Repository error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _controller.GetMetricsAlertsByCluster("cluster"));
    }

    #endregion

    #region GetMetricsAlertsByNode Tests

    [Fact]
    public async Task GetMetricsAlertsByNode_WithValidClusterAndNode_ReturnsOkWithAlerts()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>
        {
            new MetricsAlert
            {
                Id = 1,
                NodeName = "worker-node-1",
                ClusterName = "prod-cluster",
                TimeStamp = DateTime.UtcNow.AddDays(-1),
                Status = false,
                Message = "Disk pressure detected"
            }
        };

        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("prod-cluster", "worker-node-1", 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlertsByNode("prod-cluster", "worker-node-1", 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var alerts = Assert.IsAssignableFrom<List<MetricsAlert>>(okResult.Value);
        Assert.Single(alerts);
        Assert.Equal("prod-cluster", alerts[0].ClusterName);
        Assert.Equal("worker-node-1", alerts[0].NodeName);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("prod-cluster", "worker-node-1", 30), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlertsByNode_WithDefaultDays_UsesDefault()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>();
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("cluster-a", "node-5", 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlertsByNode("cluster-a", "node-5");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("cluster-a", "node-5", 30), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlertsByNode_WithCustomDays_CallsServiceWithCorrectDays()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>();
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("prod", "node-1", 14))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlertsByNode("prod", "node-1", 14);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockMetricsAlertService.Verify(s => s.GetMetricsAlerts("prod", "node-1", 14), Times.Once);
    }

    [Fact]
    public async Task GetMetricsAlertsByNode_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("cluster", "node-1", 30))
            .ThrowsAsync(new ArgumentException("Invalid node"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _controller.GetMetricsAlertsByNode("cluster", "node-1"));
    }

    [Fact]
    public async Task GetMetricsAlertsByNode_ReturnsMultipleAlerts_WhenNodeHasHistory()
    {
        // Arrange
        var expectedAlerts = new List<MetricsAlert>
        {
            new MetricsAlert { Id = 1, NodeName = "n1", ClusterName = "c1", TimeStamp = DateTime.UtcNow.AddDays(-3), Status = false, Message = "Alert 1" },
            new MetricsAlert { Id = 2, NodeName = "n1", ClusterName = "c1", TimeStamp = DateTime.UtcNow.AddDays(-1), Status = true, Message = "Recovered" }
        };

        _mockMetricsAlertService
            .Setup(s => s.GetMetricsAlerts("c1", "n1", 30))
            .ReturnsAsync(expectedAlerts);

        // Act
        var result = await _controller.GetMetricsAlertsByNode("c1", "n1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var alerts = Assert.IsAssignableFrom<List<MetricsAlert>>(okResult.Value);
        Assert.Equal(2, alerts.Count);
        Assert.Equal("n1", alerts[0].NodeName);
        Assert.Equal("n1", alerts[1].NodeName);
    }

    #endregion
}
