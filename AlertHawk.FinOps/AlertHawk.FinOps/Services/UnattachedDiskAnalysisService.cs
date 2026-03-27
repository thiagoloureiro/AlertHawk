using Azure.ResourceManager.Resources;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class UnattachedDiskAnalysisService : IResourceAnalysisService
    {
        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Unattached Disks ===");

            try
            {
                var resourceGroups = subscription.GetResourceGroups();
                var unattachedCount = 0;

                await foreach (var rg in resourceGroups)
                {
                    var disks = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Compute/disks'"
                    );

                    await foreach (var disk in disks)
                    {
                        if (disk.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(disk.Data.Properties.ToString());

                            if (props.TryGetProperty("diskState", out var diskState))
                            {
                                var state = diskState.GetString();
                                if (state == "Unattached")
                                {
                                    unattachedCount++;
                                    Console.WriteLine($"\n💿 Unattached Disk: {disk.Data.Name}");
                                    Console.WriteLine($"  Resource Group: {disk.Data.Id.ResourceGroupName}");
                                    Console.WriteLine($"  Location: {disk.Data.Location}");

                                    if (disk.Data.Sku != null)
                                    {
                                        Console.WriteLine($"  SKU: {disk.Data.Sku.Name}");
                                    }

                                    if (props.TryGetProperty("diskSizeGB", out var diskSize))
                                    {
                                        Console.WriteLine($"  Size: {diskSize.GetInt32()} GB");
                                    }

                                    if (props.TryGetProperty("timeCreated", out var timeCreated))
                                    {
                                        Console.WriteLine($"  Created: {timeCreated.GetDateTime():yyyy-MM-dd}");
                                    }
                                }
                            }
                        }
                    }
                }

                if (unattachedCount == 0)
                {
                    Console.WriteLine("\n✓ No unattached disks found");
                }
                else
                {
                    Console.WriteLine($"\n⚠️  Found {unattachedCount} unattached disk(s)");
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error checking unattached disks: {ex.Message}");
            }
        }
    }
}
