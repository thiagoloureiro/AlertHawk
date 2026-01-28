using System.Text.Json;
using AlertHawk.Metrics;
using AlertHawk.Metrics.Collectors;
using k8s;
using k8s.Models;
using Moq;
using Xunit;

namespace AlertHawk.Metrics.Tests.Collectors;

public class PodMetricsCollectorTests
{
    private readonly Mock<IKubernetesClientWrapper> _mockKubernetesWrapper;
    private readonly Mock<IMetricsApiClient> _mockApiClient;

    public PodMetricsCollectorTests()
    {
        _mockKubernetesWrapper = new Mock<IKubernetesClientWrapper>();
        _mockApiClient = new Mock<IMetricsApiClient>();
    }

    [Fact]
    public async Task CollectAsync_WithValidMetrics_CallsApiClient()
    {
        // Arrange
        var namespaces = new[] { "default" };
        var podList = CreatePodList("default", "test-pod");
        var podMetrics = CreatePodMetricsList("default", "test-pod", "test-container", "100m", "128Mi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Returns(Task.CompletedTask);

        // Act
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WritePodMetricAsync(
                "default",
                "test-pod",
                "test-container",
                It.IsAny<double>(),
                It.IsAny<double?>(),
                It.IsAny<double>(),
                It.IsAny<double>(), // Disk read bytes
                It.IsAny<double>(), // Disk write bytes
                It.IsAny<double>(), // Disk read ops
                It.IsAny<double>(), // Disk write ops
                It.IsAny<double>(), // Network usage
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<long?>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithMultipleNamespaces_ProcessesAll()
    {
        // Arrange
        var namespaces = new[] { "default", "kube-system" };
        var podList1 = CreatePodList("default", "pod1");
        var podList2 = CreatePodList("kube-system", "pod2");
        var podMetrics = CreatePodMetricsList("default", "pod1", "container1", "100m", "128Mi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList1);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("kube-system"))
            .ReturnsAsync(podList2);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Returns(Task.CompletedTask);

        // Act
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockKubernetesWrapper.Verify(
            c => c.ListNamespacedPodAsync("default"),
            Times.Once);
        _mockKubernetesWrapper.Verify(
            c => c.ListNamespacedPodAsync("kube-system"),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithCpuLimit_IncludesCpuLimit()
    {
        // Arrange
        var namespaces = new[] { "default" };
        var podList = CreatePodListWithCpuLimit("default", "test-pod", "test-container", "500m");
        var podMetrics = CreatePodMetricsList("default", "test-pod", "test-container", "100m", "128Mi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Returns(Task.CompletedTask);

        // Act
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert
        _mockApiClient.Verify(
            a => a.WritePodMetricAsync(
                "default",
                "test-pod",
                "test-container",
                It.IsAny<double>(),
                0.5, // CPU limit should be 0.5 cores
                It.IsAny<double>(),
                It.IsAny<double>(), // Disk read bytes
                It.IsAny<double>(), // Disk write bytes
                It.IsAny<double>(), // Disk read ops
                It.IsAny<double>(), // Disk write ops
                It.IsAny<double>(), // Network usage
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<long?>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithApiClientError_ContinuesProcessing()
    {
        // Arrange
        var namespaces = new[] { "default" };
        var podList = CreatePodList("default", "test-pod");
        var podMetrics = CreatePodMetricsList("default", "test-pod", "test-container", "100m", "128Mi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .ThrowsAsync(new Exception("API error"));

        // Act & Assert - Should not throw
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);
    }

    [Fact]
    public async Task CollectAsync_WithNamespaceError_ContinuesProcessing()
    {
        // Arrange
        var namespaces = new[] { "default", "kube-system" };
        var podList = CreatePodList("default", "test-pod");
        var podMetrics = CreatePodMetricsList("default", "test-pod", "test-container", "100m", "128Mi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList);

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("kube-system"))
            .ThrowsAsync(new Exception("Namespace error"));

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - Should not throw
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        // Should still process the default namespace
        _mockApiClient.Verify(
            a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithMultipleContainers_ProcessesAll()
    {
        // Arrange
        var namespaces = new[] { "default" };
        var podList = CreatePodList("default", "test-pod");
        var podMetrics = CreatePodMetricsListWithMultipleContainers("default", "test-pod");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Returns(Task.CompletedTask);

        // Act
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        // Assert - Should be called twice (once for each container)
        _mockApiClient.Verify(
            a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()),
            Times.Exactly(2));
    }

    private V1PodList CreatePodList(string @namespace, string podName)
    {
        return new V1PodList
        {
            Items = new List<V1Pod>
            {
                new V1Pod
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = podName,
                        NamespaceProperty = @namespace,
                        CreationTimestamp = DateTime.UtcNow.AddHours(-1)
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "test-container"
                            }
                        }
                    },
                    Status = new V1PodStatus
                    {
                        Phase = "Running",
                        ContainerStatuses = new List<V1ContainerStatus>
                        {
                            new V1ContainerStatus
                            {
                                Name = "test-container",
                                RestartCount = 0
                            }
                        }
                    }
                }
            }
        };
    }

    private V1PodList CreatePodListWithCpuLimit(string @namespace, string podName, string containerName, string cpuLimit)
    {
        return new V1PodList
        {
            Items = new List<V1Pod>
            {
                new V1Pod
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = podName,
                        NamespaceProperty = @namespace,
                        CreationTimestamp = DateTime.UtcNow.AddHours(-1)
                    },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = containerName,
                                Resources = new V1ResourceRequirements
                                {
                                    Limits = new Dictionary<string, ResourceQuantity>
                                    {
                                        { "cpu", new ResourceQuantity(cpuLimit) }
                                    }
                                }
                            }
                        }
                    },
                    Status = new V1PodStatus
                    {
                        Phase = "Running",
                        ContainerStatuses = new List<V1ContainerStatus>
                        {
                            new V1ContainerStatus
                            {
                                Name = containerName,
                                RestartCount = 0
                            }
                        }
                    }
                }
            }
        };
    }

    private object CreatePodMetricsList(string @namespace, string podName, string containerName, string cpuUsage, string memoryUsage)
    {
        return new PodMetricsList
        {
            Items = new[]
            {
                new PodMetricsItem
                {
                    Metadata = new Metadata
                    {
                        Name = podName,
                        Namespace = @namespace
                    },
                    Containers = new[]
                    {
                        new ContainerMetrics
                        {
                            Name = containerName,
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
                }
            }
        };
    }

    private object CreatePodMetricsListWithMultipleContainers(string @namespace, string podName)
    {
        return new PodMetricsList
        {
            Items = new[]
            {
                new PodMetricsItem
                {
                    Metadata = new Metadata
                    {
                        Name = podName,
                        Namespace = @namespace
                    },
                    Containers = new[]
                    {
                        new ContainerMetrics
                        {
                            Name = "container1",
                            Usage = new ResourceUsage
                            {
                                Cpu = "100m",
                                Memory = "128Mi"
                            }
                        },
                        new ContainerMetrics
                        {
                            Name = "container2",
                            Usage = new ResourceUsage
                            {
                                Cpu = "200m",
                                Memory = "256Mi"
                            }
                        }
                    }
                }
            }
        };
    }

    [Fact]
    public async Task CollectAsync_WithDiskIOMetrics_ExtractsAndSendsDiskIO()
    {
        // Arrange
        var namespaces = new[] { "default" };
        var podList = CreatePodList("default", "test-pod");
        podList.Items[0].Spec.NodeName = "node-1";
        var podMetrics = CreatePodMetricsList("default", "test-pod", "test-container", "100m", "128Mi");
        var statsSummaryJson = @"{
            ""pods"": [
                {
                    ""podRef"": {
                        ""name"": ""test-pod"",
                        ""namespace"": ""default""
                    },
                    ""containers"": [
                        {
                            ""name"": ""test-container"",
                            ""rootfs"": {
                                ""ioStats"": {
                                    ""readBytes"": 1073741824,
                                    ""writeBytes"": 536870912,
                                    ""readOps"": 100,
                                    ""writeOps"": 50
                                }
                            }
                        }
                    ]
                }
            ]
        }";

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1"))
            .ReturnsAsync(statsSummaryJson);

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Returns(Task.CompletedTask);

        // Act
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert - Verify disk I/O is extracted
        _mockApiClient.Verify(
            a => a.WritePodMetricAsync(
                "default",
                "test-pod",
                "test-container",
                It.IsAny<double>(),
                It.IsAny<double?>(),
                It.IsAny<double>(),
                1073741824.0, // Disk read bytes (1Gi)
                536870912.0, // Disk write bytes (512Mi)
                100.0, // Disk read ops
                50.0, // Disk write ops
                It.IsAny<double>(), // Network usage
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<long?>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNetworkMetrics_ExtractsAndSendsNetworkUsage()
    {
        // Arrange
        var namespaces = new[] { "default" };
        var podList = CreatePodList("default", "test-pod");
        var podMetrics = CreatePodMetricsListWithNetwork("default", "test-pod", "test-container", "100m", "128Mi", "500Mi");

        _mockKubernetesWrapper
            .Setup(c => c.ListNamespacedPodAsync("default"))
            .ReturnsAsync(podList);

        _mockKubernetesWrapper
            .Setup(c => c.ListClusterCustomObjectAsync("metrics.k8s.io", "v1beta1", "pods"))
            .ReturnsAsync(podMetrics);

        _mockKubernetesWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("{}");

        _mockApiClient
            .Setup(a => a.WritePodMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>()))
            .Returns(Task.CompletedTask);

        // Act
        await PodMetricsCollector.CollectAsync(_mockKubernetesWrapper.Object, namespaces, _mockApiClient.Object);

        // Assert - Verify network usage is extracted (500Mi = 524288000 bytes)
        _mockApiClient.Verify(
            a => a.WritePodMetricAsync(
                "default",
                "test-pod",
                "test-container",
                It.IsAny<double>(), // CPU usage
                It.IsAny<double?>(), // CPU limit
                It.IsAny<double>(), // Memory usage
                It.IsAny<double>(), // Disk read bytes
                It.IsAny<double>(), // Disk write bytes
                It.IsAny<double>(), // Disk read ops
                It.IsAny<double>(), // Disk write ops
                524288000.0, // Network usage (500Mi)
                It.IsAny<string>(), // Node name
                It.IsAny<string>(), // Pod state
                It.IsAny<int>(), // Restart count
                It.IsAny<long?>()), // Pod age
            Times.Once);
    }


    private object CreatePodMetricsListWithNetwork(string @namespace, string podName, string containerName, string cpuUsage, string memoryUsage, string networkUsage)
    {
        return new PodMetricsList
        {
            Items = new[]
            {
                new PodMetricsItem
                {
                    Metadata = new Metadata
                    {
                        Name = podName,
                        Namespace = @namespace
                    },
                    Containers = new[]
                    {
                        new ContainerMetrics
                        {
                            Name = containerName,
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
                }
            }
        };
    }

    private object CreatePodMetricsListWithBothStorageTypes(string @namespace, string podName, string containerName, string cpuUsage, string memoryUsage, string ephemeralStorage, string storage)
    {
        return new PodMetricsList
        {
            Items = new[]
            {
                new PodMetricsItem
                {
                    Metadata = new Metadata
                    {
                        Name = podName,
                        Namespace = @namespace
                    },
                    Containers = new[]
                    {
                        new ContainerMetrics
                        {
                            Name = containerName,
                            Usage = new ResourceUsage
                            {
                                Cpu = cpuUsage,
                                Memory = memoryUsage,
                                EphemeralStorage = ephemeralStorage,
                                Storage = storage,
                                Network = null
                            }
                        }
                    }
                }
            }
        };
    }
}

