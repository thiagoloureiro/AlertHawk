using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Services;

[ExcludeFromCodeCoverage]
public class LogCleanupService
{
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<LogCleanupService> _logger;

    public LogCleanupService(
        IClickHouseService clickHouseService,
        ILogger<LogCleanupService> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    public async Task CleanupLogsAsync()
    {
        try
        {
            _logger.LogInformation("Starting periodic system log cleanup - truncating ClickHouse system log tables");
            
            await _clickHouseService.CleanupSystemLogsAsync();
            
            _logger.LogInformation("System log cleanup completed successfully - all system log tables truncated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during system log cleanup");
            throw;
        }
    }
}
