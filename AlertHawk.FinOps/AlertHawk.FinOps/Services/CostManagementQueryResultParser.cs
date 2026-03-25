using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FinOpsToolSample.Models;

namespace FinOpsToolSample.Services;

/// <summary>
/// Parses Microsoft.CostManagement query <c>properties.rows</c> into aggregates (unit-tested).
/// </summary>
internal static class CostManagementQueryResultParser
{
    internal static (decimal TotalCost, Dictionary<string, decimal> ByResourceGroup, List<ServiceCostDetail> ByService)
        ParseCostRows(JsonElement rows)
    {
        var costsByResourceGroup = new Dictionary<string, decimal>();
        var costsByService = new List<ServiceCostDetail>();

        foreach (var row in rows.EnumerateArray())
        {
            var values = row.EnumerateArray().ToList();
            var cost = values[0].GetDecimal();
            var resourceGroup = values.Count > 2 ? values[2].GetString() ?? "Unknown" : "Unknown";
            var serviceName = values.Count > 3 ? values[3].GetString() ?? "Unknown" : "Unknown";

            if (!costsByResourceGroup.ContainsKey(resourceGroup))
            {
                costsByResourceGroup[resourceGroup] = 0;
            }

            costsByResourceGroup[resourceGroup] += cost;

            costsByService.Add(new ServiceCostDetail
            {
                ServiceName = serviceName,
                ResourceGroup = resourceGroup,
                Cost = cost
            });
        }

        var totalCost = costsByResourceGroup.Values.Sum();
        return (totalCost, costsByResourceGroup, costsByService);
    }
}
