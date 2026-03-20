using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using FinOpsToolSample.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public interface ICostAnalysisService
    {
        Task<(decimal totalCost, Dictionary<string, decimal> byResourceGroup, List<ServiceCostDetail> byService)> AnalyzeAsync(SubscriptionResource subscription, ArmClient armClient);
    }
}
