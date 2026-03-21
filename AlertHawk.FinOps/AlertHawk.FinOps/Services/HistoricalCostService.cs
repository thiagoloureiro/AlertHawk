using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class HistoricalCostService
    {
        private readonly ClientSecretCredential _credential;

        public HistoricalCostService(ClientSecretCredential credential)
        {
            _credential = credential;
        }

        public async Task<List<HistoricalCostData>> FetchHistoricalCostsAsync(
            SubscriptionResource subscription, 
            int months = 6)
        {
            Console.WriteLine($"\n=== Fetching {months} Months Historical Cost Data ===");

            try
            {
                var subscriptionData = await subscription.GetAsync();
                var subscriptionId = subscriptionData.Value.Data.SubscriptionId;

                var tokenRequestContext = new Azure.Core.TokenRequestContext(
                    new[] { "https://management.azure.com/.default" }
                );
                var token = await _credential.GetTokenAsync(tokenRequestContext, default);

                // Calculate date range
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddMonths(-months);

                Console.WriteLine($"📅 Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var queryPayload = new
                {
                    type = "ActualCost",
                    timeframe = "Custom",
                    timePeriod = new
                    {
                        from = startDate.ToString("yyyy-MM-ddT00:00:00Z"),
                        to = endDate.ToString("yyyy-MM-ddT23:59:59Z")
                    },
                    dataset = new
                    {
                        granularity = "Daily",
                        aggregation = new Dictionary<string, object>
                        {
                            ["totalCost"] = new { name = "PreTaxCost", function = "Sum" }
                        },
                        grouping = new[]
                        {
                            new { type = "Dimension", name = "ResourceGroupName" },
                            new { type = "Dimension", name = "ServiceName" }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(queryPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                var url = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.CostManagement/query?api-version=2023-11-01";

                var historicalData = new List<HistoricalCostData>();
                string? skipToken = null;
                int pageCount = 0;

                do
                {
                    pageCount++;
                    var requestUrl = string.IsNullOrEmpty(skipToken) 
                        ? url 
                        : $"{url}&$skiptoken={Uri.EscapeDataString(skipToken)}";

                    var response = await httpClient.PostAsync(requestUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API Error: {response.StatusCode} - {error}");
                    }

                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(resultJson);

                    var properties = result.GetProperty("properties");
                    var rows = properties.GetProperty("rows");
                    var columns = properties.GetProperty("columns");

                    // Parse columns to understand data structure (only once)
                    if (pageCount == 1)
                    {
                        var columnNames = new List<string>();
                        foreach (var column in columns.EnumerateArray())
                        {
                            columnNames.Add(column.GetProperty("name").GetString() ?? "");
                        }
                    }

                    var rowCount = rows.GetArrayLength();
                    Console.WriteLine($"📊 Processing page {pageCount}: {rowCount} rows of historical data...");

                    foreach (var row in rows.EnumerateArray())
                    {
                        var values = row.EnumerateArray().ToList();

                        var data = new HistoricalCostData
                        {
                            SubscriptionId = subscriptionId ?? "",
                            Cost = values[0].GetDecimal()
                        };

                        // Parse date (usually in column index 1)
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

                        // Resource group (usually index 2)
                        if (values.Count > 2)
                        {
                            data.ResourceGroup = values[2].GetString() ?? "Unknown";
                        }

                        // Service name (usually index 3)
                        if (values.Count > 3)
                        {
                            data.ServiceName = values[3].GetString() ?? "Unknown";
                        }

                        historicalData.Add(data);
                    }

                    // Check for continuation token
                    skipToken = null;
                    if (properties.TryGetProperty("nextLink", out var nextLinkElement))
                    {
                        var nextLink = nextLinkElement.GetString();
                        if (!string.IsNullOrEmpty(nextLink) && nextLink.Contains("$skiptoken="))
                        {
                            var tokenStart = nextLink.IndexOf("$skiptoken=") + "$skiptoken=".Length;
                            skipToken = nextLink.Substring(tokenStart);
                        }
                    }

                } while (!string.IsNullOrEmpty(skipToken));

                Console.WriteLine($"✅ Fetched {historicalData.Count} historical cost records across {pageCount} page(s)");

                // Show summary
                var totalCost = historicalData.Sum(h => h.Cost);
                var dateRange = historicalData.GroupBy(h => h.Date.Date).Count();
                
                Console.WriteLine($"   Total Cost: ${totalCost:F2}");
                Console.WriteLine($"   Days Covered: {dateRange}");
                Console.WriteLine();

                return historicalData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching historical costs: {ex.Message}");
                return new List<HistoricalCostData>();
            }
        }
    }

    public class HistoricalCostData
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public string ResourceGroup { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
    }
}
