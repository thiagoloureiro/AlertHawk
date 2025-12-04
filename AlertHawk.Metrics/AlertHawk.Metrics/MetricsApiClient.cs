using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using Polly;
using Polly.Retry;
using Serilog;

namespace AlertHawk.Metrics;

[ExcludeFromCodeCoverage]
public class MetricsApiClient : IMetricsApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _clusterName;
    private readonly string _clusterEnvironment;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public MetricsApiClient(string apiBaseUrl, string clusterName, string? clusterEnvironment = null)
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
        _clusterEnvironment = clusterEnvironment ?? "PROD";
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "br");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

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
                    Log.Warning("Retry {RetryCount}/3: API call failed ({StatusCode}). Retrying in {DelaySeconds:F1}s...", 
                        retryCount, statusCode, timespan.TotalSeconds);
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
        double memoryUsageBytes,
        string? nodeName = null)
    {
        var request = new
        {
            ClusterName = _clusterName,
            Namespace = @namespace,
            Pod = pod,
            Container = container,
            CpuUsageCores = cpuUsageCores,
            CpuLimitCores = cpuLimitCores,
            MemoryUsageBytes = memoryUsageBytes,
            NodeName = nodeName
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Log.Debug("Calling API: {ApiUrl}/api/metrics/pod with payload: {Payload}", _apiBaseUrl, json);
        
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/metrics/pod", content));

        response.EnsureSuccessStatusCode();
    }

    public async Task WriteNodeMetricAsync(
        string nodeName,
        double cpuUsageCores,
        double cpuCapacityCores,
        double memoryUsageBytes,
        double memoryCapacityBytes,
        string? kubernetesVersion = null,
        string? cloudProvider = null,
        bool? isReady = null,
        bool? hasMemoryPressure = null,
        bool? hasDiskPressure = null,
        bool? hasPidPressure = null,
        string? architecture = null,
        string? operatingSystem = null,
        string? region = null,
        string? instanceType = null)
    {
        var request = new
        {
            ClusterName = _clusterName,
            NodeName = nodeName,
            CpuUsageCores = cpuUsageCores,
            CpuCapacityCores = cpuCapacityCores,
            MemoryUsageBytes = memoryUsageBytes,
            MemoryCapacityBytes = memoryCapacityBytes,
            KubernetesVersion = kubernetesVersion,
            CloudProvider = cloudProvider,
            IsReady = isReady,
            HasMemoryPressure = hasMemoryPressure,
            HasDiskPressure = hasDiskPressure,
            HasPidPressure = hasPidPressure,
            Architecture = architecture,
            OperatingSystem = operatingSystem,
            Region = region,
            InstanceType = instanceType,
            ClusterEnvironment = _clusterEnvironment
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/metrics/node", content));

        response.EnsureSuccessStatusCode();
    }

    public async Task WritePodLogAsync(
        string @namespace,
        string pod,
        string container,
        string logContent)
    {
        var request = new
        {
            ClusterName = _clusterName,
            Namespace = @namespace,
            Pod = pod,
            Container = container,
            LogContent = logContent
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Log.Debug("Calling API: {ApiUrl}/api/metrics/pod/log with payload for {Namespace}/{Pod}/{Container}", 
            _apiBaseUrl, @namespace, pod, container);
        
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/metrics/pod/log", content));

        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

