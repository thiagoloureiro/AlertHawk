namespace AlertHawk.Monitoring.Infrastructure.MonitorManager;

public static class GlobalVariables
{
    public static bool MasterNode { get; set; }
    public static int NodeId { get; set; }
    public static List<int>? HttpTaskList { get; set; }
    public static List<int>? TcpTaskList { get; set; }
}