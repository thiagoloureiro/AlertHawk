using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FinOpsToolSample.Data;
using Microsoft.EntityFrameworkCore;

namespace FinOpsToolSample.Services
{
    /// <summary>
    /// Builds per–resource-group tag maps from <see cref="FinOpsToolSample.Data.Entities.ResourceAnalysis.TagsJson"/> for enriching cost rows.
    /// </summary>
    internal static class CostDetailResourceGroupTags
    {
        internal const string MultipleValuesSentinel = "<varies>";

        /// <summary>
        /// For each resource group, merges tag dictionaries from all resources in that group.
        /// When the same key appears with different values across resources, the merged value becomes <see cref="MultipleValuesSentinel"/>.
        /// </summary>
        internal static Dictionary<string, Dictionary<string, string>> MergeByResourceGroup(
            IEnumerable<(string ResourceGroup, string? TagsJson)> rows)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(System.StringComparer.Ordinal);
            foreach (var group in rows.GroupBy(r => r.ResourceGroup, System.StringComparer.Ordinal))
            {
                var dicts = new List<Dictionary<string, string>>();
                foreach (var row in group)
                {
                    if (string.IsNullOrWhiteSpace(row.TagsJson))
                    {
                        continue;
                    }

                    try
                    {
                        var d = JsonSerializer.Deserialize<Dictionary<string, string>>(row.TagsJson);
                        if (d is { Count: > 0 })
                        {
                            dicts.Add(d);
                        }
                    }
                    catch (JsonException)
                    {
                        // ignore malformed rows
                    }
                }

                var merged = MergeTagDictionaries(dicts);
                if (merged is { Count: > 0 })
                {
                    result[group.Key] = merged;
                }
            }

            return result;
        }

        /// <summary>
        /// Merges tags per analysis run and resource group (for historical rows and subscription-wide queries).
        /// </summary>
        internal static Dictionary<(int AnalysisRunId, string ResourceGroup), Dictionary<string, string>>
            MergeByAnalysisRunAndResourceGroup(
                IEnumerable<(int AnalysisRunId, string ResourceGroup, string? TagsJson)> rows)
        {
            var result = new Dictionary<(int, string), Dictionary<string, string>>();
            foreach (var group in rows.GroupBy(r => (r.AnalysisRunId, r.ResourceGroup)))
            {
                var dicts = new List<Dictionary<string, string>>();
                foreach (var row in group)
                {
                    if (string.IsNullOrWhiteSpace(row.TagsJson))
                    {
                        continue;
                    }

                    try
                    {
                        var d = JsonSerializer.Deserialize<Dictionary<string, string>>(row.TagsJson);
                        if (d is { Count: > 0 })
                        {
                            dicts.Add(d);
                        }
                    }
                    catch (JsonException)
                    {
                        // ignore malformed rows
                    }
                }

                var merged = MergeTagDictionaries(dicts);
                if (merged is { Count: > 0 })
                {
                    result[group.Key] = merged;
                }
            }

            return result;
        }

        internal static async Task<Dictionary<(int AnalysisRunId, string ResourceGroup), Dictionary<string, string>>>
            LoadMergedTagsByAnalysisRunsAsync(
                FinOpsDbContext context,
                IReadOnlyCollection<int> analysisRunIds,
                CancellationToken cancellationToken = default)
        {
            if (analysisRunIds.Count == 0)
            {
                return new Dictionary<(int, string), Dictionary<string, string>>();
            }

            var rows = await context.ResourceAnalysis
                .AsNoTracking()
                .Where(r => analysisRunIds.Contains(r.AnalysisRunId) && r.ResourceGroup != null && r.ResourceGroup != "")
                .Select(r => new { r.AnalysisRunId, r.ResourceGroup, r.TagsJson })
                .ToListAsync(cancellationToken);

            return MergeByAnalysisRunAndResourceGroup(
                rows.Select(r => (r.AnalysisRunId, r.ResourceGroup, r.TagsJson)));
        }

        internal static Dictionary<string, string>? MergeTagDictionaries(IReadOnlyList<Dictionary<string, string>> dicts)
        {
            if (dicts.Count == 0)
            {
                return null;
            }

            var merged = new Dictionary<string, string>();
            foreach (var dict in dicts)
            {
                foreach (var kv in dict)
                {
                    if (!merged.TryGetValue(kv.Key, out var existing))
                    {
                        merged[kv.Key] = kv.Value;
                    }
                    else if (existing == MultipleValuesSentinel)
                    {
                        // keep sentinel
                    }
                    else if (!string.Equals(existing, kv.Value, System.StringComparison.Ordinal))
                    {
                        merged[kv.Key] = MultipleValuesSentinel;
                    }
                }
            }

            return merged.Count > 0 ? merged : null;
        }
    }
}
