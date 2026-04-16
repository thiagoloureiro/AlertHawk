using Azure.ResourceManager.Resources;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    /// <summary>
    /// Synapse workspace–integrated logical SQL servers use Kind = analytics.
    /// Their databases should use Synapse resource metrics (sqlPools), not Microsoft.Sql database metrics.
    /// </summary>
    internal static class SynapseLinkedSqlServerDiscovery
    {
        public static async Task<HashSet<string>> GetSynapseLinkedSqlServerResourceIdsAsync(
            SubscriptionResource subscription)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var rg in subscription.GetResourceGroups())
            {
                await foreach (var server in rg.GetGenericResourcesAsync(
                                   filter: "resourceType eq 'Microsoft.Sql/servers'"))
                {
                    if (string.Equals(server.Data.Kind, "analytics", StringComparison.OrdinalIgnoreCase))
                    {
                        ids.Add(server.Id.ToString());
                    }
                }
            }

            return ids;
        }
    }
}
