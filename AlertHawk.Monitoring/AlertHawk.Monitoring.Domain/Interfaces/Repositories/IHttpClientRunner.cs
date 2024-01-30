using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IHttpClientRunner
{
    Task StartRunnerManager();
    Task<MonitorHttp> CheckUrlsAsync(MonitorHttp monitorHttp);
}