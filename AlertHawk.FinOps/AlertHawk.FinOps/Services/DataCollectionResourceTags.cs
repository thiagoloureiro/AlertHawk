using System;
using System.Collections.Generic;
using FinOpsToolSample.Models;

namespace FinOpsToolSample.Services
{
    internal static class DataCollectionResourceTags
    {
        internal const string GarIdTag = "GAR_ID";
        internal const string CostCenterTag = "COST_CENTER";
        internal const string ApplicationTag = "APPLICATION";

        private static readonly Dictionary<string, string> CanonicalKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            [GarIdTag] = GarIdTag,
            [CostCenterTag] = CostCenterTag,
            [ApplicationTag] = ApplicationTag
        };

        /// <summary>
        /// Copies resource group tags, then resource tags (resource wins on duplicate keys).
        /// Note: Azure does not apply RG tags to resources automatically; Cost Management uses tags on the resource record.
        /// Merging RG defaults here helps correlate analysis rows with RG-level GAR_ID / APPLICATION / COST_CENTER until tag inheritance policies run.
        /// Well-known tag names are stored with a stable casing so later lookups are case-insensitive.
        /// </summary>
        internal static void ApplyMergedFromArm(
            ResourceInfo resource,
            IEnumerable<KeyValuePair<string, string>>? resourceGroupTags,
            IEnumerable<KeyValuePair<string, string>>? resourceTags)
        {
            CopyInto(resource.Tags, resourceGroupTags);
            CopyInto(resource.Tags, resourceTags);
        }

        /// <summary>
        /// Maps well-known ARM tag names (GAR_ID, APPLICATION, COST_CENTER) to a stable key regardless of source casing.
        /// Other keys are returned unchanged.
        /// </summary>
        internal static string CanonicalKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            return CanonicalKeys.TryGetValue(key, out var canonical) ? canonical : key;
        }

        private static void CopyInto(Dictionary<string, string> target, IEnumerable<KeyValuePair<string, string>>? source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var kv in source)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    continue;
                }

                target[CanonicalKey(kv.Key)] = kv.Value;
            }
        }
    }
}
