using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using FinOpsToolSample.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class DatabaseService
    {
        private readonly FinOpsDbContext _context;

        public DatabaseService(FinOpsDbContext context)
        {
            _context = context;
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();
                Console.WriteLine("✅ Database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing database: {ex.Message}");
                throw;
            }
        }

        public async Task<int> SaveAnalysisRunAsync(
            AzureResourceData data,
            AIApiResponse? AIResponse,
            string reportFilePath)
        {
            try
            {
                Console.WriteLine("\n💾 Saving analysis results to database...");

                var analysisRun = new AnalysisRun
                {
                    SubscriptionId = data.SubscriptionId,
                    SubscriptionName = data.SubscriptionName,
                    RunDate = DateTime.UtcNow,
                    TotalMonthlyCost = data.TotalMonthlyCost,
                    TotalResourcesAnalyzed = data.Resources.Count,
                    AiModel = AIResponse?.model ?? "N/A",
                    ConversationId = AIResponse?.conversation_id ?? "N/A",
                    ReportFilePath = reportFilePath,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AnalysisRuns.Add(analysisRun);
                await _context.SaveChangesAsync();

                // Save cost details
                await SaveCostDetailsAsync(analysisRun.Id, data);

                // Save resources
                await SaveResourcesAsync(analysisRun.Id, data);

                // Save AI recommendations
                if (AIResponse?.output?.content != null)
                {
                    await SaveAiRecommendationAsync(analysisRun.Id, AIResponse);
                }

                Console.WriteLine($"✅ Analysis run saved with ID: {analysisRun.Id}");
                Console.WriteLine($"   - Cost details: {data.CostsByResourceGroup.Count + data.CostsByService.Count}");
                Console.WriteLine($"   - Resources: {data.Resources.Count}");
                Console.WriteLine($"   - AI recommendations: {(AIResponse != null ? "1" : "0")}");

                return analysisRun.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving to database: {ex.Message}");
                throw;
            }
        }

        public async Task SaveHistoricalCostsAsync(int analysisRunId, List<HistoricalCostData> historicalData)
        {
            try
            {
                Console.WriteLine($"\n💾 Saving {historicalData.Count} historical cost records...");

                var records = new List<HistoricalCost>();

                // Group by date and calculate daily totals
                var dailyTotals = historicalData
                    .GroupBy(h => h.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalCost = g.Sum(x => x.Cost),
                        SubscriptionId = g.First().SubscriptionId
                    })
                    .ToList();

                // Save daily totals (no resource group association)
                foreach (var daily in dailyTotals)
                {
                    records.Add(new HistoricalCost
                    {
                        AnalysisRunId = analysisRunId,
                        SubscriptionId = daily.SubscriptionId,
                        CostDate = daily.Date,
                        CostType = "Total",
                        ResourceGroup = null,
                        Name = null,
                        Cost = daily.TotalCost,
                        RecordedAt = DateTime.UtcNow
                    });
                }

                // Group by date and resource group
                var byResourceGroup = historicalData
                    .GroupBy(h => new { h.Date.Date, h.ResourceGroup })
                    .Select(g => new
                    {
                        g.Key.Date,
                        g.Key.ResourceGroup,
                        TotalCost = g.Sum(x => x.Cost),
                        SubscriptionId = g.First().SubscriptionId
                    })
                    .ToList();

                foreach (var rg in byResourceGroup)
                {
                    records.Add(new HistoricalCost
                    {
                        AnalysisRunId = analysisRunId,
                        SubscriptionId = rg.SubscriptionId,
                        CostDate = rg.Date,
                        CostType = "ResourceGroup",
                        ResourceGroup = rg.ResourceGroup,
                        Name = null, // Not needed for ResourceGroup type
                        Cost = rg.TotalCost,
                        RecordedAt = DateTime.UtcNow
                    });
                }

                // Group by date, resource group, and service
                var byService = historicalData
                    .GroupBy(h => new { h.Date.Date, h.ResourceGroup, h.ServiceName })
                    .Select(g => new
                    {
                        g.Key.Date,
                        g.Key.ResourceGroup,
                        g.Key.ServiceName,
                        TotalCost = g.Sum(x => x.Cost),
                        SubscriptionId = g.First().SubscriptionId
                    })
                    .ToList();

                foreach (var svc in byService)
                {
                    records.Add(new HistoricalCost
                    {
                        AnalysisRunId = analysisRunId,
                        SubscriptionId = svc.SubscriptionId,
                        CostDate = svc.Date,
                        CostType = "Service",
                        ResourceGroup = svc.ResourceGroup, // Now we know which RG this service belongs to!
                        Name = svc.ServiceName,
                        Cost = svc.TotalCost,
                        RecordedAt = DateTime.UtcNow
                    });
                }

                _context.HistoricalCosts.AddRange(records);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Saved {records.Count} historical cost records");
                Console.WriteLine($"   - Daily totals: {dailyTotals.Count}");
                Console.WriteLine($"   - By resource group: {byResourceGroup.Count}");
                Console.WriteLine($"   - By service (with RG): {byService.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving historical costs: {ex.Message}");
            }
        }

        private async Task SaveCostDetailsAsync(int analysisRunId, AzureResourceData data)
        {
            var costDetails = new List<CostDetail>();

            // Save costs by resource group
            foreach (var kvp in data.CostsByResourceGroup)
            {
                costDetails.Add(new CostDetail
                {
                    AnalysisRunId = analysisRunId,
                    CostType = "ResourceGroup",
                    Name = kvp.Key,
                    ResourceGroup = kvp.Key,
                    Cost = kvp.Value,
                    RecordedAt = DateTime.UtcNow
                });
            }

            // Aggregate and save costs by service (with resource group)
            var aggregatedServiceCosts = data.CostsByService
                .GroupBy(s => new { s.ServiceName, s.ResourceGroup })
                .Select(g => new
                {
                    ServiceName = g.Key.ServiceName,
                    ResourceGroup = g.Key.ResourceGroup,
                    TotalCost = g.Sum(x => x.Cost)
                });

            foreach (var serviceDetail in aggregatedServiceCosts)
            {
                costDetails.Add(new CostDetail
                {
                    AnalysisRunId = analysisRunId,
                    CostType = "Service",
                    Name = serviceDetail.ServiceName,
                    ResourceGroup = serviceDetail.ResourceGroup,
                    Cost = serviceDetail.TotalCost,
                    RecordedAt = DateTime.UtcNow
                });
            }

            _context.CostDetails.AddRange(costDetails);
            await _context.SaveChangesAsync();
        }

        private async Task SaveResourcesAsync(int analysisRunId, AzureResourceData data)
        {
            var resources = new List<ResourceAnalysis>();

            foreach (var resource in data.Resources)
            {
                resources.Add(new ResourceAnalysis
                {
                    AnalysisRunId = analysisRunId,
                    ResourceType = resource.Type,
                    ResourceName = resource.Name,
                    ResourceGroup = resource.ResourceGroup,
                    Location = resource.Location,
                    PropertiesJson = JsonSerializer.Serialize(resource.Properties),
                    MetricsJson = JsonSerializer.Serialize(resource.Metrics),
                    Flags = string.Join("; ", resource.Flags),
                    RecordedAt = DateTime.UtcNow
                });
            }

            _context.ResourceAnalysis.AddRange(resources);
            await _context.SaveChangesAsync();
        }

        private async Task SaveAiRecommendationAsync(int analysisRunId, AIApiResponse response)
        {
            // Extract summary (first 3 lines or 1000 chars)
            var fullText = response.output.content;
            var lines = fullText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var summary = string.Join(" ", lines.Take(3));
            if (summary.Length > 997)
            {
                summary = summary.Substring(0, 997) + "...";
            }

            var recommendation = new AiRecommendation
            {
                AnalysisRunId = analysisRunId,
                RecommendationText = fullText, // Store full markdown as-is
                Summary = summary,
                MessageId = response.message_id,
                ConversationId = response.conversation_id,
                Model = response.model,
                Timestamp = response.timestamp,
                RecordedAt = DateTime.UtcNow
            };

            _context.AiRecommendations.Add(recommendation);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AnalysisRun>> GetAnalysisHistoryAsync(string subscriptionId, int take = 10)
        {
            return await _context.AnalysisRuns
                .Where(a => a.SubscriptionId == subscriptionId)
                .OrderByDescending(a => a.RunDate)
                .Take(take)
                .Include(a => a.CostDetails)
                .Include(a => a.Resources)
                .Include(a => a.AiRecommendations)
                .ToListAsync();
        }

        public async Task<AnalysisRun?> GetLatestAnalysisAsync(string subscriptionId)
        {
            return await _context.AnalysisRuns
                .Where(a => a.SubscriptionId == subscriptionId)
                .OrderByDescending(a => a.RunDate)
                .Include(a => a.CostDetails)
                .Include(a => a.Resources)
                .Include(a => a.AiRecommendations)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CostDetail>> GetCostTrendAsync(string subscriptionId, string costType, int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            return await _context.CostDetails
                .Where(c => c.AnalysisRun.SubscriptionId == subscriptionId
                         && c.CostType == costType
                         && c.RecordedAt >= startDate)
                .OrderBy(c => c.RecordedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetResourceCountByTypeAsync(string subscriptionId)
        {
            var latestRun = await GetLatestAnalysisAsync(subscriptionId);
            if (latestRun == null) return new Dictionary<string, int>();

            return await _context.ResourceAnalysis
                .Where(r => r.AnalysisRunId == latestRun.Id)
                .GroupBy(r => r.ResourceType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }

        public async Task<List<ResourceAnalysis>> GetFlaggedResourcesAsync(string subscriptionId)
        {
            var latestRun = await GetLatestAnalysisAsync(subscriptionId);
            if (latestRun == null) return new List<ResourceAnalysis>();

            return await _context.ResourceAnalysis
                .Where(r => r.AnalysisRunId == latestRun.Id
                         && !string.IsNullOrEmpty(r.Flags))
                .ToListAsync();
        }

        public async Task<List<HistoricalCost>> GetDailyCostTrendAsync(string subscriptionId, int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;

            return await _context.HistoricalCosts
                .Where(h => h.SubscriptionId == subscriptionId
                         && h.CostType == "Total"
                         && h.CostDate >= startDate)
                .OrderBy(h => h.CostDate)
                .ToListAsync();
        }

        public async Task<List<HistoricalCost>> GetCostByResourceGroupTrendAsync(
            string subscriptionId, 
            string resourceGroup, 
            int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;

            return await _context.HistoricalCosts
                .Where(h => h.SubscriptionId == subscriptionId
                         && h.CostType == "ResourceGroup"
                         && h.Name == resourceGroup
                         && h.CostDate >= startDate)
                .OrderBy(h => h.CostDate)
                .ToListAsync();
        }

        public async Task<List<HistoricalCost>> GetCostByServiceTrendAsync(
            string subscriptionId, 
            string serviceName, 
            int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;

            return await _context.HistoricalCosts
                .Where(h => h.SubscriptionId == subscriptionId
                         && h.CostType == "Service"
                         && h.Name == serviceName
                         && h.CostDate >= startDate)
                .OrderBy(h => h.CostDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetMonthlySpendingAsync(string subscriptionId, int months = 6)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months).Date;

            var monthlyData = await _context.HistoricalCosts
                .Where(h => h.SubscriptionId == subscriptionId
                         && h.CostType == "Total"
                         && h.CostDate >= startDate)
                .GroupBy(h => new { h.CostDate.Year, h.CostDate.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalCost = g.Sum(x => x.Cost)
                })
                .ToListAsync();

            return monthlyData.ToDictionary(x => x.Month, x => x.TotalCost);
        }

        public async Task<Dictionary<string, decimal>> GetMonthlySpendingByResourceGroupAsync(
            string subscriptionId, 
            string resourceGroup, 
            int months = 6)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months).Date;

            var monthlyData = await _context.HistoricalCosts
                .Where(h => h.SubscriptionId == subscriptionId
                         && h.ResourceGroup == resourceGroup
                         && h.CostDate >= startDate)
                .GroupBy(h => new { h.CostDate.Year, h.CostDate.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalCost = g.Sum(x => x.Cost)
                })
                .ToListAsync();

            return monthlyData.ToDictionary(x => x.Month, x => x.TotalCost);
        }

        public async Task<List<HistoricalCost>> GetServiceCostsForResourceGroupAsync(
            string subscriptionId,
            string resourceGroup,
            int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;

            return await _context.HistoricalCosts
                .Where(h => h.SubscriptionId == subscriptionId
                         && h.ResourceGroup == resourceGroup
                         && h.CostType == "Service"
                         && h.CostDate >= startDate)
                .OrderBy(h => h.CostDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetTopServicesInResourceGroupAsync(
            string subscriptionId,
            string resourceGroup,
            int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days).Date;

            var services = await _context.HistoricalCosts
                .Where(h => h.SubscriptionId == subscriptionId
                         && h.ResourceGroup == resourceGroup
                         && h.CostType == "Service"
                         && h.CostDate >= startDate)
                .GroupBy(h => h.Name)
                .Select(g => new
                {
                    ServiceName = g.Key ?? "Unknown",
                    TotalCost = g.Sum(x => x.Cost)
                })
                .OrderByDescending(x => x.TotalCost)
                .ToListAsync();

            return services.ToDictionary(x => x.ServiceName, x => x.TotalCost);
        }
    }
}
