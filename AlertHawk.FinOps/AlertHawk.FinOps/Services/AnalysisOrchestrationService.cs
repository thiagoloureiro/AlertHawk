using Azure.Identity;
using Azure.ResourceManager;
using FinOpsToolSample.Configuration;
using FinOpsToolSample.Data;
using FinOpsToolSample.Models;
using Microsoft.Extensions.Options;

namespace FinOpsToolSample.Services
{
    public class AnalysisOrchestrationService : IAnalysisOrchestrationService
    {
        private readonly DatabaseService _databaseService;
        private readonly AzureConfiguration _azureConfig;
        private readonly AIConfiguration _AIConfig;

        public AnalysisOrchestrationService(
            DatabaseService databaseService,
            IOptions<AzureConfiguration> azureConfig,
            IOptions<AIConfiguration> AIConfig)
        {
            _databaseService = databaseService;
            _azureConfig = azureConfig.Value;
            _AIConfig = AIConfig.Value;
        }

        /// <summary>
        /// Processes a single subscription - called by Hangfire background job
        /// </summary>
        public async Task<SubscriptionAnalysisResult> RunAnalysisForSingleSubscriptionAsync(string subscriptionId)
        {
            try
            {
                // Authentication
                var credential = new ClientSecretCredential(
                    _azureConfig.TenantId,
                    _azureConfig.ClientId,
                    _azureConfig.ClientSecret);

                var armClient = new ArmClient(credential);

                // Process the subscription
                return await RunAnalysisForSubscriptionAsync(armClient, credential, subscriptionId);
            }
            catch (Exception ex)
            {
                return new SubscriptionAnalysisResult
                {
                    Success = false,
                    SubscriptionId = subscriptionId,
                    SubscriptionName = "Unknown",
                    Message = $"Error: {ex.Message}",
                    ErrorDetails = ex.StackTrace
                };
            }
        }

        public async Task<AnalysisResult> RunAnalysisAsync()
        {
            var overallResult = new AnalysisResult
            {
                Success = true,
                Message = "Analysis started",
                SubscriptionResults = new List<SubscriptionAnalysisResult>()
            };

            try
            {
                // Ensure database is created
                await _databaseService.EnsureDatabaseCreatedAsync();

                // Authentication
                var credential = new ClientSecretCredential(
                    _azureConfig.TenantId,
                    _azureConfig.ClientId,
                    _azureConfig.ClientSecret);

                var armClient = new ArmClient(credential);

                // Get list of subscription IDs
                var subscriptionIds = _azureConfig.GetSubscriptionIdList();

                if (!subscriptionIds.Any())
                {
                    return new AnalysisResult
                    {
                        Success = false,
                        Message = "No subscription IDs configured"
                    };
                }

                // Process each subscription
                foreach (var subscriptionId in subscriptionIds)
                {
                    var subResult = await RunAnalysisForSubscriptionAsync(
                        armClient, 
                        credential, 
                        subscriptionId
                    );

                    overallResult.SubscriptionResults.Add(subResult);

                    if (!subResult.Success)
                    {
                        overallResult.Success = false;
                    }
                }

                // Calculate totals
                overallResult.TotalMonthlyCost = overallResult.SubscriptionResults.Sum(s => s.TotalMonthlyCost);
                overallResult.ResourcesAnalyzed = overallResult.SubscriptionResults.Sum(s => s.ResourcesAnalyzed);
                overallResult.Message = $"Analysis completed for {overallResult.SubscriptionResults.Count} subscription(s)";

                return overallResult;
            }
            catch (Exception ex)
            {
                overallResult.Success = false;
                overallResult.Message = $"Error: {ex.Message}";
                overallResult.ErrorDetails = ex.StackTrace;
                return overallResult;
            }
        }

