using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorTypeService : IMonitorTypeService
{
    private readonly IMonitorTypeRepository _monitorTypeRepository;

    public MonitorTypeService(IMonitorTypeRepository monitorTypeRepository)
    {
        _monitorTypeRepository = monitorTypeRepository;
    }

    public async Task<IEnumerable<Entities.MonitorType>> GetMonitorType()
    {
        return await _monitorTypeRepository.GetMonitorType();
    }
}