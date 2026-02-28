using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Producers;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;
    private readonly NodeStatusTracker _nodeStatusTracker;
    private readonly INotificationProducer _notificationProducer;
    private readonly IAzurePricesService _azurePricesService;
    private readonly ILogger<MetricsController> _logger;
    
    public MetricsController(
        IClickHouseService clickHouseService,
        NodeStatusTracker nodeStatusTracker,
        INotificationProducer notificationProducer,
        IAzurePricesService azurePricesService,
        ILogger<MetricsController> logger)
    {
        _clickHouseService = clickHouseService;
        _nodeStatusTracker = nodeStatusTracker;
        _notificationProducer = notificationProducer;
        _azurePricesService = azurePricesService;
        _logger = logger;
    }

    /// <summary>
    /// Get metrics by namespace
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of pod/container metrics</returns>
    [HttpGet("namespace")]
    [Authorize]
    public async Task<ActionResult<List<PodMetricDto>>> GetMetricsByNamespace(
        [FromQuery] string? @namespace = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetMetricsByNamespaceAsync(@namespace, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get metrics for a specific namespace
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of pod/container metrics for the namespace</returns>
    [HttpGet("namespace/{namespace}")]
    [Authorize]
    public async Task<ActionResult<List<PodMetricDto>>> GetMetricsByNamespaceName(
        string @namespace,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetMetricsByNamespaceAsync(@namespace, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get node metrics
    /// </summary>
    /// <param name="nodeName">Optional node name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of node metrics</returns>
    [HttpGet("node")]
    [Authorize]
    public async Task<ActionResult<List<NodeMetricDto>>> GetNodeMetrics(
        [FromQuery] string? nodeName = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetNodeMetricsAsync(nodeName, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get metrics for a specific node
    /// </summary>
    /// <param name="nodeName">Node name</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of node metrics for the specified node</returns>
    [HttpGet("node/{nodeName}")]
    [Authorize]
    public async Task<ActionResult<List<NodeMetricDto>>> GetNodeMetricsByName(
        string nodeName,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetNodeMetricsAsync(nodeName, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write pod/container metrics
    /// </summary>
    /// <param name="request">Pod metric data</param>
    /// <returns>Success status</returns>
    [HttpPost("pod")]
    [AllowAnonymous]
    public async Task<ActionResult> WritePodMetric([FromBody] PodMetricRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WriteMetricsAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.CpuUsageCores,
                request.CpuLimitCores,
                request.MemoryUsageBytes,
                clusterName,
                request.NodeName,
                request.PodState,
                request.RestartCount,
                request.PodAge);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write node metrics
    /// </summary>
    /// <param name="request">Node metric data</param>
    /// <returns>Success status</returns>
    [HttpPost("node")]
    [AllowAnonymous]
    public async Task<ActionResult> WriteNodeMetric([FromBody] NodeMetricRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                clusterName,
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
                request.InstanceType);

            // Check for status changes and send notifications
            var nodeKey = _nodeStatusTracker.GetNodeKey(request.NodeName, clusterName);
            var hasStatusChanged = _nodeStatusTracker.HasStatusChanged(
                nodeKey,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                out var previousStatus);

            if (hasStatusChanged)
            {
                _logger.LogWarning(
                    $"Node status changed for {request.NodeName} in cluster {clusterName}. Previous status: " +
                    $"IsReady={previousStatus.IsReady}, HasMemoryPressure={previousStatus.HasMemoryPressure}, " +
                    $"HasDiskPressure={previousStatus.HasDiskPressure}, HasPidPressure={previousStatus.HasPidPressure}. " +
                    $"New status: IsReady={request.IsReady}, HasMemoryPressure={request.HasMemoryPressure}, " +
                    $"HasDiskPressure={request.HasDiskPressure}, HasPidPressure={request.HasPidPressure}.");
                
                // Determine if the node is healthy (all conditions are OK)
                var isHealthy = (request.IsReady == true || request.IsReady == null) &&
                               (request.HasMemoryPressure == false || request.HasMemoryPressure == null) &&
                               (request.HasDiskPressure == false || request.HasDiskPressure == null) &&
                               (request.HasPidPressure == false || request.HasPidPressure == null);

                // Send notification for both OK and not OK status changes
                await _notificationProducer.SendNodeStatusNotification(
                    request.NodeName,
                    clusterName,
                    request.ClusterEnvironment,
                    request.IsReady,
                    request.HasMemoryPressure,
                    request.HasDiskPressure,
                    request.HasPidPressure,
                    isHealthy);
            }

            // Fetch and store Azure prices if we have the necessary information
            _logger.LogDebug("Checking price fetch conditions for node {NodeName} in cluster {ClusterName}: CloudProvider={CloudProvider}, Region={Region}, InstanceType={InstanceType}",
                request.NodeName, clusterName, request.CloudProvider ?? "null", request.Region ?? "null", request.InstanceType ?? "null");

            var isAzure = !string.IsNullOrWhiteSpace(request.CloudProvider) && 
                          (request.CloudProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase) ||
                           request.CloudProvider.Equals("AKS", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(clusterName) && 
                isAzure &&
                !string.IsNullOrWhiteSpace(request.Region) &&
                !string.IsNullOrWhiteSpace(request.InstanceType))
            {
                _logger.LogInformation("Price fetch conditions met. Fetching Azure prices for node {NodeName} in cluster {ClusterName}, Region={Region}, InstanceType={InstanceType}, OS={OperatingSystem}",
                    request.NodeName, clusterName, request.Region, request.InstanceType, request.OperatingSystem ?? "null");
                
                try
                {
                    await FetchAndStoreClusterPriceAsync(
                        clusterName,
                        request.NodeName,
                        request.Region,
                        request.InstanceType,
                        request.OperatingSystem,
                        request.CloudProvider);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request if price fetching fails
                    _logger.LogWarning(ex, "Failed to fetch and store cluster price for node {NodeName} in cluster {ClusterName}", 
                        request.NodeName, clusterName);
                    SentrySdk.CaptureException(ex);
                }
            }
            else
            {
                var missingFields = new List<string>();
                if (string.IsNullOrWhiteSpace(clusterName)) missingFields.Add("ClusterName");
                if (string.IsNullOrWhiteSpace(request.CloudProvider)) missingFields.Add("CloudProvider");
                else if (!isAzure) missingFields.Add($"CloudProvider (not Azure/AKS, got: {request.CloudProvider})");
                if (string.IsNullOrWhiteSpace(request.Region)) missingFields.Add("Region");
                if (string.IsNullOrWhiteSpace(request.InstanceType)) missingFields.Add("InstanceType");
                
                _logger.LogDebug("Skipping price fetch for node {NodeName} in cluster {ClusterName}. Missing or invalid fields: {MissingFields}",
                    request.NodeName, clusterName, string.Join(", ", missingFields));
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write PVC/volume usage metrics
    /// </summary>
    /// <param name="request">PVC metric data</param>
    /// <returns>Success status</returns>
    [HttpPost("pvc")]
    [AllowAnonymous]
    public async Task<ActionResult> WritePvcMetric([FromBody] PvcMetricRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WritePvcMetricsAsync(
                request.Namespace,
                request.Pod,
                request.PvcNamespace,
                request.PvcName,
                request.VolumeName,
                request.UsedBytes,
                request.AvailableBytes,
                request.CapacityBytes,
                clusterName);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get PVC/volume usage metrics
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of PVC metrics</returns>
    [HttpGet("pvc")]
    [Authorize]
    public async Task<ActionResult<List<PvcMetricDto>>> GetPvcMetrics(
        [FromQuery] string? @namespace = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetPvcMetricsAsync(@namespace, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get PVC metrics for a specific namespace
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of PVC metrics for the namespace</returns>
    [HttpGet("pvc/namespace/{namespace}")]
    [Authorize]
    public async Task<ActionResult<List<PvcMetricDto>>> GetPvcMetricsByNamespace(
        string @namespace,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetPvcMetricsAsync(@namespace, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write VM/host metrics (CPU, RAM, disks). Used by the VM agent; hostname is stored with the metrics.
    /// </summary>
    /// <param name="request">Host metric data including hostname</param>
    /// <returns>Success status</returns>
    [HttpPost("host")]
    [AllowAnonymous]
    public async Task<ActionResult> WriteHostMetric([FromBody] HostMetricRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Hostname))
            {
                return BadRequest(new { error = "Hostname is required." });
            }

            var disks = request.Disks?
                .Select(d => (d.DriveName, d.TotalBytes, d.FreeBytes))
                .ToList();

            await _clickHouseService.WriteHostMetricsAsync(
                request.Hostname,
                request.CpuUsagePercent,
                request.MemoryTotalBytes,
                request.MemoryUsedBytes,
                disks);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get VM/host metrics (CPU, RAM) by hostname and time range.
    /// </summary>
    /// <param name="hostname">Optional hostname filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <returns>List of host metrics</returns>
    [HttpGet("host")]
    [Authorize]
    public async Task<ActionResult<List<HostMetricDto>>> GetHostMetrics(
        [FromQuery] string? hostname = null,
        [FromQuery] int? minutes = 1440)
    {
        try
        {
            var metrics = await _clickHouseService.GetHostMetricsAsync(hostname, minutes);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get VM/host disk metrics by hostname and time range.
    /// </summary>
    /// <param name="hostname">Optional hostname filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <returns>List of host disk metrics</returns>
    [HttpGet("host/disk")]
    [Authorize]
    public async Task<ActionResult<List<HostDiskMetricDto>>> GetHostDiskMetrics(
        [FromQuery] string? hostname = null,
        [FromQuery] int? minutes = 1440)
    {
        try
        {
            var metrics = await _clickHouseService.GetHostDiskMetricsAsync(hostname, minutes);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get unique cluster names from both node and namespace tables
    /// </summary>
    /// <returns>List of unique cluster names</returns>
    [HttpGet("clusters")]
    [Authorize]
    public async Task<ActionResult<List<string>>> GetUniqueClusterNames()
    {
        try
        {
            var clusterNames = await _clickHouseService.GetUniqueClusterNamesAsync();
           
            // Order alphabetically
            var orderedClusterNames = clusterNames.OrderBy(name => name).ToList();
            
            return Ok(orderedClusterNames);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get unique namespace names from the metrics table
    /// </summary>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of unique namespace names</returns>
    [HttpGet("namespaces")]
    [Authorize]
    public async Task<ActionResult<List<string>>> GetUniqueNamespaceNames([FromQuery] string? clusterName = null)
    {
        try
        {
            var namespaceNames = await _clickHouseService.GetUniqueNamespaceNamesAsync(clusterName);
          
            // Order alphabetically
            var orderedNamespaces = namespaceNames.OrderBy(name => name).ToList();
            
            return Ok(orderedNamespaces);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Clean up metrics tables
    /// </summary>
    /// <param name="days">Number of days of retention. If 0, truncates both tables.</param>
    /// <returns>Success status with information about the cleanup operation</returns>
    [HttpDelete("cleanup")]
    [Authorize]
    public async Task<ActionResult> CleanupMetrics([FromQuery] int days = 0)
    {
        try
        {
            await _clickHouseService.CleanupMetricsAsync(days);
            var message = days == 0
                ? "All seven tables (k8s_metrics, k8s_node_metrics, k8s_pod_logs, k8s_events, k8s_pvc_metrics, vm_metrics, vm_disk_metrics) have been truncated."
                : $"Records older than {days} days have been deleted from all seven tables (k8s_metrics, k8s_node_metrics, k8s_pod_logs, k8s_events, k8s_pvc_metrics, vm_metrics, vm_disk_metrics).";
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write pod logs
    /// </summary>
    /// <param name="request">Pod log data</param>
    /// <returns>Success status</returns>
    [HttpPost("pod/log")]
    [AllowAnonymous]
    public async Task<ActionResult> WritePodLog([FromBody] PodLogRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WritePodLogAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.LogContent,
                clusterName);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get pod logs
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="pod">Optional pod name filter</param>
    /// <param name="container">Optional container name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of pod logs</returns>
    [HttpGet("pod/log")]
    [Authorize]
    public async Task<ActionResult<List<PodLogDto>>> GetPodLogs(
        [FromQuery] string? @namespace = null,
        [FromQuery] string? pod = null,
        [FromQuery] string? container = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 100,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var logs = await _clickHouseService.GetPodLogsAsync(@namespace, pod, container, minutes, limit, clusterName);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get pod logs for a specific namespace
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="pod">Optional pod name filter</param>
    /// <param name="container">Optional container name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of pod logs for the namespace</returns>
    [HttpGet("pod/log/namespace/{namespace}")]
    [Authorize]
    public async Task<ActionResult<List<PodLogDto>>> GetPodLogsByNamespace(
        string @namespace,
        [FromQuery] string? pod = null,
        [FromQuery] string? container = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 100,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var logs = await _clickHouseService.GetPodLogsAsync(@namespace, pod, container, minutes, limit, clusterName);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get pod logs for a specific pod
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="pod">Pod name</param>
    /// <param name="container">Optional container name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of pod logs for the specified pod</returns>
    [HttpGet("pod/log/namespace/{namespace}/pod/{pod}")]
    [Authorize]
    public async Task<ActionResult<List<PodLogDto>>> GetPodLogsByPod(
        string @namespace,
        string pod,
        [FromQuery] string? container = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 100,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var logs = await _clickHouseService.GetPodLogsAsync(@namespace, pod, container, minutes, limit, clusterName);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task FetchAndStoreClusterPriceAsync(
        string clusterName,
        string nodeName,
        string region,
        string instanceType,
        string? operatingSystem,
        string? cloudProvider)
    {

        // capitalize first character of the OS
        if (!string.IsNullOrWhiteSpace(operatingSystem))
        {
            operatingSystem = char.ToUpper(operatingSystem[0]) + operatingSystem.Substring(1).ToLower();
        }

        // Build Azure price request
        var priceRequest = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines",
            ArmRegionName = region,
            OperatingSystem = operatingSystem ?? "Linux",
            Type = "Consumption"
        };

        // Try to match instance type - Azure uses formats like "Standard_D2s_v3"
        // The instance type from Kubernetes might be different, so we'll try to match it
        if (!string.IsNullOrWhiteSpace(instanceType))
        {
            // Try using the instance type as ArmSkuName first
            priceRequest.ArmSkuName = instanceType;
        }

        _logger.LogDebug("Fetching Azure prices with request: ServiceName={ServiceName}, ArmRegionName={ArmRegionName}, ArmSkuName={ArmSkuName}, OperatingSystem={OperatingSystem}, Type={Type}",
            priceRequest.ServiceName, priceRequest.ArmRegionName, priceRequest.ArmSkuName ?? "null", priceRequest.OperatingSystem, priceRequest.Type);

        try
        {
            var priceResponse = await _azurePricesService.GetPricesAsync(priceRequest);
            
            _logger.LogDebug("Azure prices API returned {Count} items for node {NodeName} in cluster {ClusterName}",
                priceResponse?.Items?.Count ?? 0, nodeName, clusterName);
            
            if (priceResponse?.Items != null && priceResponse.Items.Any())
            {
                // Store the first matching price (you might want to filter more specifically)
                var priceItem = priceResponse.Items.FirstOrDefault();
                
                if (priceItem != null)
                {
                    _logger.LogInformation("Storing cluster price for node {NodeName} in cluster {ClusterName}: UnitPrice={UnitPrice} {CurrencyCode}/hour, ProductName={ProductName}, SkuName={SkuName}",
                        nodeName, clusterName, priceItem.UnitPrice, priceItem.CurrencyCode, priceItem.ProductName, priceItem.SkuName);
                    
                    await _clickHouseService.WriteClusterPriceAsync(
                        clusterName,
                        nodeName,
                        region,
                        instanceType,
                        operatingSystem,
                        cloudProvider,
                        priceItem.CurrencyCode,
                        priceItem.UnitPrice,
                        priceItem.RetailPrice,
                        priceItem.MeterName,
                        priceItem.ProductName,
                        priceItem.SkuName,
                        priceItem.ServiceName,
                        priceItem.ArmRegionName,
                        priceItem.EffectiveStartDate);
                    
                    _logger.LogInformation("Successfully stored cluster price for node {NodeName} in cluster {ClusterName}: {UnitPrice} {CurrencyCode}/hour", 
                        nodeName, clusterName, priceItem.UnitPrice, priceItem.CurrencyCode);
                }
                else
                {
                    _logger.LogWarning("Price response has items but FirstOrDefault returned null for node {NodeName} in cluster {ClusterName}",
                        nodeName, clusterName);
                }
            }
            else
            {
                _logger.LogWarning("No price items found in Azure API response for node {NodeName} in cluster {ClusterName}, Region={Region}, InstanceType={InstanceType}",
                    nodeName, clusterName, region, instanceType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Azure prices for node {NodeName} in cluster {ClusterName}, Region={Region}, InstanceType={InstanceType}", 
                nodeName, clusterName, region, instanceType);
            throw;
        }
    }
}