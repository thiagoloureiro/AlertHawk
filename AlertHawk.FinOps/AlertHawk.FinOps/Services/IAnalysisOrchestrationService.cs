namespace FinOpsToolSample.Services;

public interface IAnalysisOrchestrationService
{
    Task<SubscriptionAnalysisResult> RunAnalysisForSingleSubscriptionAsync(string subscriptionId);
}
