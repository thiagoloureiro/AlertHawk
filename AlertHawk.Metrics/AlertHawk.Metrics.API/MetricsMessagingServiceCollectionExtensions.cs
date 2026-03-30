using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;
using SharedModels;

namespace AlertHawk.Metrics.API;

public static class MetricsMessagingServiceCollectionExtensions
{
    private const string RabbitMqNotificationQueue = "notifications";
    private const string AzureNotificationTopic = "notificationsTopic";

    public static IServiceCollection AddMetricsNotificationRebus(this IServiceCollection services,
        IConfiguration configuration)
    {
        var queueType = configuration.GetValue<string>("QueueType") ?? "RABBITMQ";
        var rabbitMqHost = configuration.GetValue<string>("RabbitMq:Host");
        var rabbitMqUser = configuration.GetValue<string>("RabbitMq:User");
        var rabbitMqPass = configuration.GetValue<string>("RabbitMq:Pass");
        var serviceBusConnectionString = configuration.GetValue<string>("ServiceBus:ConnectionString");

        var destination = queueType.ToUpperInvariant() switch
        {
            "RABBITMQ" => RabbitMqNotificationQueue,
            "SERVICEBUS" => AzureNotificationTopic,
            _ => throw new InvalidOperationException($"Unknown QueueType: {queueType}")
        };

        services.AddRebus(configure => configure
            .Serialization(s => s.UseSystemTextJson())
            .Routing(r => r.TypeBased().Map<NotificationAlertMessage>(destination))
            .Transport(t =>
            {
                switch (queueType.ToUpperInvariant())
                {
                    case "RABBITMQ":
                        t.UseRabbitMqAsOneWayClient(
                            BuildRabbitMqConnectionString(rabbitMqHost, rabbitMqUser, rabbitMqPass));
                        break;
                    case "SERVICEBUS":
                        if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
                        {
                            throw new InvalidOperationException(
                                "ServiceBus:ConnectionString is required when QueueType is SERVICEBUS.");
                        }

                        t.UseAzureServiceBusAsOneWayClient(serviceBusConnectionString);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown QueueType: {queueType}");
                }
            }));

        return services;
    }

    private static string BuildRabbitMqConnectionString(string? host, string? user, string? password)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("RabbitMq:Host is required when QueueType is RABBITMQ.");

        if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password))
            return $"amqp://{host}";

        var userEnc = Uri.EscapeDataString(user ?? "");
        var passEnc = Uri.EscapeDataString(password ?? "");
        return $"amqp://{userEnc}:{passEnc}@{host}";
    }
}
