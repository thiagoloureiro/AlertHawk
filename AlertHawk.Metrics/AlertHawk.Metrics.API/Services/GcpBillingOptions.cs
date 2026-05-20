namespace AlertHawk.Metrics.API.Services;

public class GcpBillingOptions
{
    public const string SectionName = "GcpBilling";

    public string? ApiKey { get; set; }

    /// <summary>
    /// Compute Engine service ID in the Cloud Billing Catalog API.
    /// </summary>
    public string ComputeEngineServiceId { get; set; } = "6F81-5844-456A";
}
