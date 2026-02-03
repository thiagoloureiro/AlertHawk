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
        // A Sentry Data Source Name (DSN) is required.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
        options.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? "https://7539147312d4c51ccf970c6ddd0f15ca@o418696.ingest.us.sentry.io/4510386963283968";

        // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
        // This might be helpful, or might interfere with the normal operation of your application.
        // We enable it here for demonstration purposes when first trying Sentry.
        // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
        options.Debug = false;
        options.Environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";

        // This option is recommended. It enables Sentry's "Release Health" feature.
        options.AutoSessionTracking = true;
    });
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to initialize Sentry");
}

// Read configuration from environment variables
var collectionIntervalSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("METRICS_COLLECTION_INTERVAL_SECONDS"),
    out var interval) && interval > 0
    ? interval
    : 30;

var apiBaseUrl = Environment.GetEnvironmentVariable("METRICS_API_URL")
    ?? "http://localhost:5000";

var clusterName = Environment.GetEnvironmentVariable("CLUSTER_NAME");
if (string.IsNullOrWhiteSpace(clusterName))
{
    Log.Fatal("CLUSTER_NAME environment variable is required but not set!");
    Log.Fatal("Please set the CLUSTER_NAME environment variable before starting the application.");
    Environment.Exit(1);
}

var clusterEnvironment = Environment.GetEnvironmentVariable("CLUSTER_ENVIRONMENT") ?? "PROD";

var collectLogs = Environment.GetEnvironmentVariable("COLLECT_LOGS");
var isLogCollectionEnabled = !string.IsNullOrWhiteSpace(collectLogs) && 
                              collectLogs.Equals("true", StringComparison.OrdinalIgnoreCase);

Log.Information("Starting metrics collection service (interval: {Interval} seconds)", collectionIntervalSeconds);
Log.Information("Cluster name: {ClusterName}", clusterName);
Log.Information("Cluster environment: {ClusterEnvironment}", clusterEnvironment);
Log.Information("Metrics API URL: {ApiUrl}", apiBaseUrl);
Log.Information("Log collection: {Status}", isLogCollectionEnabled ? "Enabled" : "Disabled (set COLLECT_LOGS=true to enable)");
Log.Information("Press Ctrl+C to stop...");

// Initialize API client
using var apiClient = new MetricsApiClient(apiBaseUrl, clusterName, clusterEnvironment);

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);

// Fetch from Env Variables the namespaces to watch, or use defaults
var namespacesEnv = Environment.GetEnvironmentVariable("NAMESPACES_TO_WATCH") ?? "alerthawk,traefik,ilstudio,clickhouse,thiagoloureiro";
var namespacesToWatch = namespacesEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
Log.Information("Watching namespaces: {Namespaces}", string.Join(", ", namespacesToWatch));

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Log.Information("Shutting down gracefully...");
    cancellationTokenSource.Cancel();
};

// Start metrics collection loop
try
{
    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        await PodMetricsCollector.CollectAsync(client, namespacesToWatch, apiClient);
        await NodeMetricsCollector.CollectAsync(client, apiClient);
        await PvcUsageCollector.CollectAsync(client);
        await EventsCollector.CollectAsync(client, namespacesToWatch, apiClient);
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