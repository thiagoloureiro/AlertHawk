using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Resources;
using System;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class AppServicePlanAnalysisService : IResourceAnalysisService
    {
        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Finding Empty App Service Plans ===");

            try
            {
                var appServicePlans = subscription.GetAppServicePlansAsync();

                await foreach (var plan in appServicePlans)
                {
                    Console.WriteLine($"Checking plan: {plan.Data.Name}");

                    var apps = plan.GetWebAppsAsync();
                    var appCount = 0;

                    await foreach (var app in apps)
                    {
                        appCount++;
                    }

                    if (appCount == 0)
                    {
                        Console.WriteLine($"⚠️ UNUSED: App Service Plan '{plan.Data.Name}' " +
                                        $"has NO apps - SKU: {plan.Data.Sku.Name}, " +
                                        $"Location: {plan.Data.Location}");
                    }
                    else
                    {
                        Console.WriteLine($"✓ Plan '{plan.Data.Name}' has {appCount} app(s)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking App Service Plans: {ex.Message}");
            }
        }
    }
}
