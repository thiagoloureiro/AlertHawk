using AlertHawk.Notification.Domain.Classes;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Interfaces.Services;
using AlertHawk.Notification.Infrastructure.Notifiers;
using AlertHawk.Notification.Infrastructure.Repositories.Class;
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

        services.AddTransient<INotificationTypeService, NotificationTypeService>();
        services.AddTransient<INotificationTypeRepository, NotificationTypeRepository>();

        services.AddTransient<INotificationService, NotificationService>();
        services.AddTransient<INotificationRepository, NotificationRepository>();
        services.AddTransient<IMailNotifier, MailNotifier>();
        services.AddTransient<ISlackNotifier, SlackNotifier>();
        services.AddTransient<ITeamsNotifier, TeamsNotifier>();
        services.AddTransient<ITelegramNotifier, TelegramNotifier>();
        services.AddTransient<IWebHookNotifier, WebHookNotifier>();
        services.AddTransient<IPushNotifier, PushNotifier>();
    }
}