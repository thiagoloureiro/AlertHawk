using System.Collections.Generic;
using FinOpsToolSample.Models;

namespace FinOpsToolSample.Services
{
    internal static class DataCollectionResourceTags
    {
        /// <summary>
        /// Copies resource group tags, then resource tags (resource wins on duplicate keys).
        /// Note: Azure does not apply RG tags to resources automatically; Cost Management uses tags on the resource record.
        /// Merging RG defaults here helps correlate analysis rows with RG-level GAR_ID / COST_CENTER until tag inheritance policies run.
        /// </summary>
        internal static void ApplyMergedFromArm(
            ResourceInfo resource,
            IEnumerable<KeyValuePair<string, string>>? resourceGroupTags,
            IEnumerable<KeyValuePair<string, string>>? resourceTags)
        {
            CopyInto(resource.Tags, resourceGroupTags);
            CopyInto(resource.Tags, resourceTags);
        }

        private static void CopyInto(Dictionary<string, string> target, IEnumerable<KeyValuePair<string, string>>? source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var kv in source)
            {
                target[kv.Key] = kv.Value;
            }
        }
    }
}
