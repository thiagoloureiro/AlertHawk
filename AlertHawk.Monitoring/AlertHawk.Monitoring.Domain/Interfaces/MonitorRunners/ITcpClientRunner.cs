using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface ITcpClientRunner
{
    Task<bool> CheckTcpAsync(MonitorTcp monitorTcp);
    bool MakeTcpCall(MonitorTcp monitorTcp);
}