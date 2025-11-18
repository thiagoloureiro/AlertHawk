using System.Net;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.Retry;

namespace AlertHawk.Metrics;

public class MetricsApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _clusterName;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public MetricsApiClient(string apiBaseUrl, string clusterName)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentException("API base URL is required.", nameof(apiBaseUrl));
        }
        if (string.IsNullOrWhiteSpace(clusterName))
        {
            throw new ArgumentException("Cluster name is required.", nameof(clusterName));
        }

        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _clusterName = clusterName;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && IsTransientError(r.StatusCode))
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "Exception";
                    Console.WriteLine($"Retry {retryCount}/3: API call failed ({statusCode}). Retrying in {timespan.TotalSeconds:F1}s...");
                });
    }

    private static bool IsTransientError(HttpStatusCode statusCode)
    {
        // Retry on server errors (5xx) and specific client errors that might be transient
        return statusCode >= HttpStatusCode.InternalServerError ||
               statusCode == HttpStatusCode.RequestTimeout ||
               statusCode == HttpStatusCode.BadGateway ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.GatewayTimeout;
    }

    public async Task WritePodMetricAsync(
        string @namespace,
        string pod,
        string container,
        double cpuUsageCores,
        double? cpuLimitCores,
        double memoryUsageBytes)
    {
        var request = new
        {
            ClusterName = _clusterName,
            Namespace = @namespace,
            Pod = pod,
            Container = container,
            CpuUsageCores = cpuUsageCores,
            CpuLimitCores = cpuLimitCores,
            MemoryUsageBytes = memoryUsageBytes
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/metrics/pod", content));

        response.EnsureSuccessStatusCode();
    }

    public async Task WriteNodeMetricAsync(
        string nodeName,
        double cpuUsageCores,
        double cpuCapacityCores,
        double memoryUsageBytes,
        double memoryCapacityBytes)
    {
        var request = new
        {
            ClusterName = _clusterName,
            NodeName = nodeName,
            CpuUsageCores = cpuUsageCores,
            CpuCapacityCores = cpuCapacityCores,
            MemoryUsageBytes = memoryUsageBytes,
            MemoryCapacityBytes = memoryCapacityBytes
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/metrics/node", content));

        response.EnsureSuccessStatusCode();
    }

    public async Task WritePvcMetricAsync(
        string @namespace,
        string pvcName,
        string storageClass,
        string status,
        double capacityBytes,
        double? usedBytes = null,
        string? volumeName = null)
    {
        var request = new
        {
            ClusterName = _clusterName,
            Namespace = @namespace,
            PvcName = pvcName,
            StorageClass = storageClass,
            Status = status,
            CapacityBytes = capacityBytes,
            UsedBytes = usedBytes,
            VolumeName = volumeName
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/metrics/pvc", content));

        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

