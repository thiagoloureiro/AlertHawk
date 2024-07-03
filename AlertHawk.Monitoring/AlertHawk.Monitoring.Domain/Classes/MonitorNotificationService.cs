using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorNotificationService: IMonitorNotificationService
{
    private readonly IMonitorGroupService _monitorGroupService;
    private readonly IMonitorNotificationRepository _monitorNotificationRepository;

    public MonitorNotificationService(IMonitorGroupService monitorGroupService, IMonitorNotificationRepository monitorNotificationRepository)
    {
        _monitorGroupService = monitorGroupService;
        _monitorNotificationRepository = monitorNotificationRepository;
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        return await _monitorNotificationRepository.GetMonitorNotifications(id);
    }

    public async Task AddMonitorNotification(MonitorNotification monitorNotification)
    {
        await _monitorNotificationRepository.AddMonitorNotification(monitorNotification);
    }

    public async Task RemoveMonitorNotification(MonitorNotification monitorNotification)
    {
        await _monitorNotificationRepository.RemoveMonitorNotification(monitorNotification);
    }
    
    public async Task AddMonitorGroupNotification(MonitorGroupNotification monitorGroupNotification)
    {
        var monitorList = await _monitorGroupService.GetMonitorGroupById(monitorGroupNotification.MonitorGroupId);

        foreach (var monitor in monitorList.Monitors)
        {
            var monitorNotification = new MonitorNotification
            {
                MonitorId = monitor.Id,
                NotificationId = monitorGroupNotification.NotificationId
            };

            var notificationExist = await _monitorNotificationRepository.GetMonitorNotifications(monitor.Id);
            if (notificationExist.All(x => x.NotificationId != monitorGroupNotification.NotificationId))
            {
                await AddMonitorNotification(monitorNotification);
            }
        }
    }

    public async Task RemoveMonitorGroupNotification(MonitorGroupNotification monitorGroupNotification)
    {
        var monitorList = await _monitorGroupService.GetMonitorGroupById(monitorGroupNotification.MonitorGroupId);

        if (monitorList != null)
        {
            foreach (var monitor in monitorList.Monitors)
            {
                var monitorNotification = new MonitorNotification
                {
                    MonitorId = monitor.Id,
                    NotificationId = monitorGroupNotification.NotificationId
                };
                await RemoveMonitorNotification(monitorNotification);
            }
        }
    }
}