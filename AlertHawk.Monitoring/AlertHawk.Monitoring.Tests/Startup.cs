using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using AlertHawk.Monitoring.Infrastructure.Producers;
using AlertHawk.Monitoring.Infrastructure.Repositories.Class;
using EasyMemoryCache.Configuration;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Rebus.Bus;
using SharedModels;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Tests;

[ExcludeFromCodeCoverage]
public class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, false)
            .AddEnvironmentVariables()
            .Build();
        services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

        services.AddHangfire(config => config.UseInMemoryStorage());

        services.AddTransient<IMonitorTypeService, MonitorTypeService>();
        services.AddTransient<IMonitorService, MonitorService>();
        services.AddTransient<IMonitorGroupService, MonitorGroupService>();
        services.AddTransient<IMonitorAgentService, MonitorAgentService>();
        services.AddTransient<IMonitorNotificationService, MonitorNotificationService>();
        services.AddTransient<IMonitorHistoryService, MonitorHistoryService>();

        services.AddTransient<IMonitorTypeRepository, MonitorTypeRepository>();
        services.AddTransient<IMonitorRepository, MonitorRepository>();
        services.AddTransient<IMonitorHistoryRepository, MonitorHistoryRepository>();
        services.AddTransient<IMonitorNotificationRepository, MonitorNotificationRepository>();
        services.AddTransient<IMonitorAgentRepository, MonitorAgentRepository>();
        services.AddTransient<IMonitorManager, MonitorManager>();
        services.AddTransient<IMonitorGroupRepository, MonitorGroupRepository>();
        services.AddTransient<IMonitorAlertRepository, MonitorAlertRepository>();
        services.AddTransient<ISystemConfigurationRepository, SystemConfigurationRepository>();

        services.AddTransient<IHttpClientRunner, HttpClientRunner>();
        services.AddTransient<ITcpClientRunner, TcpClientRunner>();
        services.AddTransient<IK8sClientRunner, K8sClientRunner>();
        
        services.AddTransient<INotificationProducer, NotificationProducer>();

        var bus = Substitute.For<IBus>();
        bus.Send(Arg.Any<NotificationAlertMessage>(), Arg.Any<IDictionary<string, string>>())
            .Returns(Task.CompletedTask);
        services.AddSingleton(bus);
    }
}