using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using AlertHawk.Monitoring.Infrastructure.Repositories.Class;
using EasyMemoryCache.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Notification.Tests;

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

        services.AddTransient<IMonitorTypeService, MonitorTypeService>();
        services.AddTransient<IMonitorService, MonitorService>();
        services.AddTransient<IMonitorGroupService, MonitorGroupService>();
        services.AddTransient<IMonitorAgentService, MonitorAgentService>();
        
        services.AddTransient<IMonitorTypeRepository, MonitorTypeRepository>();
        services.AddTransient<IMonitorRepository, MonitorRepository>();
        services.AddTransient<IMonitorAgentRepository, MonitorAgentRepository>();
        services.AddTransient<IMonitorManager, MonitorManager>();
        services.AddTransient<IMonitorGroupRepository, MonitorGroupRepository>();

        services.AddTransient<IHttpClientRunner, HttpClientRunner>();
        services.AddTransient<ITcpClientRunner, TcpClientRunner>();
        services.AddTransient<IHttpClientScreenshot, HttpClientScreenshot>();
    }
}