        private async Task<SubscriptionAnalysisResult> RunAnalysisForSubscriptionAsync(
            ArmClient armClient, 
            ClientSecretCredential credential, 
            string subscriptionId)
        {
            try
            {
                // Get subscription
                var subscription = armClient.GetSubscriptionResource(
                    new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}")
                );

                // Initialize services
                var analysisServices = new List<IResourceAnalysisService>
                {
                    new AppServicePlanAnalysisService(),
                    new SqlDatabaseAnalysisService(credential),
                    new VirtualMachineAnalysisService(credential),
                    new StorageAccountAnalysisService(),
                    new AppServiceAnalysisService(credential),
                    new UnattachedDiskAnalysisService(),
                    new UnusedPublicIpAnalysisService(),
                    new KubernetesAnalysisService(credential),
                    new RedisAnalysisService(credential)
                };

                var costService = new CostManagementService(credential);
                var historicalCostService = new HistoricalCostService(credential);
                var dataCollector = new DataCollectionService();
                var AIService = new AIRecommendationService(_AIConfig.ApiKey, _AIConfig.ApiUrl, _AIConfig.ApiKeyHeaderName);

                // Get subscription info
                var subscriptionData = await subscription.GetAsync();
                dataCollector.SetSubscriptionInfo(
                    subscriptionData.Value.Data.DisplayName ?? "Unknown",
                    subscriptionData.Value.Data.SubscriptionId ?? "Unknown"
                );

                // Fetch Cost Data
                var (totalCost, costsByResourceGroup, costsByService) = 
                    await costService.AnalyzeAsync(subscription, armClient);

                // Set cost data in collector
                dataCollector.SetCostData(totalCost, costsByResourceGroup, costsByService);

                // Fetch Historical Costs (last 6 months)
                var historicalCosts = await historicalCostService.FetchHistoricalCostsAsync(subscription, months: 6);

                // Run all resource analysis services
                foreach (var service in analysisServices)
                {
                    await service.AnalyzeAsync(subscription);
                }

                // Collect data for AI analysis
                await dataCollector.CollectAppServicePlans(subscription, credential);
                await dataCollector.CollectSqlDatabases(subscription, credential);
                await dataCollector.CollectVirtualMachines(subscription, credential);
                await dataCollector.CollectStorageAccounts(subscription);
                await dataCollector.CollectUnattachedDisks(subscription);
                await dataCollector.CollectUnusedPublicIPs(subscription);
                await dataCollector.CollectKubernetesClusters(subscription);
                await dataCollector.CollectRedisCaches(subscription, credential);

                var collectedData = dataCollector.GetCollectedData();

                // Get AI recommendations from AI
                var (recommendations, AIResponse) = await AIService.GetRecommendationsAsync(collectedData);

                // Save to database
                string reportPath = string.Empty;
                if (AIResponse != null)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    reportPath = $"FinOps_Report_{timestamp}.md";
                }

                var analysisRunId = await _databaseService.SaveAnalysisRunAsync(collectedData, AIResponse, reportPath);

                // Save historical costs
                if (historicalCosts.Any())
                {
                    await _databaseService.SaveHistoricalCostsAsync(analysisRunId, historicalCosts);
                }

                return new SubscriptionAnalysisResult
                {
                    Success = true,
                    SubscriptionId = collectedData.SubscriptionId,
                    SubscriptionName = collectedData.SubscriptionName,
                    AnalysisRunId = analysisRunId,
                    TotalMonthlyCost = totalCost,
                    ResourcesAnalyzed = collectedData.Resources.Count,
                    Message = "Analysis completed successfully"
                };
            }
            catch (Exception ex)
            {
                return new SubscriptionAnalysisResult
                {
                    Success = false,
                    SubscriptionId = subscriptionId,
                    SubscriptionName = "Unknown",
                    Message = $"Error: {ex.Message}",
                    ErrorDetails = ex.StackTrace
                };
            }
        }
    }

    public class AnalysisResult
    {
        public bool Success { get; set; }
        public decimal TotalMonthlyCost { get; set; }
        public int ResourcesAnalyzed { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public List<SubscriptionAnalysisResult> SubscriptionResults { get; set; } = new();
    }

    public class SubscriptionAnalysisResult
    {
        public bool Success { get; set; }
        public string SubscriptionId { get; set; } = string.Empty;
        public string SubscriptionName { get; set; } = string.Empty;
        public int AnalysisRunId { get; set; }
        public decimal TotalMonthlyCost { get; set; }
        public int ResourcesAnalyzed { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
    }
}
