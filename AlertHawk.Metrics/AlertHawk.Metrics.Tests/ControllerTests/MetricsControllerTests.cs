using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Metrics.Tests;

public class MetricsControllerTests
{
    private readonly Mock<IClickHouseService> _mockClickHouseService;
    private readonly MetricsController _controller;

    public MetricsControllerTests()
    {
        _mockClickHouseService = new Mock<IClickHouseService>();
        _controller = new MetricsController(_mockClickHouseService.Object);
        
        // Setup controller context for authorization
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

    #region GetMetricsByNamespace Tests

    [Fact]
    public async Task GetMetricsByNamespace_WithNamespaceFilter_ReturnsOkWithMetrics()
    {
        // Arrange
        var expectedMetrics = new List<PodMetricDto>
        {
            new PodMetricDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "test-pod",
                Container = "test-container",
                CpuUsageCores = 0.5,
                CpuLimitCores = 1.0,
                MemoryUsageBytes = 1024
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetMetricsByNamespaceAsync("default", 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetMetricsByNamespace("default", 1440);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsAssignableFrom<List<PodMetricDto>>(okResult.Value);
        Assert.Single(metrics);
        Assert.Equal("default", metrics[0].Namespace);
        _mockClickHouseService.Verify(s => s.GetMetricsByNamespaceAsync("default", 1440), Times.Once);
    }

    [Fact]
    public async Task GetMetricsByNamespace_WithoutNamespaceFilter_ReturnsOkWithMetrics()
    {
        // Arrange
        var expectedMetrics = new List<PodMetricDto>
        {
            new PodMetricDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "kube-system",
                Pod = "test-pod",
                Container = "test-container",
                CpuUsageCores = 0.3,
                CpuLimitCores = 0.5,
                MemoryUsageBytes = 512
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetMetricsByNamespaceAsync(null, 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetMetricsByNamespace(null, 1440);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsAssignableFrom<List<PodMetricDto>>(okResult.Value);
        Assert.Single(metrics);
        _mockClickHouseService.Verify(s => s.GetMetricsByNamespaceAsync(null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetMetricsByNamespace_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedMetrics = new List<PodMetricDto>();

        _mockClickHouseService
            .Setup(s => s.GetMetricsByNamespaceAsync(null, 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetMetricsByNamespace();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetMetricsByNamespaceAsync(null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetMetricsByNamespace_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetMetricsByNamespaceAsync(null, 1440))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetMetricsByNamespace();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        _mockClickHouseService.Verify(s => s.GetMetricsByNamespaceAsync(null, 1440), Times.Once);
    }

    #endregion

    #region GetMetricsByNamespaceName Tests

    [Fact]
    public async Task GetMetricsByNamespaceName_WithValidNamespace_ReturnsOkWithMetrics()
    {
        // Arrange
        var expectedMetrics = new List<PodMetricDto>
        {
            new PodMetricDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "production",
                Pod = "app-pod",
                Container = "app-container",
                CpuUsageCores = 1.5,
                CpuLimitCores = 2.0,
                MemoryUsageBytes = 2048
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetMetricsByNamespaceAsync("production", 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetMetricsByNamespaceName("production", 1440);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsAssignableFrom<List<PodMetricDto>>(okResult.Value);
        Assert.Single(metrics);
        Assert.Equal("production", metrics[0].Namespace);
        _mockClickHouseService.Verify(s => s.GetMetricsByNamespaceAsync("production", 1440), Times.Once);
    }

    [Fact]
    public async Task GetMetricsByNamespaceName_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedMetrics = new List<PodMetricDto>();

        _mockClickHouseService
            .Setup(s => s.GetMetricsByNamespaceAsync("default", 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetMetricsByNamespaceName("default");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetMetricsByNamespaceAsync("default", 1440), Times.Once);
    }

    [Fact]
    public async Task GetMetricsByNamespaceName_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetMetricsByNamespaceAsync("test", 1440))
            .ThrowsAsync(new Exception("Query failed"));

        // Act
        var result = await _controller.GetMetricsByNamespaceName("test");

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetNodeMetrics Tests

    [Fact]
    public async Task GetNodeMetrics_WithNodeNameFilter_ReturnsOkWithMetrics()
    {
        // Arrange
        var expectedMetrics = new List<NodeMetricDto>
        {
            new NodeMetricDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                CpuUsageCores = 4.0,
                CpuCapacityCores = 8.0,
                MemoryUsageBytes = 4096,
                MemoryCapacityBytes = 8192
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetNodeMetricsAsync("node-1", 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetNodeMetrics("node-1", 1440);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsAssignableFrom<List<NodeMetricDto>>(okResult.Value);
        Assert.Single(metrics);
        Assert.Equal("node-1", metrics[0].NodeName);
        _mockClickHouseService.Verify(s => s.GetNodeMetricsAsync("node-1", 1440), Times.Once);
    }

    [Fact]
    public async Task GetNodeMetrics_WithoutNodeNameFilter_ReturnsOkWithMetrics()
    {
        // Arrange
        var expectedMetrics = new List<NodeMetricDto>
        {
            new NodeMetricDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-2",
                CpuUsageCores = 2.0,
                CpuCapacityCores = 4.0,
                MemoryUsageBytes = 2048,
                MemoryCapacityBytes = 4096
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetNodeMetricsAsync(null, 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetNodeMetrics(null, 1440);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsAssignableFrom<List<NodeMetricDto>>(okResult.Value);
        Assert.Single(metrics);
        _mockClickHouseService.Verify(s => s.GetNodeMetricsAsync(null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetNodeMetrics_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedMetrics = new List<NodeMetricDto>();

        _mockClickHouseService
            .Setup(s => s.GetNodeMetricsAsync(null, 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetNodeMetrics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetNodeMetricsAsync(null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetNodeMetrics_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetNodeMetricsAsync(null, 1440))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetNodeMetrics();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetNodeMetricsByName Tests

    [Fact]
    public async Task GetNodeMetricsByName_WithValidNodeName_ReturnsOkWithMetrics()
    {
        // Arrange
        var expectedMetrics = new List<NodeMetricDto>
        {
            new NodeMetricDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "worker-node-1",
                CpuUsageCores = 6.0,
                CpuCapacityCores = 16.0,
                MemoryUsageBytes = 8192,
                MemoryCapacityBytes = 16384
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetNodeMetricsAsync("worker-node-1", 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetNodeMetricsByName("worker-node-1", 1440);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsAssignableFrom<List<NodeMetricDto>>(okResult.Value);
        Assert.Single(metrics);
        Assert.Equal("worker-node-1", metrics[0].NodeName);
        _mockClickHouseService.Verify(s => s.GetNodeMetricsAsync("worker-node-1", 1440), Times.Once);
    }

    [Fact]
    public async Task GetNodeMetricsByName_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedMetrics = new List<NodeMetricDto>();

        _mockClickHouseService
            .Setup(s => s.GetNodeMetricsAsync("node-1", 1440))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetNodeMetricsByName("node-1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetNodeMetricsAsync("node-1", 1440), Times.Once);
    }

    [Fact]
    public async Task GetNodeMetricsByName_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetNodeMetricsAsync("node-1", 1440))
            .ThrowsAsync(new Exception("Query execution failed"));

        // Act
        var result = await _controller.GetNodeMetricsByName("node-1");

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region WritePodMetric Tests

    [Fact]
    public async Task WritePodMetric_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new PodMetricRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            CpuUsageCores = 0.5,
            CpuLimitCores = 1.0,
            MemoryUsageBytes = 1024
        };

        _mockClickHouseService
            .Setup(s => s.WriteMetricsAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.CpuUsageCores,
                request.CpuLimitCores,
                request.MemoryUsageBytes,
                request.ClusterName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodMetric(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var responseType = okResult.Value.GetType();
        var successProperty = responseType.GetProperty("success");
        Assert.NotNull(successProperty);
        Assert.True((bool)successProperty.GetValue(okResult.Value)!);
        _mockClickHouseService.Verify(s => s.WriteMetricsAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.CpuUsageCores,
            request.CpuLimitCores,
            request.MemoryUsageBytes,
            request.ClusterName), Times.Once);
    }

    [Fact]
    public async Task WritePodMetric_WithEmptyClusterName_UsesNull()
    {
        // Arrange
        var request = new PodMetricRequest
        {
            ClusterName = "",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            CpuUsageCores = 0.5,
            CpuLimitCores = 1.0,
            MemoryUsageBytes = 1024
        };

        _mockClickHouseService
            .Setup(s => s.WriteMetricsAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.CpuUsageCores,
                request.CpuLimitCores,
                request.MemoryUsageBytes,
                null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodMetric(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WriteMetricsAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.CpuUsageCores,
            request.CpuLimitCores,
            request.MemoryUsageBytes,
            null), Times.Once);
    }

    [Fact]
    public async Task WritePodMetric_WithWhitespaceClusterName_UsesNull()
    {
        // Arrange
        var request = new PodMetricRequest
        {
            ClusterName = "   ",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            CpuUsageCores = 0.5,
            CpuLimitCores = 1.0,
            MemoryUsageBytes = 1024
        };

        _mockClickHouseService
            .Setup(s => s.WriteMetricsAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.CpuUsageCores,
                request.CpuLimitCores,
                request.MemoryUsageBytes,
                null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodMetric(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WriteMetricsAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.CpuUsageCores,
            request.CpuLimitCores,
            request.MemoryUsageBytes,
            null), Times.Once);
    }

    [Fact]
    public async Task WritePodMetric_WithNullCpuLimit_HandlesCorrectly()
    {
        // Arrange
        var request = new PodMetricRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            CpuUsageCores = 0.5,
            CpuLimitCores = null,
            MemoryUsageBytes = 1024
        };

        _mockClickHouseService
            .Setup(s => s.WriteMetricsAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.CpuUsageCores,
                null,
                request.MemoryUsageBytes,
                request.ClusterName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodMetric(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WriteMetricsAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.CpuUsageCores,
            null,
            request.MemoryUsageBytes,
            request.ClusterName), Times.Once);
    }

    [Fact]
    public async Task WritePodMetric_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new PodMetricRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            CpuUsageCores = 0.5,
            CpuLimitCores = 1.0,
            MemoryUsageBytes = 1024
        };

        _mockClickHouseService
            .Setup(s => s.WriteMetricsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<double?>(),
                It.IsAny<double>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Write failed"));

        // Act
        var result = await _controller.WritePodMetric(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region WriteNodeMetric Tests

    [Fact]
    public async Task WriteNodeMetric_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new NodeMetricRequest
        {
            ClusterName = "test-cluster",
            NodeName = "node-1",
            CpuUsageCores = 4.0,
            CpuCapacityCores = 8.0,
            MemoryUsageBytes = 4096,
            MemoryCapacityBytes = 8192
        };

        _mockClickHouseService
            .Setup(s => s.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                request.ClusterName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WriteNodeMetric(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var responseType = okResult.Value.GetType();
        var successProperty = responseType.GetProperty("success");
        Assert.NotNull(successProperty);
        Assert.True((bool)successProperty.GetValue(okResult.Value)!);
        _mockClickHouseService.Verify(s => s.WriteNodeMetricsAsync(
            request.NodeName,
            request.CpuUsageCores,
            request.CpuCapacityCores,
            request.MemoryUsageBytes,
            request.MemoryCapacityBytes,
            request.ClusterName), Times.Once);
    }

    [Fact]
    public async Task WriteNodeMetric_WithEmptyClusterName_UsesNull()
    {
        // Arrange
        var request = new NodeMetricRequest
        {
            ClusterName = "",
            NodeName = "node-1",
            CpuUsageCores = 4.0,
            CpuCapacityCores = 8.0,
            MemoryUsageBytes = 4096,
            MemoryCapacityBytes = 8192
        };

        _mockClickHouseService
            .Setup(s => s.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WriteNodeMetric(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WriteNodeMetricsAsync(
            request.NodeName,
            request.CpuUsageCores,
            request.CpuCapacityCores,
            request.MemoryUsageBytes,
            request.MemoryCapacityBytes,
            null), Times.Once);
    }

    [Fact]
    public async Task WriteNodeMetric_WithWhitespaceClusterName_UsesNull()
    {
        // Arrange
        var request = new NodeMetricRequest
        {
            ClusterName = "   ",
            NodeName = "node-1",
            CpuUsageCores = 4.0,
            CpuCapacityCores = 8.0,
            MemoryUsageBytes = 4096,
            MemoryCapacityBytes = 8192
        };

        _mockClickHouseService
            .Setup(s => s.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WriteNodeMetric(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WriteNodeMetricsAsync(
            request.NodeName,
            request.CpuUsageCores,
            request.CpuCapacityCores,
            request.MemoryUsageBytes,
            request.MemoryCapacityBytes,
            null), Times.Once);
    }

    [Fact]
    public async Task WriteNodeMetric_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new NodeMetricRequest
        {
            ClusterName = "test-cluster",
            NodeName = "node-1",
            CpuUsageCores = 4.0,
            CpuCapacityCores = 8.0,
            MemoryUsageBytes = 4096,
            MemoryCapacityBytes = 8192
        };

        _mockClickHouseService
            .Setup(s => s.WriteNodeMetricsAsync(
                It.IsAny<string>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Write operation failed"));

        // Act
        var result = await _controller.WriteNodeMetric(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion
}