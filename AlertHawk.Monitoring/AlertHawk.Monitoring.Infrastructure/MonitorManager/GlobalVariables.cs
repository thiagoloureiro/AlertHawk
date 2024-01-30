namespace AlertHawk.Monitoring.Infrastructure.MonitorManager;

public static class GlobalVariables
{
    public static bool MasterNode { get; set; }
    public static int NodeId { get; set; }
    public static List<int>? TaskList { get; set; }
}