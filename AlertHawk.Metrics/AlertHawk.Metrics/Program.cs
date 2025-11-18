using AlertHawk.Metrics;
using AlertHawk.Metrics.Collectors;
using k8s;

SentrySdk.Init(options =>
{
    // A Sentry Data Source Name (DSN) is required.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
    options.Dsn = "https://7539147312d4c51ccf970c6ddd0f15ca@o418696.ingest.us.sentry.io/4510386963283968";

    // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
    // This might be helpful, or might interfere with the normal operation of your application.
    // We enable it here for demonstration purposes when first trying Sentry.
    // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
    options.Debug = false;

    // This option is recommended. It enables Sentry's "Release Health" feature.
    options.AutoSessionTracking = true;
});

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
    Console.Error.WriteLine("ERROR: CLUSTER_NAME environment variable is required but not set!");
    Console.Error.WriteLine("Please set the CLUSTER_NAME environment variable before starting the application.");
    Environment.Exit(1);
}

Console.WriteLine($"Starting metrics collection service (interval: {collectionIntervalSeconds} seconds)");
Console.WriteLine($"Cluster name: {clusterName}");
Console.WriteLine($"Metrics API URL: {apiBaseUrl}");
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

// Initialize API client
using var apiClient = new MetricsApiClient(apiBaseUrl, clusterName);

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);
var namespacesToWatch = new[] { "alerthawk", "traefik", "ilstudio" };

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down gracefully...");
    cancellationTokenSource.Cancel();
};

// Start metrics collection loop
try
{
    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        await PodMetricsCollector.CollectAsync(client, namespacesToWatch, apiClient);
        await NodeMetricsCollector.CollectAsync(client, apiClient);
        await Task.Delay(TimeSpan.FromSeconds(collectionIntervalSeconds), cancellationTokenSource.Token);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Metrics collection stopped.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    cancellationTokenSource.Cancel();
}