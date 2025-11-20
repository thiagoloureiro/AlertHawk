using System.Text.Json;
using AlertHawk.Metrics;
using AlertHawk.Metrics.Collectors;
using k8s;
using k8s.Models;
using Moq;
using Xunit;

namespace AlertHawk.Metrics.Tests.Collectors;

public class NodeMetricsCollectorTests
{
    private readonly Mock<IKubernetesClientWrapper> _mockKubernetesWrapper;
    private readonly Mock<IMetricsApiClient> _mockApiClient;

    public NodeMetricsCollectorTests()
    {
        _mockKubernetesWrapper = new Mock<IKubernetesClientWrapper>();
        _mockApiClient = new Mock<IMetricsApiClient>();
    }

    [Fact]
    public async Task CollectAsync_WithValidMetrics_CallsApiClient()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1", "4", "8Gi");
        var nodeMetrics = CreateNodeMetricsList("node-1", "1000m", "2Gi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(
                "node-1",
                It.IsAny<double>(), // CPU usage
                4.0, // CPU capacity
                It.IsAny<double>(), // Memory usage
                It.IsAny<double>()), // Memory capacity
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithMultipleNodes_ProcessesAll()
    {
        // Arrange
        var nodeList = CreateNodeListWithMultipleNodes();
        var nodeMetrics = CreateNodeMetricsListWithMultipleNodes();

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert - Should be called twice (once for each node)
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CollectAsync_WithZeroMemory_SkipsApiCall()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1", "4", "8Gi");
        var nodeMetrics = CreateNodeMetricsList("node-1", "1000m", "0");

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_WithApiClientError_ContinuesProcessing()
    {
        // Arrange
        var nodeList = CreateNodeListWithMultipleNodes();
        var nodeMetrics = CreateNodeMetricsListWithMultipleNodes();

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .ThrowsAsync(new Exception("API error"));

        // Act & Assert - Should not throw
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);
    }

    [Fact]
    public async Task CollectAsync_WithMissingNodeCapacity_UsesZeroCapacity()
    {
        // Arrange
        var nodeList = CreateNodeListWithoutCapacity("node-1");
        var nodeMetrics = CreateNodeMetricsList("node-1", "1000m", "2Gi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert - Should use 0.0 for capacity when missing
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(
                "node-1",
                It.IsAny<double>(), // CPU usage
                0.0, // CPU capacity (missing)
                It.IsAny<double>(), // Memory usage
                0.0), // Memory capacity (missing)
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNodeNotInMetricsList_SkipsNode()
    {
        // Arrange
        var nodeList = CreateNodeListWithMultipleNodes();
        // Only include metrics for node-1, not node-2
        var nodeMetrics = CreateNodeMetricsList("node-1", "1000m", "2Gi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert - Should only be called once for node-1
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync("node-1", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            Times.Once);
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync("node-2", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);
    }

    private V1NodeList CreateNodeList(string nodeName, string cpuCapacity, string memoryCapacity)
    {
        return new V1NodeList
        {
            Items = new List<V1Node>
            {
                new V1Node
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = nodeName
                    },
                    Status = new V1NodeStatus
                    {
                        Capacity = new Dictionary<string, ResourceQuantity>
                        {
                            { "cpu", new ResourceQuantity(cpuCapacity) },
                            { "memory", new ResourceQuantity(memoryCapacity) }
                        }
                    }
                }
            }
        };
    }

    private V1NodeList CreateNodeListWithoutCapacity(string nodeName)
    {
        return new V1NodeList
        {
            Items = new List<V1Node>
            {
                new V1Node
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = nodeName
                    },
                    Status = new V1NodeStatus
                    {
                        Capacity = null
                    }
                }
            }
        };
    }

    private V1NodeList CreateNodeListWithMultipleNodes()
    {
        return new V1NodeList
        {
            Items = new List<V1Node>
            {
                new V1Node
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "node-1"
                    },
                    Status = new V1NodeStatus
                    {
                        Capacity = new Dictionary<string, ResourceQuantity>
                        {
                            { "cpu", new ResourceQuantity("4") },
                            { "memory", new ResourceQuantity("8Gi") }
                        }
                    }
                },
                new V1Node
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "node-2"
                    },
                    Status = new V1NodeStatus
                    {
                        Capacity = new Dictionary<string, ResourceQuantity>
                        {
                            { "cpu", new ResourceQuantity("8") },
                            { "memory", new ResourceQuantity("16Gi") }
                        }
                    }
                }
            }
        };
    }

    private object CreateNodeMetricsList(string nodeName, string cpuUsage, string memoryUsage)
    {
        return new NodeMetricsList
        {
            Items = new[]
            {
                new NodeMetricsItem
                {
                    Metadata = new Metadata
                    {
                        Name = nodeName
                    },
                    Usage = new ResourceUsage
                    {
                        Cpu = cpuUsage,
                        Memory = memoryUsage
                    }
                }
            }
        };
    }

    private object CreateNodeMetricsListWithMultipleNodes()
    {
        return new NodeMetricsList
        {
            Items = new[]
            {
                new NodeMetricsItem
                {
                    Metadata = new Metadata
                    {
                        Name = "node-1"
                    },
                    Usage = new ResourceUsage
                    {
                        Cpu = "1000m",
                        Memory = "2Gi"
                    }
                },
                new NodeMetricsItem
                {
                    Metadata = new Metadata
                    {
                        Name = "node-2"
                    },
                    Usage = new ResourceUsage
                    {
                        Cpu = "2000m",
                        Memory = "4Gi"
                    }
                }
            }
        };
    }
}

