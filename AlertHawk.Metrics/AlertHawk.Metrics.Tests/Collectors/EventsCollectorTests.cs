using AlertHawk.Metrics;
using AlertHawk.Metrics.Collectors;
using k8s.Models;
using Moq;
using Xunit;

namespace AlertHawk.Metrics.Tests.Collectors;

public class EventsCollectorTests
{
    private readonly Mock<IKubernetesClientWrapper> _mockKubernetesWrapper;
    private readonly Mock<IMetricsApiClient> _mockApiClient;

    public EventsCollectorTests()
    {
        _mockKubernetesWrapper = new Mock<IKubernetesClientWrapper>();
        _mockApiClient = new Mock<IMetricsApiClient>();
    }

    [Fact]
    public async Task CollectAsync_WithValidEvents_CallsApiClient()
    {
        // Arrange - Use unique namespace to avoid test interference
        var uniqueNamespace = $"test-ns-valid-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace };
        var eventList = CreateEventList(uniqueNamespace, "test-event", "Pod", "test-pod");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // Act
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
                uniqueNamespace,
                "test-event",
                It.IsAny<string>(),
                "Pod",
                "test-pod",
                uniqueNamespace,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithMultipleNamespaces_ProcessesAll()
    {
        // Arrange - Use unique namespaces to avoid test interference
        var uniqueNamespace1 = $"test-ns-1-{Guid.NewGuid()}";
        var uniqueNamespace2 = $"test-ns-2-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace1, uniqueNamespace2 };
        // Use recent timestamps to ensure events are processed
        var recentTime = DateTime.UtcNow.AddMinutes(-1);
        var eventList1 = CreateEventListWithTimestamps(uniqueNamespace1, "event1", "Pod", "pod1", recentTime, recentTime);
        var eventList2 = CreateEventListWithTimestamps(uniqueNamespace2, "event2", "Node", "node1", recentTime, recentTime);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace1))
            .ReturnsAsync(eventList1);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace2))
            .ReturnsAsync(eventList2);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // Act
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockKubernetesWrapper.Verify(
            c => c.ListNamespacedEventAsync(uniqueNamespace1),
            Times.Once);
        _mockKubernetesWrapper.Verify(
            c => c.ListNamespacedEventAsync(uniqueNamespace2),
            Times.Once);
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CollectAsync_WithNewEvents_ProcessesNewEvents()
    {
        // Arrange - Use unique namespace to avoid test interference
        var uniqueNamespace = $"test-ns-new-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace };
        var firstSeen = DateTime.UtcNow.AddMinutes(-5);
        var lastModified = DateTime.UtcNow.AddMinutes(-2);
        var eventList = CreateEventListWithTimestamps(uniqueNamespace, "new-event", "Pod", "test-pod", firstSeen, lastModified);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // Act
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
                uniqueNamespace,
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
                firstSeen,
                lastModified),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithOldEvents_DoesNotProcessOldEvents()
    {
        // Arrange - Use unique namespace to avoid test interference
        var uniqueNamespace = $"test-ns-old-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace };
        var firstSeen = DateTime.UtcNow.AddMinutes(-10);
        var lastModified = DateTime.UtcNow.AddMinutes(-5);
        var eventList = CreateEventListWithTimestamps(uniqueNamespace, "old-event", "Pod", "test-pod", firstSeen, lastModified);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // First collection - should process the event
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Reset mock
        _mockApiClient.Reset();

        // Second collection with same old event - should not process it again
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert - second collection should not call the API
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_WithUpdatedEvents_ProcessesUpdatedEvents()
    {
        // Arrange - Use unique namespace to avoid test interference
        var uniqueNamespace = $"test-ns-updated-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace };
        var firstSeen = DateTime.UtcNow.AddHours(-1);
        var lastModified = DateTime.UtcNow.AddMinutes(-1);
        var eventList = CreateEventListWithTimestamps(uniqueNamespace, "updated-event", "Pod", "test-pod", firstSeen, lastModified);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // First collection
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Wait a bit to ensure the updated timestamp is definitely after the last collection time
        await Task.Delay(100);

        // Reset and create updated event (newer lastModified)
        _mockApiClient.Reset();
        var updatedLastModified = DateTime.UtcNow;
        var updatedEventList = CreateEventListWithTimestamps(uniqueNamespace, "updated-event", "Pod", "test-pod", firstSeen, updatedLastModified);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(updatedEventList);

        // Second collection - should process the updated event
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
                uniqueNamespace,
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
                updatedLastModified),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithApiClientError_ContinuesProcessing()
    {
        // Arrange - Use unique namespace to avoid test interference
        var uniqueNamespace = $"test-ns-error-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace };
        var eventList = CreateEventList(uniqueNamespace, "test-event", "Pod", "test-pod");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("API error"));

        // Act & Assert - Should not throw
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);
    }

    [Fact]
    public async Task CollectAsync_WithNamespaceError_ContinuesProcessing()
    {
        // Arrange
        var namespaces = new[] { "default", "kube-system" };
        // Use a unique namespace name to avoid interference from other tests
        var uniqueNamespace = $"test-ns-{Guid.NewGuid()}";
        namespaces = new[] { uniqueNamespace, "kube-system" };
        var recentTime = DateTime.UtcNow.AddMinutes(-1);
        var eventList = CreateEventListWithTimestamps(uniqueNamespace, "event1", "Pod", "pod1", recentTime, recentTime);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync("kube-system"))
            .ThrowsAsync(new Exception("Namespace error"));

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - Should not throw
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Should still process the first namespace
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithMultipleEvents_ProcessesAll()
    {
        // Arrange - Use unique namespace to avoid test interference
        var uniqueNamespace = $"test-ns-multi-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace };
        var eventList = CreateEventListWithMultipleEvents(uniqueNamespace);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // Act
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert - Should be called for each event
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
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
                It.IsAny<DateTime?>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task CollectAsync_WithEventCount_IncludesCount()
    {
        // Arrange - Use unique namespace to avoid test interference
        var uniqueNamespace = $"test-ns-count-{Guid.NewGuid()}";
        var namespaces = new[] { uniqueNamespace };
        // Use recent timestamps to ensure event is processed
        var recentTime = DateTime.UtcNow.AddMinutes(-1);
        var eventList = CreateEventListWithCountAndTimestamps(uniqueNamespace, "test-event", "Pod", "test-pod", 5, recentTime, recentTime);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedEventAsync(uniqueNamespace))
            .ReturnsAsync(eventList);

        _mockApiClient
            .Setup(a => a.WriteKubernetesEventAsync(
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
                5, // Count should be 5
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        // Act
        await EventsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WriteKubernetesEventAsync(
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
                5,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()),
            Times.Once);
    }

    private Corev1EventList CreateEventList(string @namespace, string eventName, string involvedObjectKind, string involvedObjectName)
    {
        // Use recent timestamps to ensure events are processed
        var recentTime = DateTime.UtcNow.AddMinutes(-1);
        return new Corev1EventList
        {
            Items = new List<Corev1Event>
            {
                new Corev1Event
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = eventName,
                        NamespaceProperty = @namespace,
                        Uid = Guid.NewGuid().ToString()
                    },
                    InvolvedObject = new V1ObjectReference
                    {
                        Kind = involvedObjectKind,
                        Name = involvedObjectName,
                        NamespaceProperty = @namespace
                    },
                    Type = "Normal",
                    Reason = "Started",
                    Message = "Container started",
                    Source = new V1EventSource
                    {
                        Component = "kubelet"
                    },
                    Count = 1,
                    FirstTimestamp = recentTime,
                    LastTimestamp = recentTime
                }
            }
        };
    }

    private Corev1EventList CreateEventListWithTimestamps(string @namespace, string eventName, string involvedObjectKind, string involvedObjectName, DateTime firstTimestamp, DateTime lastTimestamp)
    {
        return new Corev1EventList
        {
            Items = new List<Corev1Event>
            {
                new Corev1Event
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = eventName,
                        NamespaceProperty = @namespace,
                        Uid = Guid.NewGuid().ToString()
                    },
                    InvolvedObject = new V1ObjectReference
                    {
                        Kind = involvedObjectKind,
                        Name = involvedObjectName,
                        NamespaceProperty = @namespace
                    },
                    Type = "Normal",
                    Reason = "Started",
                    Message = "Container started",
                    Source = new V1EventSource
                    {
                        Component = "kubelet"
                    },
                    Count = 1,
                    FirstTimestamp = firstTimestamp,
                    LastTimestamp = lastTimestamp
                }
            }
        };
    }

    private Corev1EventList CreateEventListWithCount(string @namespace, string eventName, string involvedObjectKind, string involvedObjectName, int count)
    {
        return new Corev1EventList
        {
            Items = new List<Corev1Event>
            {
                new Corev1Event
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = eventName,
                        NamespaceProperty = @namespace,
                        Uid = Guid.NewGuid().ToString()
                    },
                    InvolvedObject = new V1ObjectReference
                    {
                        Kind = involvedObjectKind,
                        Name = involvedObjectName,
                        NamespaceProperty = @namespace
                    },
                    Type = "Normal",
                    Reason = "Started",
                    Message = "Container started",
                    Source = new V1EventSource
                    {
                        Component = "kubelet"
                    },
                    Count = count,
                    FirstTimestamp = DateTime.UtcNow.AddMinutes(-10),
                    LastTimestamp = DateTime.UtcNow.AddMinutes(-5)
                }
            }
        };
    }

    private Corev1EventList CreateEventListWithCountAndTimestamps(string @namespace, string eventName, string involvedObjectKind, string involvedObjectName, int count, DateTime firstTimestamp, DateTime lastTimestamp)
    {
        return new Corev1EventList
        {
            Items = new List<Corev1Event>
            {
                new Corev1Event
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = eventName,
                        NamespaceProperty = @namespace,
                        Uid = Guid.NewGuid().ToString()
                    },
                    InvolvedObject = new V1ObjectReference
                    {
                        Kind = involvedObjectKind,
                        Name = involvedObjectName,
                        NamespaceProperty = @namespace
                    },
                    Type = "Normal",
                    Reason = "Started",
                    Message = "Container started",
                    Source = new V1EventSource
                    {
                        Component = "kubelet"
                    },
                    Count = count,
                    FirstTimestamp = firstTimestamp,
                    LastTimestamp = lastTimestamp
                }
            }
        };
    }

    private Corev1EventList CreateEventListWithMultipleEvents(string @namespace)
    {
        // Use recent timestamps to ensure all events are processed
        var recentTime1 = DateTime.UtcNow.AddMinutes(-2);
        var recentTime2 = DateTime.UtcNow.AddMinutes(-1);
        var recentTime3 = DateTime.UtcNow;
        
        return new Corev1EventList
        {
            Items = new List<Corev1Event>
            {
                new Corev1Event
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "event1",
                        NamespaceProperty = @namespace,
                        Uid = Guid.NewGuid().ToString()
                    },
                    InvolvedObject = new V1ObjectReference
                    {
                        Kind = "Pod",
                        Name = "pod1",
                        NamespaceProperty = @namespace
                    },
                    Type = "Normal",
                    Reason = "Started",
                    Message = "Container started",
                    Source = new V1EventSource
                    {
                        Component = "kubelet"
                    },
                    Count = 1,
                    FirstTimestamp = recentTime1,
                    LastTimestamp = recentTime1
                },
                new Corev1Event
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "event2",
                        NamespaceProperty = @namespace,
                        Uid = Guid.NewGuid().ToString()
                    },
                    InvolvedObject = new V1ObjectReference
                    {
                        Kind = "Pod",
                        Name = "pod2",
                        NamespaceProperty = @namespace
                    },
                    Type = "Warning",
                    Reason = "Failed",
                    Message = "Container failed",
                    Source = new V1EventSource
                    {
                        Component = "kubelet"
                    },
                    Count = 1,
                    FirstTimestamp = recentTime2,
                    LastTimestamp = recentTime2
                },
                new Corev1Event
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "event3",
                        NamespaceProperty = @namespace,
                        Uid = Guid.NewGuid().ToString()
                    },
                    InvolvedObject = new V1ObjectReference
                    {
                        Kind = "Node",
                        Name = "node1",
                        NamespaceProperty = null
                    },
                    Type = "Normal",
                    Reason = "Ready",
                    Message = "Node is ready",
                    Source = new V1EventSource
                    {
                        Component = "kubelet"
                    },
                    Count = 1,
                    FirstTimestamp = recentTime3,
                    LastTimestamp = recentTime3
                }
            }
        };
    }
}

