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
                        var skuName = plan.Data.Sku?.Name ?? "Unknown";
                        Console.WriteLine(AppServicePlanAnalysisMessages.FormatUnusedPlanWarning(
                            plan.Data.Name,
                            skuName,
                            plan.Data.Location.ToString()));
                    }
                    else
                    {
                        Console.WriteLine(AppServicePlanAnalysisMessages.FormatPlanWithApps(plan.Data.Name, appCount));
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error checking App Service Plans: {ex.Message}");
            }
        }
    }
}
