using AlertHawk.Metrics;
using AlertHawk.Metrics.Collectors;
using k8s.Models;
using Moq;
using Xunit;

namespace AlertHawk.Metrics.Tests.Collectors;

public class PvcUsageCollectorTests
{
    private readonly Mock<IKubernetesClientWrapper> _mockWrapper;
    private readonly Mock<IMetricsApiClient> _mockApiClient;

    public PvcUsageCollectorTests()
    {
        _mockWrapper = new Mock<IKubernetesClientWrapper>();
        _mockApiClient = new Mock<IMetricsApiClient>();
    }

    [Fact]
    public async Task CollectAsync_WithValidStatsSummaryAndWatchedNamespace_CallsWritePvcMetric()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = CreateStatsSummaryJson(
            ("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "clickhouse-data", 1000UL, 2000UL, 3000UL));

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);
        _mockApiClient
            .Setup(a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()))
            .Returns(Task.CompletedTask);

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });

        // Assert
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync(
                "clickhouse",
                "clickhouse-0",
                "clickhouse",
                "clickhouse-data",
                "clickhouse-data",
                1000UL,
                2000UL,
                3000UL),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNamespacesToWatch_FiltersOutOtherNamespaces()
    {
        // Arrange: pod in "clickhouse" and pod in "other" namespace
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = CreateStatsSummaryJson(
            ("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL),
            ("other", "other-pod", "other", "other-pvc", "vol", 1UL, 2UL, 3UL));

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);
        _mockApiClient
            .Setup(a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()))
            .Returns(Task.CompletedTask);

        // Act - only watch "clickhouse"
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });

        // Assert - only clickhouse pod's PVC should be sent
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL),
            Times.Once);
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync("other", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_WithNullNamespacesToWatch_ProcessesAllNamespaces()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = CreateStatsSummaryJson(
            ("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL),
            ("other", "other-pod", "other", "other-pvc", "vol", 1UL, 2UL, 3UL));

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);
        _mockApiClient
            .Setup(a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()))
            .Returns(Task.CompletedTask);

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, namespacesToWatch: null);

        // Assert - both PVCs sent
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CollectAsync_WithEmptyNamespacesToWatch_ProcessesAllNamespaces()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = CreateStatsSummaryJson(
            ("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL));

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);
        _mockApiClient
            .Setup(a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()))
            .Returns(Task.CompletedTask);

        // Act - empty array = no filter (same as null)
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, Array.Empty<string>());

        // Assert
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNoPvcRefVolumes_DoesNotCallWritePvcMetric()
    {
        // Arrange - volume without pvcRef (e.g. emptyDir)
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = """
            {
                "pods": [
                    {
                        "podRef": { "name": "pod-1", "namespace": "clickhouse" },
                        "volume": [
                            { "name": "tmp", "usedBytes": 4096, "availableBytes": 0, "capacityBytes": 0 }
                        ]
                    }
                ]
            }
            """;

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });

        // Assert
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_WithApiClientNull_DoesNotCallWritePvcMetric()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = CreateStatsSummaryJson(
            ("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL));

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, apiClient: null, new[] { "clickhouse" });

        // Assert
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_WithNoNodes_ReturnsWithoutCallingGetNodeStatsSummary()
    {
        // Arrange
        var nodeList = new V1NodeList { Items = [] };
        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });

        // Assert
        _mockWrapper.Verify(c => c.GetNodeStatsSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_WithEmptyStatsSummary_DoesNotThrow()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1");
        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"pods":[]}""");

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });

        // Assert
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_WhenGetNodeStatsSummaryThrows_ContinuesToNextNode()
    {
        // Arrange - two nodes, first throws
        var nodeList = new V1NodeList
        {
            Items =
            [
                new V1Node { Metadata = new V1ObjectMeta { Name = "node-1" } },
                new V1Node { Metadata = new V1ObjectMeta { Name = "node-2" } }
            ]
        };
        var statsSummaryJson = CreateStatsSummaryJson(
            ("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL));

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("kubelet unreachable"));
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);
        _mockApiClient
            .Setup(a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()))
            .Returns(Task.CompletedTask);

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });

        // Assert - second node's PVC still sent
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithApiClientThrowing_DoesNotThrow()
    {
        // Arrange
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = CreateStatsSummaryJson(
            ("clickhouse", "clickhouse-0", "clickhouse", "clickhouse-data", "data", 100UL, 200UL, 300UL));

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);
        _mockApiClient
            .Setup(a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()))
            .ThrowsAsync(new Exception("API error"));

        // Act & Assert - should not throw
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });
    }

    [Fact]
    public async Task CollectAsync_WithCaseInsensitiveNamespaceMatch_CallsWritePvcMetric()
    {
        // Arrange - pod namespace is "ClickHouse", watch list has "clickhouse"
        var nodeList = CreateNodeList("node-1");
        var statsSummaryJson = """
            {
                "pods": [
                    {
                        "podRef": { "name": "clickhouse-0", "namespace": "ClickHouse" },
                        "volume": [
                            { "name": "data", "usedBytes": 100, "availableBytes": 200, "capacityBytes": 300, "pvcRef": { "name": "clickhouse-data", "namespace": "ClickHouse" } }
                        ]
                    }
                ]
            }
            """;

        _mockWrapper.Setup(c => c.ListNodeAsync()).ReturnsAsync(nodeList);
        _mockWrapper
            .Setup(c => c.GetNodeStatsSummaryAsync("node-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(statsSummaryJson);
        _mockApiClient
            .Setup(a => a.WritePvcMetricAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<ulong>()))
            .Returns(Task.CompletedTask);

        // Act
        await PvcUsageCollector.CollectAsync(_mockWrapper.Object, config: null, _mockApiClient.Object, new[] { "clickhouse" });

        // Assert - case-insensitive match
        _mockApiClient.Verify(
            a => a.WritePvcMetricAsync("ClickHouse", "clickhouse-0", "ClickHouse", "clickhouse-data", "data", 100UL, 200UL, 300UL),
            Times.Once);
    }

    private static V1NodeList CreateNodeList(string nodeName)
    {
        return new V1NodeList
        {
            Items =
            [
                new V1Node
                {
                    Metadata = new V1ObjectMeta { Name = nodeName }
                }
            ]
        };
    }

    /// <summary>
    /// Creates kubelet stats/summary JSON. Each tuple is (podNamespace, podName, pvcNamespace, pvcName, volumeName, usedBytes, availableBytes, capacityBytes).
    /// </summary>
    private static string CreateStatsSummaryJson(params (string PodNs, string PodName, string PvcNs, string PvcName, string VolName, ulong Used, ulong Avail, ulong Cap)[] volumes)
    {
        var podEntries = volumes.Select(v =>
            "{\"podRef\":{\"name\":\"" + v.PodName + "\",\"namespace\":\"" + v.PodNs + "\"}," +
            "\"volume\":[{\"name\":\"" + v.VolName + "\",\"usedBytes\":" + v.Used + ",\"availableBytes\":" + v.Avail + ",\"capacityBytes\":" + v.Cap + "," +
            "\"pvcRef\":{\"name\":\"" + v.PvcName + "\",\"namespace\":\"" + v.PvcNs + "\"}}]}" );
        var podsArray = string.Join(",", podEntries);
        return "{\"pods\":[" + podsArray + "]}";
    }
}
