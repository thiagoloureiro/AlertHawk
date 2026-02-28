using System.Net;
using System.Text.Json;
using AlertHawk.Metrics;
using Xunit;

namespace AlertHawk.Metrics.Tests;

public class MetricsApiClientTests
{
    [Fact]
    public async Task WriteHostMetricAsync_SendsCorrectRequestToHostEndpoint()
    {
        // Arrange
        string? capturedContent = null;
        string? capturedUri = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedUri = request.RequestUri?.ToString();
            capturedContent = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };

        using var client = new MetricsApiClient("http://localhost:5000", "vm", httpClient, null);

        var disks = new List<(string DriveName, ulong TotalBytes, ulong FreeBytes)>
        {
            ("C:", 1000UL, 200UL)
        };

        // Act
        await client.WriteHostMetricAsync("server-01", 25.5, 1024UL, 512UL, disks);

        // Assert
        Assert.NotNull(capturedUri);
        Assert.Contains("/api/metrics/host", capturedUri);
        Assert.NotNull(capturedContent);

        var json = JsonDocument.Parse(capturedContent);
        var root = json.RootElement;
        Assert.Equal("server-01", root.GetProperty("Hostname").GetString());
        Assert.Equal(25.5, root.GetProperty("CpuUsagePercent").GetDouble());
        Assert.Equal(1024UL, root.GetProperty("MemoryTotalBytes").GetUInt64());
        Assert.Equal(512UL, root.GetProperty("MemoryUsedBytes").GetUInt64());
        var disksArray = root.GetProperty("Disks");
        Assert.Equal(1, disksArray.GetArrayLength());
        Assert.Equal("C:", disksArray[0].GetProperty("DriveName").GetString());
        Assert.Equal(1000UL, disksArray[0].GetProperty("TotalBytes").GetUInt64());
        Assert.Equal(200UL, disksArray[0].GetProperty("FreeBytes").GetUInt64());
    }

    [Fact]
    public async Task WriteHostMetricAsync_WithNullDisks_OmitsOrSendsEmptyDisks()
    {
        // Arrange
        string? capturedContent = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedContent = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var httpClient = new HttpClient(handler);

        using var client = new MetricsApiClient("http://localhost:5000", "vm", httpClient, null);

        // Act
        await client.WriteHostMetricAsync("server-02", 10.0, 2048UL, 1024UL, null);

        // Assert
        Assert.NotNull(capturedContent);
        var json = JsonDocument.Parse(capturedContent);
        var root = json.RootElement;
        Assert.Equal("server-02", root.GetProperty("Hostname").GetString());
        Assert.Equal(10.0, root.GetProperty("CpuUsagePercent").GetDouble());
        Assert.Equal(2048UL, root.GetProperty("MemoryTotalBytes").GetUInt64());
        Assert.Equal(1024UL, root.GetProperty("MemoryUsedBytes").GetUInt64());
        // Disks can be null or empty array in JSON
        if (root.TryGetProperty("Disks", out var disksProp))
        {
            Assert.True(disksProp.ValueKind == JsonValueKind.Null || disksProp.GetArrayLength() == 0);
        }
    }
}

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        _sendAsync = sendAsync;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _sendAsync(request, cancellationToken);
    }
}
