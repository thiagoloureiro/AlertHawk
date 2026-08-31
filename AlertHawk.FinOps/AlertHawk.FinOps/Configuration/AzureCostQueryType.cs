namespace FinOpsToolSample.Configuration
{
    /// <summary>
    /// Azure Cost Management Query API <c>type</c> values (ActualCost vs AmortizedCost).
    /// </summary>
    public static class AzureCostQueryType
    {
        public const string ActualCost = "ActualCost";
        public const string AmortizedCost = "AmortizedCost";

        public const string Default = ActualCost;

        /// <summary>
        /// Maps config/env input to a supported query type. Unknown values fall back to ActualCost.
        /// </summary>
        public static string Normalize(string? value)
        {
            if (string.Equals(value, AmortizedCost, StringComparison.OrdinalIgnoreCase))
            {
                return AmortizedCost;
            }

            return ActualCost;
        }

        public static string DisplayLabel(string normalizedType) =>
            normalizedType == AmortizedCost ? "Amortized" : "Actual";

        public static string Description(string normalizedType) =>
            normalizedType == AmortizedCost
                ? "Reservation and savings plan charges spread over the commitment term (FinOps / showback view)."
                : "Invoice-style costs as billed (cash / reconciliation view).";
    }
}
