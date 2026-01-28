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

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1"))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
                It.IsAny<double>(), // Memory capacity
                It.IsAny<double>(), // Disk read bytes
                It.IsAny<double>(), // Disk write bytes
                It.IsAny<double>(), // Disk read ops
                It.IsAny<double>(), // Disk write ops
                It.IsAny<double>(), // Network usage
                It.IsAny<string>(), // Kubernetes version
                It.IsAny<string>(), // Cloud provider
                It.IsAny<bool?>(), // IsReady
                It.IsAny<bool?>(), // HasMemoryPressure
                It.IsAny<bool?>(), // HasDiskPressure
                It.IsAny<bool?>(), // HasPidPressure
                It.IsAny<string>(), // Architecture
                It.IsAny<string>(), // OperatingSystem
                It.IsAny<string>(), // Region
                It.IsAny<string>()), // InstanceType
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

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert - Should be called twice (once for each node)
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
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

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1"))
            .ReturnsAsync("{}");

        // Assert
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
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

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1"))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
                0.0, // Memory capacity (missing)
                It.IsAny<double>(), // Disk read bytes
                It.IsAny<double>(), // Disk write bytes
                It.IsAny<double>(), // Disk read ops
                It.IsAny<double>(), // Disk write ops
                It.IsAny<double>(), // Network usage
                It.IsAny<string>(), // Kubernetes version
                It.IsAny<string>(), // Cloud provider
                It.IsAny<bool?>(), // IsReady
                It.IsAny<bool?>(), // HasMemoryPressure
                It.IsAny<bool?>(), // HasDiskPressure
                It.IsAny<bool?>(), // HasPidPressure
                It.IsAny<string>(), // Architecture
                It.IsAny<string>(), // OperatingSystem
                It.IsAny<string>(), // Region
                It.IsAny<string>()), // InstanceType
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

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert - Should only be called once for node-1
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync("node-1", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync("node-2", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
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
                        Memory = memoryUsage,
                        EphemeralStorage = null,
                        Storage = null,
                        Network = null
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

    [Fact]
    public async Task CollectAsync_WithDiskIOMetrics_ExtractsAndSendsDiskIO()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1", "4", "8Gi");
        var nodeMetrics = CreateNodeMetricsList("node-1", "1000m", "2Gi");
        var statsSummaryJson = @"{
            ""node"": {
                ""fs"": {
                    ""ioStats"": {
                        ""readBytes"": 10737418240,
                        ""writeBytes"": 5368709120,
                        ""readOps"": 1000,
                        ""writeOps"": 500
                    }
                }
            }
        }";

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1"))
            .ReturnsAsync(statsSummaryJson);

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert - Verify disk I/O is extracted
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(
                "node-1",
                It.IsAny<double>(), // CPU usage
                It.IsAny<double>(), // CPU capacity
                It.IsAny<double>(), // Memory usage
                It.IsAny<double>(), // Memory capacity
                10737418240.0, // Disk read bytes (10Gi)
                5368709120.0, // Disk write bytes (5Gi)
                1000.0, // Disk read ops
                500.0, // Disk write ops
                It.IsAny<double>(), // Network usage
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNetworkMetrics_ExtractsAndSendsNetworkUsage()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1", "4", "8Gi");
        var nodeMetrics = CreateNodeMetricsListWithNetwork("node-1", "1000m", "2Gi", "5Gi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNodeAsync())
            .ReturnsAsync(nodeList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "nodes"))
            .ReturnsAsync(nodeMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1"))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WriteNodeMetricAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await NodeMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, _mockApiClient.Object);

        // Assert - Verify network usage is extracted (5Gi = 5368709120 bytes)
        _mockApiClient.Verify(
            a => a.WriteNodeMetricAsync(
                "node-1",
                It.IsAny<double>(), // CPU usage
                It.IsAny<double>(), // CPU capacity
                It.IsAny<double>(), // Memory usage
                It.IsAny<double>(), // Memory capacity
                It.IsAny<double>(), // Disk read bytes
                It.IsAny<double>(), // Disk write bytes
                It.IsAny<double>(), // Disk read ops
                It.IsAny<double>(), // Disk write ops
                5368709120.0, // Network usage (5Gi)
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    private object CreateNodeMetricsListWithNetwork(string nodeName, string cpuUsage, string memoryUsage, string networkUsage)
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
                        Memory = memoryUsage,
                        EphemeralStorage = null,
                        Storage = null,
                        Network = networkUsage
                    }
                }
            }
        };
    }
}

