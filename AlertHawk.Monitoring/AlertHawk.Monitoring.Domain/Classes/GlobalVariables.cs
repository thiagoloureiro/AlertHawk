using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Classes;

[ExcludeFromCodeCoverage]
public static class GlobalVariables
{
    public static bool MasterNode { get; set; }
    public static int NodeId { get; set; }
    public static List<int>? HttpTaskList { get; set; }
    public static List<int>? TcpTaskList { get; set; }
    public static List<int>? K8sTaskList { get; set; }
    public static string? RandomString { get; set; }
}