using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FinOpsToolSample.Services;

/// <summary>
/// Parses Cost Management historical query pages (unit-tested).
/// </summary>
internal static class HistoricalCostQueryResponseParser
{
    internal static List<HistoricalCostData> ParseRows(JsonElement rows, string subscriptionId)
    {
        var list = new List<HistoricalCostData>();

        foreach (var row in rows.EnumerateArray())
        {
            var values = row.EnumerateArray().ToList();

            var data = new HistoricalCostData
            {
                SubscriptionId = subscriptionId ?? "",
                Cost = values[0].GetDecimal()
            };

            if (values.Count > 1 && values[1].ValueKind == JsonValueKind.Number)
            {
                var dateInt = values[1].GetInt64();
                data.Date = DateTime.ParseExact(dateInt.ToString(), "yyyyMMdd", null);
            }
            else if (values.Count > 1 && values[1].ValueKind == JsonValueKind.String)
            {
                var dateStr = values[1].GetString();
                if (DateTime.TryParse(dateStr, out var parsedDate))
                {
                    data.Date = parsedDate;
                }
            }

            if (values.Count > 2)
            {
                data.ResourceGroup = values[2].GetString() ?? "Unknown";
            }

            if (values.Count > 3)
            {
                data.ServiceName = values[3].GetString() ?? "Unknown";
            }

            list.Add(data);
        }

        return list;
    }

    internal static string? TryExtractSkipTokenFromNextLink(string? nextLink)
    {
        if (string.IsNullOrEmpty(nextLink) || !nextLink.Contains("$skiptoken=", StringComparison.Ordinal))
        {
            return null;
        }

        var tokenStart = nextLink.IndexOf("$skiptoken=", StringComparison.Ordinal) + "$skiptoken=".Length;
        return nextLink[tokenStart..];
    }

    internal static string? TryGetNextSkipToken(JsonElement properties)
    {
        if (!properties.TryGetProperty("nextLink", out var nextLinkElement))
        {
            return null;
        }

        return TryExtractSkipTokenFromNextLink(nextLinkElement.GetString());
    }
}
