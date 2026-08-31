using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using FinOpsToolSample.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class HistoricalCostService
    {
        private readonly ClientSecretCredential _credential;
        private readonly string _costQueryType;

        public HistoricalCostService(ClientSecretCredential credential, string costQueryType)
        {
            _credential = credential;
            _costQueryType = AzureCostQueryType.Normalize(costQueryType);
        }

        public async Task<List<HistoricalCostData>> FetchHistoricalCostsAsync(
            SubscriptionResource subscription, 
            int months = 6)
        {
            Console.WriteLine($"\n=== Fetching {months} Months Historical Cost Data ===");
            Console.WriteLine(
                $"Cost query type: {AzureCostQueryType.DisplayLabel(_costQueryType)} ({_costQueryType})");

            try
            {
                var subscriptionData = await AzureThrottledRequestRetry.ExecuteAsync(
                    () => subscription.GetAsync());
                var subscriptionId = subscriptionData.Value.Data.SubscriptionId;

                var tokenRequestContext = new Azure.Core.TokenRequestContext(
                    new[] { "https://management.azure.com/.default" }
                );
                var token = await AzureThrottledRequestRetry.ExecuteAsync(
                    async () => await _credential.GetTokenAsync(tokenRequestContext, default));

                // Calculate date range - start from 1st day of the month N months ago
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddMonths(-months);
                startDate = new DateTime(startDate.Year, startDate.Month, 1);

                Console.WriteLine($"📅 Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var queryPayload = CostManagementQueryBuilder.BuildCustomDailyQuery(
                    _costQueryType,
                    startDate,
                    endDate);

                var jsonPayload = JsonSerializer.Serialize(queryPayload);

                var httpClient = AzureManagementHttp.Shared;
                var url = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.CostManagement/query?api-version=2023-11-01";

                var historicalData = new List<HistoricalCostData>();
                string? skipToken = null;
                int pageCount = 0;

                do
                {
                    // Cost Management is strict: wait between pages (especially after a full 5k page).
                    await PaceBeforeCostQueryPageAsync(pageCount == 0, CancellationToken.None);
                    pageCount++;
                    var requestUrl = string.IsNullOrEmpty(skipToken)
                        ? url
                        : $"{url}&$skiptoken={Uri.EscapeDataString(skipToken)}";

                    using var response = await AzureThrottledRequestRetry.SendCostManagementPostWithRetryAsync(
                        httpClient,
                        requestUrl,
                        token.Token,
                        () => new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API Error: {response.StatusCode} - {error}");
                    }

                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(resultJson);

                    var properties = result.GetProperty("properties");
                    var rows = properties.GetProperty("rows");

                    var rowCount = rows.GetArrayLength();
                    Console.WriteLine($"📊 Processing page {pageCount}: {rowCount} rows of historical data...");

                    historicalData.AddRange(
                        HistoricalCostQueryResponseParser.ParseRows(rows, subscriptionId ?? ""));

                    skipToken = HistoricalCostQueryResponseParser.TryGetNextSkipToken(properties);

                    // Full pages almost always mean another request is coming — give ARM breathing room.
                    if (!string.IsNullOrEmpty(skipToken) && rowCount >= 5000)
                    {
                        var coolDown = TimeSpan.FromSeconds(20 + Random.Shared.Next(0, 10));
                        Console.WriteLine(
                            $"⏳ Full page received; cooling down {coolDown.TotalSeconds:0}s before next Cost Management page...");
                        await Task.Delay(coolDown).ConfigureAwait(false);
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
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"❌ Error fetching historical costs: {ex.Message}");
                return new List<HistoricalCostData>();
            }
        }

        private static Task PaceBeforeCostQueryPageAsync(bool isFirstPage, CancellationToken cancellationToken)
        {
            // Cost Management Query often allows only a few QPS; sub-second pacing is not enough.
            var ms = isFirstPage
                ? 2000 + Random.Shared.Next(0, 2000)
                : 8000 + Random.Shared.Next(0, 4000);
            Console.WriteLine(
                isFirstPage
                    ? $"⏳ Waiting {ms / 1000.0:0.#}s before first historical Cost Management query..."
                    : $"⏳ Waiting {ms / 1000.0:0.#}s before next historical Cost Management page...");
            return Task.Delay(TimeSpan.FromMilliseconds(ms), cancellationToken);
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
