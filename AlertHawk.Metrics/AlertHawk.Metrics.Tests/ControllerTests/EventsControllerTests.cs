using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Metrics.Tests;

public class EventsControllerTests
{
    private readonly Mock<IClickHouseService> _mockClickHouseService;
    private readonly Mock<ILogger<EventsController>> _mockLogger;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _mockClickHouseService = new Mock<IClickHouseService>();
        _mockLogger = new Mock<ILogger<EventsController>>();
        
        _controller = new EventsController(
            _mockClickHouseService.Object,
            _mockLogger.Object);
        
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

    #region WriteKubernetesEvent Tests

    [Fact]
    public async Task WriteKubernetesEvent_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new KubernetesEventRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            EventName = "test-event",
            EventUid = "event-uid-123",
            InvolvedObjectKind = "Pod",
            InvolvedObjectName = "test-pod",
            InvolvedObjectNamespace = "default",
            EventType = "Normal",
            Reason = "Started",
            Message = "Container started",
            SourceComponent = "kubelet",
            Count = 1,
            FirstTimestamp = DateTime.UtcNow.AddMinutes(-10),
            LastTimestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockClickHouseService
            .Setup(s => s.WriteKubernetesEventAsync(
                request.Namespace,
                request.EventName,
                request.EventUid,
                request.InvolvedObjectKind,
                request.InvolvedObjectName,
                request.InvolvedObjectNamespace,
                request.EventType,
                request.Reason,
                request.Message,
                request.SourceComponent,
                request.Count,
                request.FirstTimestamp,
                request.LastTimestamp,
                request.ClusterName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WriteKubernetesEvent(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var responseType = okResult.Value.GetType();
        var successProperty = responseType.GetProperty("success");
        Assert.NotNull(successProperty);
        Assert.True((bool)successProperty.GetValue(okResult.Value)!);
        _mockClickHouseService.Verify(s => s.WriteKubernetesEventAsync(
            request.Namespace,
            request.EventName,
            request.EventUid,
            request.InvolvedObjectKind,
            request.InvolvedObjectName,
            request.InvolvedObjectNamespace,
            request.EventType,
            request.Reason,
            request.Message,
            request.SourceComponent,
            request.Count,
            request.FirstTimestamp,
            request.LastTimestamp,
            request.ClusterName), Times.Once);
    }

    [Fact]
    public async Task WriteKubernetesEvent_WithEmptyClusterName_UsesNull()
    {
        // Arrange
        var request = new KubernetesEventRequest
        {
            ClusterName = "",
            Namespace = "default",
            EventName = "test-event",
            EventUid = "event-uid-123",
            InvolvedObjectKind = "Pod",
            InvolvedObjectName = "test-pod",
            InvolvedObjectNamespace = "default",
            EventType = "Normal",
            Reason = "Started",
            Message = "Container started",
            SourceComponent = "kubelet",
            Count = 1
        };

        _mockClickHouseService
            .Setup(s => s.WriteKubernetesEventAsync(
                request.Namespace,
                request.EventName,
                request.EventUid,
                request.InvolvedObjectKind,
                request.InvolvedObjectName,
                request.InvolvedObjectNamespace,
                request.EventType,
                request.Reason,
                request.Message,
                request.SourceComponent,
                request.Count,
                request.FirstTimestamp,
                request.LastTimestamp,
                null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WriteKubernetesEvent(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WriteKubernetesEventAsync(
            request.Namespace,
            request.EventName,
            request.EventUid,
            request.InvolvedObjectKind,
            request.InvolvedObjectName,
            request.InvolvedObjectNamespace,
            request.EventType,
            request.Reason,
            request.Message,
            request.SourceComponent,
            request.Count,
            request.FirstTimestamp,
            request.LastTimestamp,
            null), Times.Once);
    }

    [Fact]
    public async Task WriteKubernetesEvent_WithNullTimestamps_HandlesCorrectly()
    {
        // Arrange
        var request = new KubernetesEventRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            EventName = "test-event",
            EventUid = "event-uid-123",
            InvolvedObjectKind = "Pod",
            InvolvedObjectName = "test-pod",
            InvolvedObjectNamespace = "default",
            EventType = "Normal",
            Reason = "Started",
            Message = "Container started",
            SourceComponent = "kubelet",
            Count = 1,
            FirstTimestamp = null,
            LastTimestamp = null
        };

        _mockClickHouseService
            .Setup(s => s.WriteKubernetesEventAsync(
                request.Namespace,
                request.EventName,
                request.EventUid,
                request.InvolvedObjectKind,
                request.InvolvedObjectName,
                request.InvolvedObjectNamespace,
                request.EventType,
                request.Reason,
                request.Message,
                request.SourceComponent,
                request.Count,
                null,
                null,
                request.ClusterName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.WriteKubernetesEvent(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockClickHouseService.Verify(s => s.WriteKubernetesEventAsync(
            request.Namespace,
            request.EventName,
            request.EventUid,
            request.InvolvedObjectKind,
            request.InvolvedObjectName,
            request.InvolvedObjectNamespace,
            request.EventType,
            request.Reason,
            request.Message,
            request.SourceComponent,
            request.Count,
            null,
            null,
            request.ClusterName), Times.Once);
    }

    [Fact]
    public async Task WriteKubernetesEvent_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new KubernetesEventRequest
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            EventName = "test-event",
            EventUid = "event-uid-123",
            InvolvedObjectKind = "Pod",
            InvolvedObjectName = "test-pod",
            InvolvedObjectNamespace = "default",
            EventType = "Normal",
            Reason = "Started",
            Message = "Container started",
            SourceComponent = "kubelet",
            Count = 1
        };

        _mockClickHouseService
            .Setup(s => s.WriteKubernetesEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Write failed"));

        // Act
        var result = await _controller.WriteKubernetesEvent(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetKubernetesEvents Tests

    [Fact]
    public async Task GetKubernetesEvents_WithAllFilters_ReturnsOkWithEvents()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>
        {
            new KubernetesEventDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                EventName = "test-event",
                EventUid = "event-uid-123",
                InvolvedObjectKind = "Pod",
                InvolvedObjectName = "test-pod",
                InvolvedObjectNamespace = "default",
                EventType = "Normal",
                Reason = "Started",
                Message = "Container started",
                SourceComponent = "kubelet",
                Count = 1,
                FirstTimestamp = DateTime.UtcNow.AddMinutes(-10),
                LastTimestamp = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync("default", "Pod", "test-pod", "Normal", 1440, 1000, "test-cluster"))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEvents("default", "Pod", "test-pod", "Normal", 1440, 1000, "test-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<List<KubernetesEventDto>>(okResult.Value);
        Assert.Single(events);
        Assert.Equal("default", events[0].Namespace);
        Assert.Equal("test-pod", events[0].InvolvedObjectName);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync("default", "Pod", "test-pod", "Normal", 1440, 1000, "test-cluster"), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEvents_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>();

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync(null, null, null, null, 1440, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEvents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync(null, null, null, null, 1440, 1000, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEvents_WithNamespaceFilter_ReturnsOkWithEvents()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>
        {
            new KubernetesEventDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "production",
                EventName = "event-1",
                EventUid = "uid-1",
                InvolvedObjectKind = "Pod",
                InvolvedObjectName = "pod-1",
                InvolvedObjectNamespace = "production",
                EventType = "Warning",
                Reason = "Failed",
                Message = "Container failed",
                SourceComponent = "kubelet",
                Count = 3
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync("production", null, null, null, 1440, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEvents("production");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<List<KubernetesEventDto>>(okResult.Value);
        Assert.Single(events);
        Assert.Equal("production", events[0].Namespace);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync("production", null, null, null, 1440, 1000, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEvents_WithEventTypeFilter_ReturnsFilteredEvents()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>
        {
            new KubernetesEventDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                EventName = "warning-event",
                EventUid = "uid-1",
                InvolvedObjectKind = "Pod",
                InvolvedObjectName = "pod-1",
                InvolvedObjectNamespace = "default",
                EventType = "Warning",
                Reason = "Failed",
                Message = "Container failed",
                SourceComponent = "kubelet",
                Count = 1
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync(null, null, null, "Warning", 1440, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEvents(null, null, null, "Warning");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<List<KubernetesEventDto>>(okResult.Value);
        Assert.Single(events);
        Assert.Equal("Warning", events[0].EventType);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync(null, null, null, "Warning", 1440, 1000, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEvents_WithCustomLimit_RespectsLimit()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>();

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync(null, null, null, null, 1440, 500, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEvents(null, null, null, null, 1440, 500);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync(null, null, null, null, 1440, 500, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEvents_WithCustomMinutes_RespectsTimeWindow()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>();

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync(null, null, null, null, 60, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEvents(null, null, null, null, 60);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync(null, null, null, null, 60, 1000, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEvents_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync(null, null, null, null, 1440, 1000, null))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetKubernetesEvents();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetKubernetesEvents_WithMultipleEvents_ReturnsAllEvents()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>
        {
            new KubernetesEventDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                ClusterName = "test-cluster",
                Namespace = "default",
                EventName = "event-1",
                EventUid = "uid-1",
                InvolvedObjectKind = "Pod",
                InvolvedObjectName = "pod-1",
                InvolvedObjectNamespace = "default",
                EventType = "Normal",
                Reason = "Started",
                Message = "Container started",
                SourceComponent = "kubelet",
                Count = 1
            },
            new KubernetesEventDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                ClusterName = "test-cluster",
                Namespace = "default",
                EventName = "event-2",
                EventUid = "uid-2",
                InvolvedObjectKind = "Pod",
                InvolvedObjectName = "pod-2",
                InvolvedObjectNamespace = "default",
                EventType = "Warning",
                Reason = "Failed",
                Message = "Container failed",
                SourceComponent = "kubelet",
                Count = 2
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync("default", null, null, null, 1440, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEvents("default");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<List<KubernetesEventDto>>(okResult.Value);
        Assert.Equal(2, events.Count);
    }

    #endregion

    #region GetKubernetesEventsByNamespace Tests

    [Fact]
    public async Task GetKubernetesEventsByNamespace_WithValidNamespace_ReturnsOkWithEvents()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>
        {
            new KubernetesEventDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "production",
                EventName = "event-1",
                EventUid = "uid-1",
                InvolvedObjectKind = "Pod",
                InvolvedObjectName = "pod-1",
                InvolvedObjectNamespace = "production",
                EventType = "Normal",
                Reason = "Started",
                Message = "Container started",
                SourceComponent = "kubelet",
                Count = 1
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync("production", null, null, null, 1440, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEventsByNamespace("production");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<List<KubernetesEventDto>>(okResult.Value);
        Assert.Single(events);
        Assert.Equal("production", events[0].Namespace);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync("production", null, null, null, 1440, 1000, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEventsByNamespace_WithInvolvedObjectKindFilter_ReturnsFilteredEvents()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>
        {
            new KubernetesEventDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                Namespace = "default",
                EventName = "event-1",
                EventUid = "uid-1",
                InvolvedObjectKind = "Node",
                InvolvedObjectName = "node-1",
                InvolvedObjectNamespace = string.Empty,
                EventType = "Normal",
                Reason = "Ready",
                Message = "Node is ready",
                SourceComponent = "kubelet",
                Count = 1
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync("default", "Node", null, null, 1440, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEventsByNamespace("default", "Node");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<List<KubernetesEventDto>>(okResult.Value);
        Assert.Single(events);
        Assert.Equal("Node", events[0].InvolvedObjectKind);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync("default", "Node", null, null, 1440, 1000, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEventsByNamespace_WithDefaultParameters_UsesDefaults()
    {
        // Arrange
        var expectedEvents = new List<KubernetesEventDto>();

        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync("default", null, null, null, 1440, 1000, null))
            .ReturnsAsync(expectedEvents);

        // Act
        var result = await _controller.GetKubernetesEventsByNamespace("default");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetKubernetesEventsAsync("default", null, null, null, 1440, 1000, null), Times.Once);
    }

    [Fact]
    public async Task GetKubernetesEventsByNamespace_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetKubernetesEventsAsync("test", null, null, null, 1440, 1000, null))
            .ThrowsAsync(new Exception("Query failed"));

        // Act
        var result = await _controller.GetKubernetesEventsByNamespace("test");

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion
}

