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
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Monitoring.Tests;

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

        services.AddTransient<IHttpClientRunner, HttpClientRunner>();
        services.AddTransient<ITcpClientRunner, TcpClientRunner>();
        services.AddTransient<IHttpClientScreenshot, HttpClientScreenshot>();
        
        services.AddTransient<INotificationProducer, NotificationProducer>();
        var rabbitMqHost = configuration.GetValue<string>("RabbitMq:Host");
        var rabbitMqUser = configuration.GetValue<string>("RabbitMq:User");
        var rabbitMqPass = configuration.GetValue<string>("RabbitMq:Pass");

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri($"rabbitmq://{rabbitMqHost}"), h =>
                {
                    h.Username(rabbitMqUser);
                    h.Password(rabbitMqPass);
                });
            });
        });
    }
}