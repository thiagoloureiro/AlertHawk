using Azure.ResourceManager.Resources;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public interface IResourceAnalysisService
    {
        Task AnalyzeAsync(SubscriptionResource subscription);
    }
}
