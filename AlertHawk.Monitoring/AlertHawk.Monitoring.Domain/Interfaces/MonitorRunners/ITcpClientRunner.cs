using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface ITcpClientRunner
{
    Task<bool> CheckTcpAsync(MonitorTcp monitorTcp);
    Task<bool> MakeTcpCall(MonitorTcp monitorTcp);
}