using FinOpsToolSample.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace FinOpsToolSample.Data
{
    public class FinOpsDbContext : DbContext
    {
        public DbSet<AnalysisRun> AnalysisRuns { get; set; }
        public DbSet<CostDetail> CostDetails { get; set; }
        public DbSet<ResourceAnalysis> ResourceAnalysis { get; set; }
        public DbSet<AiRecommendation> AiRecommendations { get; set; }
        public DbSet<HistoricalCost> HistoricalCosts { get; set; }

        public FinOpsDbContext(DbContextOptions<FinOpsDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<AnalysisRun>()
                .HasMany(a => a.CostDetails)
                .WithOne(c => c.AnalysisRun)
                .HasForeignKey(c => c.AnalysisRunId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnalysisRun>()
                .HasMany(a => a.Resources)
                .WithOne(r => r.AnalysisRun)
                .HasForeignKey(r => r.AnalysisRunId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnalysisRun>()
                .HasMany(a => a.AiRecommendations)
                .WithOne(ai => ai.AnalysisRun)
                .HasForeignKey(ai => ai.AnalysisRunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for better query performance
            modelBuilder.Entity<AnalysisRun>()
                .HasIndex(a => a.SubscriptionId);

            modelBuilder.Entity<AnalysisRun>()
                .HasIndex(a => a.RunDate);

            modelBuilder.Entity<CostDetail>()
                .HasIndex(c => new { c.AnalysisRunId, c.CostType });

            modelBuilder.Entity<ResourceAnalysis>()
                .HasIndex(r => new { r.AnalysisRunId, r.ResourceType });

            modelBuilder.Entity<ResourceAnalysis>()
                .HasIndex(r => r.ResourceGroup);

            modelBuilder.Entity<HistoricalCost>()
                .HasIndex(h => new { h.SubscriptionId, h.CostDate });

            modelBuilder.Entity<HistoricalCost>()
                .HasIndex(h => new { h.SubscriptionId, h.CostType, h.Name });

            modelBuilder.Entity<HistoricalCost>()
                .HasIndex(h => new { h.SubscriptionId, h.ResourceGroup, h.CostDate });
        }
    }
}
