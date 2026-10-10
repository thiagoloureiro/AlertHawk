namespace AlertHawk.Metrics.API.Authentication;

public static class MetricsPolicies
{
    /// <summary>
    /// JWT, Azure AD, or an API key. Used by the read endpoints the Rancher extension calls.
    /// </summary>
    public const string Read = "MetricsRead";
}
