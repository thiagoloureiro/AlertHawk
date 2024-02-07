using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface ITcpClientRunner
{
    Task<bool> CheckTcpAsync(string ipAddress, int port, int maxRetries, int retryIntervalMilliseconds);
}