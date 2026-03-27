namespace FinOpsToolSample.Services;

public interface IDataCleanupService
{
    Task<CleanupResult> CleanupOldAnalysisRunsAsync();
}

public class CleanupResult
{
    public bool Success { get; set; }
    public int AnalysisRunsDeleted { get; set; }
    public int AnalysisRunsKept { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> DeletedRunDetails { get; set; } = new();
    public string? ErrorDetails { get; set; }
}
