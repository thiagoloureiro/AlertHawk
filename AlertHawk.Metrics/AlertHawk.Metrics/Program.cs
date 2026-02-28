using AlertHawk.Metrics;
using AlertHawk.Metrics.Collectors;
using k8s;
using Serilog;

// Configure Serilog
var logLevelEnv = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
var logLevel = Enum.TryParse<Serilog.Events.LogEventLevel>(logLevelEnv, ignoreCase: true, out var parsedLevel)
    ? parsedLevel
    : Serilog.Events.LogEventLevel.Information;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(logLevel)
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Initializing Sentry...");
    SentrySdk.Init(options =>
    {
        options.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? "https://7539147312d4c51ccf970c6ddd0f15ca@o418696.ingest.us.sentry.io/4510386963283968";
        options.Debug = false;
        options.Environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";
        options.AutoSessionTracking = true;
    });
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to initialize Sentry");
}

// Agent type: Kubernetes (default) or VM. VM collects CPU, RAM, disks on the host.
var agentTypeEnv = Environment.GetEnvironmentVariable("AGENT_TYPE") ?? "Kubernetes";
var isVmAgent = agentTypeEnv.Equals("vm", StringComparison.OrdinalIgnoreCase) ||
                agentTypeEnv.Equals("VM", StringComparison.OrdinalIgnoreCase);

var collectionIntervalSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("METRICS_COLLECTION_INTERVAL_SECONDS"),
    out var interval) && interval > 0
    ? interval
    : 30;

var apiBaseUrl = Environment.GetEnvironmentVariable("METRICS_API_URL")
    ?? "http://localhost:5000";

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Log.Information("Shutting down gracefully...");
    cancellationTokenSource.Cancel();
};

if (isVmAgent)
{
    // VM agent: collect CPU, RAM, disks and send to API with hostname
    var hostname = Environment.GetEnvironmentVariable("HOSTNAME")
        ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
        ?? Environment.MachineName;

    Log.Information("Starting VM metrics collection (interval: {Interval}s, hostname: {Hostname})", collectionIntervalSeconds, hostname);
    Log.Information("Metrics API URL: {ApiUrl}", apiBaseUrl);
    Log.Information("Press Ctrl+C to stop...");

    using var apiClient = new MetricsApiClient(apiBaseUrl, "vm", null);

    try
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var (cpuPercent, memoryTotal, memoryUsed, disks) = await HostMetricsCollector.CollectAsync();
                await apiClient.WriteHostMetricAsync(hostname, cpuPercent, memoryTotal, memoryUsed, disks);
                sw.Stop();
                Log.Information("VM metrics sent for {Hostname} (CPU: {Cpu:F1}%, Memory: {MemUsed}/{MemTotal} bytes, Disks: {DiskCount}). Cycle: {ElapsedSeconds}s",
                    hostname, cpuPercent, memoryUsed, memoryTotal, disks.Count, sw.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error collecting or sending VM metrics");
            }

            await Task.Delay(TimeSpan.FromSeconds(collectionIntervalSeconds), cancellationTokenSource.Token);
        }
    }
    catch (OperationCanceledException)
    {
        Log.Information("VM metrics collection stopped.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Fatal error in VM agent");
        Environment.Exit(1);
    }
    finally
    {
        cancellationTokenSource.Cancel();
        Log.CloseAndFlush();
    }
}
else
{
    // Kubernetes agent (default)
    var clusterName = Environment.GetEnvironmentVariable("CLUSTER_NAME");
    if (string.IsNullOrWhiteSpace(clusterName))
    {
        Log.Fatal("CLUSTER_NAME environment variable is required for Kubernetes agent. Set AGENT_TYPE=vm for host metrics, or set CLUSTER_NAME.");
        Environment.Exit(1);
    }

    var clusterEnvironment = Environment.GetEnvironmentVariable("CLUSTER_ENVIRONMENT") ?? "PROD";
    var collectLogs = Environment.GetEnvironmentVariable("COLLECT_LOGS");
    var isLogCollectionEnabled = !string.IsNullOrWhiteSpace(collectLogs) &&
                                  collectLogs.Equals("true", StringComparison.OrdinalIgnoreCase);

    Log.Information("Starting Kubernetes metrics collection (interval: {Interval} seconds)", collectionIntervalSeconds);
    Log.Information("Cluster name: {ClusterName}", clusterName);
    Log.Information("Cluster environment: {ClusterEnvironment}", clusterEnvironment);
    Log.Information("Metrics API URL: {ApiUrl}", apiBaseUrl);
    Log.Information("Log collection: {Status}", isLogCollectionEnabled ? "Enabled" : "Disabled (set COLLECT_LOGS=true to enable)");
    Log.Information("Press Ctrl+C to stop...");

    using var apiClient = new MetricsApiClient(apiBaseUrl, clusterName, clusterEnvironment);

    var config = KubernetesClientConfiguration.InClusterConfig();
    var client = new Kubernetes(config);

    var namespacesEnv = Environment.GetEnvironmentVariable("NAMESPACES_TO_WATCH") ?? "alerthawk,traefik,ilstudio,clickhouse,thiagoloureiro";
    var namespacesToWatch = namespacesEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    Log.Information("Watching namespaces: {Namespaces}", string.Join(", ", namespacesToWatch));

    try
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await Task.WhenAll(
                PodMetricsCollector.CollectAsync(client, namespacesToWatch, apiClient),
                NodeMetricsCollector.CollectAsync(client, apiClient),
                PvcUsageCollector.CollectAsync(client, config, apiClient, namespacesToWatch),
                EventsCollector.CollectAsync(client, namespacesToWatch, apiClient)
            );

            sw.Stop();
            Log.Information("Metrics collection cycle completed in {ElapsedSeconds} seconds", sw.Elapsed.TotalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(collectionIntervalSeconds), cancellationTokenSource.Token);
        }
    }
    catch (OperationCanceledException)
    {
        Log.Information("Metrics collection stopped.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Fatal error occurred");
        Environment.Exit(1);
    }
    finally
    {
        cancellationTokenSource.Cancel();
        Log.CloseAndFlush();
    }
}