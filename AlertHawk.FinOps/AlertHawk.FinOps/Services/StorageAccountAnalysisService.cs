using Azure.ResourceManager.Resources;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class StorageAccountAnalysisService : IResourceAnalysisService
    {
        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Storage Accounts ===");

            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var storageAccounts = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Storage/storageAccounts'"
                    );

                    await foreach (var storage in storageAccounts)
                    {
                        Console.WriteLine($"\n💾 Storage Account: {storage.Data.Name}");
                        Console.WriteLine($"  Resource Group: {storage.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {storage.Data.Location}");

                        if (storage.Data.Sku != null)
                        {
                            Console.WriteLine($"  SKU: {storage.Data.Sku.Name}");
                            Console.WriteLine($"  Tier: {storage.Data.Sku.Tier}");
                        }

                        if (storage.Data.Kind != null)
                        {
                            Console.WriteLine($"  Kind: {storage.Data.Kind}");
                        }

                        if (storage.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(storage.Data.Properties.ToString());

                            if (props.TryGetProperty("primaryEndpoints", out var endpoints))
                            {
                                Console.WriteLine($"  Endpoints:");
                                if (endpoints.TryGetProperty("blob", out var blob))
                                    Console.WriteLine($"    - Blob: {blob.GetString()}");
                                if (endpoints.TryGetProperty("file", out var file))
                                    Console.WriteLine($"    - File: {file.GetString()}");
                                if (endpoints.TryGetProperty("queue", out var queue))
                                    Console.WriteLine($"    - Queue: {queue.GetString()}");
                                if (endpoints.TryGetProperty("table", out var table))
                                    Console.WriteLine($"    - Table: {table.GetString()}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error checking storage accounts: {ex.Message}");
            }
        }
    }
}
