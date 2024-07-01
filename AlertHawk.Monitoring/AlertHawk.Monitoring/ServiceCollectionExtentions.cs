using System.Diagnostics.CodeAnalysis;
using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using AlertHawk.Monitoring.Infrastructure.Repositories.Class;

namespace AlertHawk.Monitoring
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomServices(this IServiceCollection services)
        {
            services.AddTransient<IMonitorTypeService, MonitorTypeService>();
            services.AddTransient<IMonitorService, MonitorService>();
            services.AddTransient<IMonitorGroupService, MonitorGroupService>();
            services.AddTransient<IMonitorAgentService, MonitorAgentService>();
            services.AddTransient<IMonitorAlertService, MonitorAlertService>();
            services.AddTransient<IHealthCheckService, HealthCheckService>();
            services.AddTransient<IMonitorReportService, MonitorReportService>();
            services.AddTransient<IMonitorNotificationService, MonitorNotificationService>();
            services.AddTransient<IMonitorHistoryService, MonitorHistoryService>();

            return services;
        }

        public static IServiceCollection AddCustomRepositories(this IServiceCollection services)
        {
            services.AddTransient<IMonitorTypeRepository, MonitorTypeRepository>();
            services.AddTransient<IMonitorRepository, MonitorRepository>();
            services.AddTransient<IMonitorAgentRepository, MonitorAgentRepository>();
            services.AddTransient<IMonitorManager, MonitorManager>();
            services.AddTransient<IMonitorGroupRepository, MonitorGroupRepository>();
            services.AddTransient<IMonitorAlertRepository, MonitorAlertRepository>();
            services.AddTransient<IHealthCheckRepository, HealthCheckRepository>();
            services.AddTransient<IMonitorReportRepository, MonitorReportRepository>();
            services.AddTransient<IMonitorNotificationRepository, MonitorNotificationRepository>();
            services.AddTransient<IMonitorHistoryRepository, MonitorHistoryRepository>();

            return services;
        }
    }
}
