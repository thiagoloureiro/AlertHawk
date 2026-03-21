using Azure.ResourceManager.Resources;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class UnusedPublicIpAnalysisService : IResourceAnalysisService
    {
        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Unused Public IPs ===");

            try
            {
                var resourceGroups = subscription.GetResourceGroups();
                var unusedCount = 0;

                await foreach (var rg in resourceGroups)
                {
                    var publicIPs = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Network/publicIPAddresses'"
                    );

                    await foreach (var ip in publicIPs)
                    {
                        if (ip.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(ip.Data.Properties.ToString());

                            bool isAttached = props.TryGetProperty("ipConfiguration", out _);

                            if (!isAttached)
                            {
                                unusedCount++;
                                Console.WriteLine($"\n🌍 Unused Public IP: {ip.Data.Name}");
                                Console.WriteLine($"  Resource Group: {ip.Data.Id.ResourceGroupName}");
                                Console.WriteLine($"  Location: {ip.Data.Location}");

                                if (ip.Data.Sku != null)
                                {
                                    Console.WriteLine($"  SKU: {ip.Data.Sku.Name}");
                                }

                                if (props.TryGetProperty("ipAddress", out var ipAddress))
                                {
                                    Console.WriteLine($"  IP Address: {ipAddress.GetString()}");
                                }

                                if (props.TryGetProperty("publicIPAllocationMethod", out var allocMethod))
                                {
                                    Console.WriteLine($"  Allocation: {allocMethod.GetString()}");
                                }
                            }
                        }
                    }
                }

                if (unusedCount == 0)
                {
                    Console.WriteLine("\n✓ No unused public IPs found");
                }
                else
                {
                    Console.WriteLine($"\n⚠️  Found {unusedCount} unused public IP(s)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking unused public IPs: {ex.Message}");
            }
        }
    }
}
