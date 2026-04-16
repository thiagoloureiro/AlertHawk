using Azure.ResourceManager.Resources;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    /// <summary>
    /// Synapse workspace–integrated logical SQL servers are usually Kind = analytics, but Kind is not always populated.
    /// The workspace name also matches the linked logical server name; we use both signals to skip
    /// <c>Microsoft.Sql/servers/databases</c> that belong in Synapse pool/workspace collection instead.
    /// </summary>
    internal sealed class SynapseSqlExclusions
    {
        private SynapseSqlExclusions(
            HashSet<string> analyticsLinkedSqlServerResourceIds,
            HashSet<string> synapseWorkspaceNames)
        {
            AnalyticsLinkedSqlServerResourceIds = analyticsLinkedSqlServerResourceIds;
            SynapseWorkspaceNames = synapseWorkspaceNames;
        }

        public HashSet<string> AnalyticsLinkedSqlServerResourceIds { get; }

        public HashSet<string> SynapseWorkspaceNames { get; }

        public static async Task<SynapseSqlExclusions> DiscoverAsync(SubscriptionResource subscription)
        {
            var analyticsServerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var workspaceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var rg in subscription.GetResourceGroups())
            {
                await foreach (var server in rg.GetGenericResourcesAsync(
                                   filter: "resourceType eq 'Microsoft.Sql/servers'"))
                {
                    if (string.Equals(server.Data.Kind, "analytics", StringComparison.OrdinalIgnoreCase))
                    {
                        analyticsServerIds.Add(server.Id.ToString());
                    }
                }

                await foreach (var ws in rg.GetGenericResourcesAsync(
                                   filter: "resourceType eq 'Microsoft.Synapse/workspaces'"))
                {
                    workspaceNames.Add(ws.Data.Name);
                }
            }

            return new SynapseSqlExclusions(analyticsServerIds, workspaceNames);
        }

        /// <summary>
        /// True when this database should not use <see cref="SqlDatabaseAnalysisService"/> / SQL DB metrics;
        /// use Synapse workspace / sqlPools collection instead.
        /// </summary>
        public bool IsSynapseWorkspaceSqlDatabase(GenericResource db)
        {
            if (!TryGetSqlServerResourceIdAndName(db.Id.ToString(), out var serverResourceId, out var serverName))
            {
                return false;
            }

            if (AnalyticsLinkedSqlServerResourceIds.Contains(serverResourceId))
            {
                return true;
            }

            return SynapseWorkspaceNames.Contains(serverName);
        }

        private static bool TryGetSqlServerResourceIdAndName(
            string databaseResourceId,
            out string serverResourceId,
            out string serverName)
        {
            serverResourceId = string.Empty;
            serverName = string.Empty;

            const string marker = "/providers/Microsoft.Sql/servers/";
            var idx = databaseResourceId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return false;
            }

            var afterMarker = databaseResourceId.Substring(idx + marker.Length);
            var slash = afterMarker.IndexOf('/');
            if (slash <= 0)
            {
                return false;
            }

            serverName = afterMarker.Substring(0, slash);
            serverResourceId = databaseResourceId.Substring(0, idx + marker.Length + serverName.Length);
            return true;
        }
    }
}
