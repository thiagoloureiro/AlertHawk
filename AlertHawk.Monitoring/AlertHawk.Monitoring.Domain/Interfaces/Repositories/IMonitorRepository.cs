using AlertHawk.Monitoring.Domain.Entities;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorRepository
{
    Task<IEnumerable<Monitor>> GetMonitorList();
    Task<IEnumerable<MonitorHttp>> GetHttpMonitorByIds(List<int> ids);
    Task<IEnumerable<Monitor>> GetMonitorListByIds(List<int> ids);
}