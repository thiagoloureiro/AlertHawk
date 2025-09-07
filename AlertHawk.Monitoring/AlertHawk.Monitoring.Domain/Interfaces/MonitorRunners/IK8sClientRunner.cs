using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

public interface IK8sClientRunner
{
    Task CheckK8sAsync(MonitorK8s monitorK8s);
    Task<(bool succeeded, string responseMessage, MonitorHistory monitorHistory)> CallK8S(MonitorK8s monitorK8s);
}