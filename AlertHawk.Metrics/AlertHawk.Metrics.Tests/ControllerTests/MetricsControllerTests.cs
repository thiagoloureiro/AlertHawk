using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Producers;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Metrics.Tests;

public class MetricsControllerTests
{
    private readonly Mock<IClickHouseService> _mockClickHouseService;
    private readonly NodeStatusTracker _nodeStatusTracker;
    private readonly Mock<INotificationProducer> _mockNotificationProducer;
    private readonly MetricsController _controller;

    public MetricsControllerTests()
    {
        _mockClickHouseService = new Mock<IClickHouseService>();
        _nodeStatusTracker = new NodeStatusTracker();
        _mockNotificationProducer = new Mock<INotificationProducer>();
        
        _controller = new MetricsController(
            _mockClickHouseService.Object,
            _nodeStatusTracker,
            _mockNotificationProducer.Object);
        
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
                request.ClusterName,
                request.NodeName,
                request.PodState,
                request.RestartCount,
                request.PodAge))
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
            request.ClusterName,
            request.NodeName,
            request.PodState,
            request.RestartCount,
            request.PodAge), Times.Once);
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
                null,
                request.NodeName,
                request.PodState,
                request.RestartCount,
                request.PodAge))
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
            null,
            request.NodeName,
            request.PodState,
            request.RestartCount,
            request.PodAge), Times.Once);
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
                null,
                request.NodeName,
                request.PodState,
                request.RestartCount,
                request.PodAge))
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
            null,
            request.NodeName,
            request.PodState,
            request.RestartCount,
            request.PodAge), Times.Once);
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
                request.ClusterName,
                request.NodeName,
                request.PodState,
                request.RestartCount,
                request.PodAge))
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
            request.ClusterName,
            request.NodeName,
            request.PodState,
            request.RestartCount,
            request.PodAge), Times.Once);
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
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<long?>()))
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
            MemoryCapacityBytes = 8192,
            IsReady = true,
            HasMemoryPressure = false,
            HasDiskPressure = false,
            HasPidPressure = false
        };

        _mockClickHouseService
            .Setup(s => s.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                request.ClusterName,
                request.ClusterEnvironment,
                request.KubernetesVersion,
                request.CloudProvider,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                request.Architecture,
                request.OperatingSystem,
                request.Region,
                request.InstanceType))
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
            request.ClusterName,
            request.ClusterEnvironment,
            request.KubernetesVersion,
            request.CloudProvider,
            request.IsReady,
            request.HasMemoryPressure,
            request.HasDiskPressure,
            request.HasPidPressure,
            request.Architecture,
            request.OperatingSystem,
            request.Region,
            request.InstanceType), Times.Once);
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
                null,
                request.ClusterEnvironment,
                request.KubernetesVersion,
                request.CloudProvider,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                request.Architecture,
                request.OperatingSystem,
                request.Region,
                request.InstanceType))
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
            null,
            request.ClusterEnvironment,
            request.KubernetesVersion,
            request.CloudProvider,
            request.IsReady,
            request.HasMemoryPressure,
            request.HasDiskPressure,
            request.HasPidPressure,
            request.Architecture,
            request.OperatingSystem,
            request.Region,
            request.InstanceType), Times.Once);
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
                null,
                request.ClusterEnvironment,
                request.KubernetesVersion,
                request.CloudProvider,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                request.Architecture,
                request.OperatingSystem,
                request.Region,
                request.InstanceType))
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
            null,
            request.ClusterEnvironment,
            request.KubernetesVersion,
            request.CloudProvider,
            request.IsReady,
            request.HasMemoryPressure,
            request.HasDiskPressure,
            request.HasPidPressure,
            request.Architecture,
            request.OperatingSystem,
            request.Region,
            request.InstanceType), Times.Once);
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
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Write operation failed"));

        // Act
        var result = await _controller.WriteNodeMetric(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region WritePodLog Tests

    [Fact]
    public async Task WritePodLog_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new PodLogRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            LogContent = "2024-01-01 10:00:00 INFO: Application started"
        };

        _mockClickHouseService
            .Setup(s => s.WritePodLogAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.LogContent,
                request.ClusterName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodLog(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var responseType = okResult.Value.GetType();
        var successProperty = responseType.GetProperty("success");
        Assert.NotNull(successProperty);
        Assert.True((bool)successProperty.GetValue(okResult.Value)!);
        _mockClickHouseService.Verify(s => s.WritePodLogAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.LogContent,
            request.ClusterName), Times.Once);
    }

    [Fact]
    public async Task WritePodLog_WithEmptyClusterName_UsesNull()
    {
        // Arrange
        var request = new PodLogRequest
        {
            ClusterName = "",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            LogContent = "Log message"
        };

        _mockClickHouseService
            .Setup(s => s.WritePodLogAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.LogContent,
                null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodLog(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WritePodLogAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.LogContent,
            null), Times.Once);
    }

    [Fact]
    public async Task WritePodLog_WithWhitespaceClusterName_UsesNull()
    {
        // Arrange
        var request = new PodLogRequest
        {
            ClusterName = "   ",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            LogContent = "Log message"
        };

        _mockClickHouseService
            .Setup(s => s.WritePodLogAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.LogContent,
                null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodLog(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WritePodLogAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.LogContent,
            null), Times.Once);
    }

    [Fact]
    public async Task WritePodLog_WithLongLogContent_HandlesCorrectly()
    {
        // Arrange
        var longLogContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i}: Log message content"));
        var request = new PodLogRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            LogContent = longLogContent
        };

        _mockClickHouseService
            .Setup(s => s.WritePodLogAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.LogContent,
                request.ClusterName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WritePodLog(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WritePodLogAsync(
            request.Namespace,
            request.Pod,
            request.Container,
            request.LogContent,
            request.ClusterName), Times.Once);
    }

    [Fact]
    public async Task WritePodLog_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new PodLogRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            Pod = "test-pod",
            Container = "test-container",
            LogContent = "Log message"
        };

        _mockClickHouseService
            .Setup(s => s.WritePodLogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Write failed"));

        // Act
        var result = await _controller.WritePodLog(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetPodLogs Tests

    [Fact]
    public async Task GetPodLogs_WithAllFilters_ReturnsOkWithLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "test-pod",
                Container = "test-container",
                LogContent = "2024-01-01 10:00:00 INFO: Application started"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "test-pod", "test-container", 1440, 100, "test-cluster"))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogs("default", "test-pod", "test-container", 1440, 100, "test-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("default", logs[0].Namespace);
        Assert.Equal("test-pod", logs[0].Pod);
        Assert.Equal("test-container", logs[0].Container);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "test-pod", "test-container", 1440, 100, "test-cluster"), Times.Once);
    }

    [Fact]
    public async Task GetPodLogs_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>();

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync(null, null, null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync(null, null, null, 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogs_WithNamespaceFilter_ReturnsOkWithLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "production",
                Pod = "app-pod",
                Container = "app-container",
                LogContent = "Log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("production", null, null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogs("production");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("production", logs[0].Namespace);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("production", null, null, 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogs_WithCustomLimit_RespectsLimit()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>();

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync(null, null, null, 1440, 50, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogs(null, null, null, 1440, 50);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync(null, null, null, 1440, 50, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogs_WithCustomMinutes_RespectsTimeWindow()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>();

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync(null, null, null, 60, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogs(null, null, null, 60);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync(null, null, null, 60, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogs_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync(null, null, null, 1440, 100, null))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetPodLogs();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetPodLogs_WithMultipleLogs_ReturnsAllLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "pod-1",
                Container = "container-1",
                LogContent = "First log message"
            },
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "pod-2",
                Container = "container-2",
                LogContent = "Second log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", null, null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogs("default");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Equal(2, logs.Count);
    }

    #endregion

    #region GetPodLogsByNamespace Tests

    [Fact]
    public async Task GetPodLogsByNamespace_WithValidNamespace_ReturnsOkWithLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "production",
                Pod = "app-pod",
                Container = "app-container",
                LogContent = "Log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("production", null, null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByNamespace("production");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("production", logs[0].Namespace);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("production", null, null, 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByNamespace_WithPodFilter_ReturnsFilteredLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "specific-pod",
                Container = "container",
                LogContent = "Log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "specific-pod", null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByNamespace("default", "specific-pod");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("specific-pod", logs[0].Pod);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "specific-pod", null, 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByNamespace_WithContainerFilter_ReturnsFilteredLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "pod",
                Container = "specific-container",
                LogContent = "Log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", null, "specific-container", 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByNamespace("default", null, "specific-container");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("specific-container", logs[0].Container);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", null, "specific-container", 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByNamespace_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>();

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", null, null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByNamespace("default");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", null, null, 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByNamespace_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("test", null, null, 1440, 100, null))
            .ThrowsAsync(new Exception("Query failed"));

        // Act
        var result = await _controller.GetPodLogsByNamespace("test");

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetPodLogsByPod Tests

    [Fact]
    public async Task GetPodLogsByPod_WithValidNamespaceAndPod_ReturnsOkWithLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "app-pod",
                Container = "app-container",
                LogContent = "Log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByPod("default", "app-pod");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("default", logs[0].Namespace);
        Assert.Equal("app-pod", logs[0].Pod);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByPod_WithContainerFilter_ReturnsFilteredLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                Pod = "app-pod",
                Container = "specific-container",
                LogContent = "Log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "app-pod", "specific-container", 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByPod("default", "app-pod", "specific-container");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("specific-container", logs[0].Container);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "app-pod", "specific-container", 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByPod_WithCustomMinutes_RespectsTimeWindow()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>();

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "app-pod", null, 60, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByPod("default", "app-pod", null, 60);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "app-pod", null, 60, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByPod_WithCustomLimit_RespectsLimit()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>();

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 50, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByPod("default", "app-pod", null, 1440, 50);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 50, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByPod_WithClusterName_ReturnsFilteredLogs()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>
        {
            new PodLogDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "production-cluster",
                Namespace = "default",
                Pod = "app-pod",
                Container = "app-container",
                LogContent = "Log message"
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 100, "production-cluster"))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByPod("default", "app-pod", null, 1440, 100, "production-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<List<PodLogDto>>(okResult.Value);
        Assert.Single(logs);
        Assert.Equal("production-cluster", logs[0].ClusterName);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 100, "production-cluster"), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByPod_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedLogs = new List<PodLogDto>();

        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 100, null))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetPodLogsByPod("default", "app-pod");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 100, null), Times.Once);
    }

    [Fact]
    public async Task GetPodLogsByPod_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetPodLogsAsync("default", "app-pod", null, 1440, 100, null))
            .ThrowsAsync(new Exception("Query execution failed"));

        // Act
        var result = await _controller.GetPodLogsByPod("default", "app-pod");

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task WriteNodeMetric_WithStatusChange_SendsNotification()
    {
        // Arrange
        var request = new NodeMetricRequest
        {
            ClusterName = "test-cluster",
            NodeName = "node-1",
            CpuUsageCores = 4.0,
            CpuCapacityCores = 8.0,
            MemoryUsageBytes = 4096,
            MemoryCapacityBytes = 8192,
            IsReady = true,
            HasMemoryPressure = false,
            HasDiskPressure = false,
            HasPidPressure = false
        };

        _mockClickHouseService
            .Setup(s => s.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                request.ClusterName,
                request.ClusterEnvironment,
                request.KubernetesVersion,
                request.CloudProvider,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                request.Architecture,
                request.OperatingSystem,
                request.Region,
                request.InstanceType))
            .Returns(Task.CompletedTask);

        // First call - no status change (first time seeing this node)
        var result1 = await _controller.WriteNodeMetric(request);
        var okResult1 = Assert.IsType<OkObjectResult>(result1);
        _mockNotificationProducer.Verify(
            p => p.SendNodeStatusNotification(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool>()),
            Times.Never);

        // Second call with different status - should trigger notification
        request.HasMemoryPressure = true;
        var result2 = await _controller.WriteNodeMetric(request);
        var okResult2 = Assert.IsType<OkObjectResult>(result2);
        _mockNotificationProducer.Verify(
            p => p.SendNodeStatusNotification(
                request.NodeName,
                request.ClusterName,
                request.ClusterEnvironment,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                false), // Not healthy due to memory pressure
            Times.Once);
    }

    [Fact]
    public async Task WriteNodeMetric_WithStatusChangeToHealthy_SendsNotification()
    {
        // Arrange
        var request = new NodeMetricRequest
        {
            ClusterName = "test-cluster",
            NodeName = "node-2",
            CpuUsageCores = 4.0,
            CpuCapacityCores = 8.0,
            MemoryUsageBytes = 4096,
            MemoryCapacityBytes = 8192,
            IsReady = false,
            HasMemoryPressure = true,
            HasDiskPressure = false,
            HasPidPressure = false
        };

        _mockClickHouseService
            .Setup(s => s.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                request.ClusterName,
                request.ClusterEnvironment,
                request.KubernetesVersion,
                request.CloudProvider,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                request.Architecture,
                request.OperatingSystem,
                request.Region,
                request.InstanceType))
            .Returns(Task.CompletedTask);

        // First call - unhealthy status
        await _controller.WriteNodeMetric(request);

        // Second call - status changed to healthy
        request.IsReady = true;
        request.HasMemoryPressure = false;
        await _controller.WriteNodeMetric(request);

        // Verify notification was sent for the status change to healthy
        _mockNotificationProducer.Verify(
            p => p.SendNodeStatusNotification(
                request.NodeName,
                request.ClusterName,
                request.ClusterEnvironment,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                true), // Healthy
            Times.Once);
    }

    #endregion
}