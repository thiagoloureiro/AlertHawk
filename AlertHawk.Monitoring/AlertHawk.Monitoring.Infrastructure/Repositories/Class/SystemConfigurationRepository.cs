using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class SystemConfigurationRepository : RepositoryBase, ISystemConfigurationRepository
{
    private readonly string _connstring;

    public SystemConfigurationRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<SystemConfiguration?> GetSystemConfigurationByKey(string key)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"SELECT Id, [Key], Value, Description, CreatedAt, UpdatedAt 
                       FROM [SystemConfiguration] 
                       WHERE [Key] = @key";
        return await db.QueryFirstOrDefaultAsync<SystemConfiguration>(sql, new { key }, commandType: CommandType.Text);
    }

    public async Task<bool> IsMonitorExecutionDisabled()
    {
        // Check manual disable flag
        var config = await GetSystemConfigurationByKey("MonitorExecutionDisabled");
        if (config != null && bool.TryParse(config.Value, out var isDisabled) && isDisabled)
        {
            return true; // Manually disabled
        }
        
        // Check if we're within a maintenance window
        return await IsWithinMaintenanceWindow();
    }

    public async Task<bool> IsWithinMaintenanceWindow()
    {
        var (startUtc, endUtc) = await GetMaintenanceWindow();
        
        if (!startUtc.HasValue || !endUtc.HasValue)
        {
            return false; // No maintenance window set
        }
        
        var now = DateTime.UtcNow;
        return now >= startUtc.Value && now <= endUtc.Value;
    }

    public async Task SetMaintenanceWindow(DateTime? startUtc, DateTime? endUtc)
    {
        // If both are null, clear the maintenance window
        if (!startUtc.HasValue && !endUtc.HasValue)
        {
            await using var db = new SqlConnection(_connstring);
            string deleteSql = @"DELETE FROM [SystemConfiguration] WHERE [Key] IN ('MaintenanceWindowStartUtc', 'MaintenanceWindowEndUtc')";
            await db.ExecuteAsync(deleteSql, commandType: CommandType.Text);
            return;
        }
        
        // If setting a window, both start and end must be provided
        if (!startUtc.HasValue || !endUtc.HasValue)
        {
            throw new ArgumentException("Both start and end times must be provided when setting a maintenance window.");
        }
        
        // Validate that start is before end
        if (startUtc.Value >= endUtc.Value)
        {
            throw new ArgumentException("Maintenance window start time must be before end time.");
        }
        
        // Set both values
        await UpsertSystemConfiguration(
            "MaintenanceWindowStartUtc",
            startUtc.Value.ToString("O"), // ISO 8601 format
            "Maintenance window start time in UTC. When set, monitors will be disabled during this window."
        );
        
        await UpsertSystemConfiguration(
            "MaintenanceWindowEndUtc",
            endUtc.Value.ToString("O"), // ISO 8601 format
            "Maintenance window end time in UTC. When set, monitors will be disabled during this window."
        );
    }

    public async Task<(DateTime? StartUtc, DateTime? EndUtc)> GetMaintenanceWindow()
    {
        var startConfig = await GetSystemConfigurationByKey("MaintenanceWindowStartUtc");
        var endConfig = await GetSystemConfigurationByKey("MaintenanceWindowEndUtc");
        
        DateTime? startUtc = null;
        DateTime? endUtc = null;
        
        if (startConfig != null && DateTime.TryParse(startConfig.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedStart))
        {
            startUtc = parsedStart.ToUniversalTime();
        }
        
        if (endConfig != null && DateTime.TryParse(endConfig.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedEnd))
        {
            endUtc = parsedEnd.ToUniversalTime();
        }
        
        return (startUtc, endUtc);
    }

    public async Task UpsertSystemConfiguration(string key, string value, string? description = null)
    {
        await using var db = new SqlConnection(_connstring);
        
        // Check if configuration exists
        var existing = await GetSystemConfigurationByKey(key);
        
        if (existing != null)
        {
            // Update existing
            string sql = @"UPDATE [SystemConfiguration] 
                          SET Value = @value, 
                              Description = @description,
                              UpdatedAt = @updatedAt
                          WHERE [Key] = @key";
            await db.ExecuteAsync(sql, new { key, value, description, updatedAt = DateTime.UtcNow }, commandType: CommandType.Text);
        }
        else
        {
            // Insert new
            string sql = @"INSERT INTO [SystemConfiguration] ([Key], Value, Description, CreatedAt, UpdatedAt)
                          VALUES (@key, @value, @description, @createdAt, @updatedAt)";
            await db.ExecuteAsync(sql, new { key, value, description, createdAt = DateTime.UtcNow, updatedAt = (DateTime?)null }, commandType: CommandType.Text);
        }
    }

    public async Task InitializeTableIfNotExists()
    {
        try
        {
            await using var db = new SqlConnection(_connstring);
            
            // Check if table exists
            string checkTableSql = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = 'dbo' 
                AND TABLE_NAME = 'SystemConfiguration'";
            
            var tableExists = await db.ExecuteScalarAsync<int>(checkTableSql, commandType: CommandType.Text);
            
            if (tableExists == 0)
            {
                // Create table if it doesn't exist
                string createTableSql = @"
                    CREATE TABLE [SystemConfiguration] (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        [Key] NVARCHAR(255) NOT NULL UNIQUE,
                        Value NVARCHAR(MAX) NOT NULL,
                        Description NVARCHAR(MAX) NULL,
                        CreatedAt DATETIME NOT NULL,
                        UpdatedAt DATETIME NULL
                    );
                    
                    CREATE INDEX IX_SystemConfiguration_Key ON [SystemConfiguration]([Key]);";
                
                await db.ExecuteAsync(createTableSql, commandType: CommandType.Text);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - allow application to continue
            // The table might already exist or there might be a permission issue
            System.Diagnostics.Debug.WriteLine($"Error initializing SystemConfiguration table: {ex.Message}");
        }
    }
}
