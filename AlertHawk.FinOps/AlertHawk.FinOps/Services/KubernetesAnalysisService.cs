using Azure.Identity;
using Azure.ResourceManager.Resources;
using k8s;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class KubernetesAnalysisService : IResourceAnalysisService
    {
        private readonly ClientSecretCredential _credential;

        public KubernetesAnalysisService(ClientSecretCredential credential)
        {
            _credential = credential;
        }

        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Kubernetes (AKS) Clusters ===");

            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    // Get AKS clusters
                    var aksClusters = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.ContainerService/managedClusters'"
                    );

                    await foreach (var cluster in aksClusters)
                    {
                        Console.WriteLine($"\n☸️  AKS Cluster: {cluster.Data.Name}");
                        Console.WriteLine($"  Resource Group: {cluster.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {cluster.Data.Location}");

                        if (cluster.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(cluster.Data.Properties.ToString());

                            // Display cluster info
                            if (props.TryGetProperty("kubernetesVersion", out var version))
                            {
                                Console.WriteLine($"  Kubernetes Version: {version.GetString()}");
                            }

                            if (props.TryGetProperty("agentPoolProfiles", out var agentPools))
                            {
                                Console.WriteLine($"  📊 Node Pools:");
                                
                                foreach (var pool in agentPools.EnumerateArray())
                                {
                                    var poolName = pool.TryGetProperty("name", out var pn) ? pn.GetString() : "Unknown";
                                    var count = pool.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
                                    var vmSize = pool.TryGetProperty("vmSize", out var vm) ? vm.GetString() : "Unknown";
                                    
                                    Console.WriteLine($"    - Pool: {poolName}");
                                    Console.WriteLine($"      Node Count: {count}");
                                    Console.WriteLine($"      VM Size: {vmSize}");

                                    if (pool.TryGetProperty("enableAutoScaling", out var autoScale))
                                    {
                                        var isAutoScaling = autoScale.GetBoolean();
                                        Console.WriteLine($"      Auto Scaling: {(isAutoScaling ? "Enabled" : "Disabled")}");
                                        
                                        if (isAutoScaling)
                                        {
                                            if (pool.TryGetProperty("minCount", out var min))
                                                Console.WriteLine($"      Min Nodes: {min.GetInt32()}");
                                            if (pool.TryGetProperty("maxCount", out var max))
                                                Console.WriteLine($"      Max Nodes: {max.GetInt32()}");
                                        }
                                    }

                                    if (pool.TryGetProperty("osDiskSizeGB", out var diskSize))
                                    {
                                        Console.WriteLine($"      OS Disk: {diskSize.GetInt32()} GB");
                                    }

                                    if (pool.TryGetProperty("nodeLabels", out var labels) && labels.ValueKind == JsonValueKind.Object)
                                    {
                                        var labelCount = 0;
                                        foreach (var label in labels.EnumerateObject())
                                        {
                                            labelCount++;
                                        }
                                        if (labelCount > 0)
                                            Console.WriteLine($"      Labels: {labelCount} label(s)");
                                    }
                                }
                            }

                            // Try to connect to cluster and get node metrics
                            try
                            {
                                await AnalyzeClusterNodes(cluster.Data.Name, cluster.Data.Id.ResourceGroupName ?? "", subscription);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  ⚠️  Could not connect to cluster for detailed analysis: {ex.Message}");
                                Console.WriteLine($"     (This requires cluster access credentials)");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error checking Kubernetes clusters: {ex.Message}");
            }
        }

        private async Task AnalyzeClusterNodes(string clusterName, string resourceGroup, SubscriptionResource subscription)
        {
            try
            {
                // Note: This requires proper RBAC permissions on the cluster
                // In production, you'd get the kubeconfig from Azure CLI or use managed identity
                Console.WriteLine($"\n  🔍 Attempting to analyze cluster nodes...");
                Console.WriteLine($"     Note: This requires cluster admin access");
                
                // For now, we'll show what data we would collect
                Console.WriteLine($"\n  📈 Node Metrics Analysis (Would Collect):");
                Console.WriteLine($"    - CPU utilization per node");
                Console.WriteLine($"    - Memory utilization per node");
                Console.WriteLine($"    - Pod count per node");
                Console.WriteLine($"    - Node capacity vs requests");
                Console.WriteLine($"    - Identify underutilized nodes");
                Console.WriteLine($"    - Spot instances opportunities");
                
                // Placeholder for actual implementation
                // You would use: var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                // var client = new Kubernetes(config);
                // var nodes = await client.CoreV1.ListNodeAsync();
                
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"  ⚠️  Could not analyze cluster nodes: {ex.Message}");
            }
        }
    }
}
