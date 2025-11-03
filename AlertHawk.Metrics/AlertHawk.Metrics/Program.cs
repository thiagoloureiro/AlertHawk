using System.Text.Json;
using AlertHawk.Metrics;
using k8s;

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);

var namespacesToWatch = new[] { "alerthawk", "traefik", "ilstudio" };
foreach (var ns in namespacesToWatch)
{
    var pods = await client.CoreV1.ListNamespacedPodAsync(ns);

    foreach (var pod in pods.Items)
    {
        Console.WriteLine($"{pod.Metadata.NamespaceProperty}/{pod.Metadata.Name} - {pod.Status.Phase}");
    }

    var response = await client.CustomObjects.ListClusterCustomObjectAsync(
        group: "metrics.k8s.io",
        version: "v1beta1",
        plural: "pods");

    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    var jsonString = JsonSerializer.Serialize(response);
    var podMetricsList = JsonSerializer.Deserialize<PodMetricsList>(jsonString, jsonOptions);

    if (podMetricsList != null)
    {
        Console.WriteLine($"Found {podMetricsList.Items.Length} pod metrics");
        foreach (var item in podMetricsList.Items)
        {
            Console.WriteLine($"Pod: {item.Metadata.Namespace}/{item.Metadata.Name} - Timestamp: {item.Timestamp}");
            foreach (var container in item.Containers)
            {
                Console.WriteLine($"  Container: {container.Name} - CPU: {container.Usage.Cpu}, Memory: {container.Usage.Memory}");
            }
        }
    }
}