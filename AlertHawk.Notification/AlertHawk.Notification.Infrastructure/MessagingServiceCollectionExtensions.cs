using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;
using SharedModels;

namespace AlertHawk.Notification.Infrastructure;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationRebus(this IServiceCollection services, IConfiguration configuration)
    {
        var queueType = configuration.GetValue<string>("QueueType") ?? "RABBITMQ";
        var rabbitMqHost = configuration.GetValue<string>("RabbitMq:Host");
        var rabbitMqUser = configuration.GetValue<string>("RabbitMq:User");
        var rabbitMqPass = configuration.GetValue<string>("RabbitMq:Pass");
        var serviceBusConnectionString = configuration.GetValue<string>("ServiceBus:ConnectionString");
        var serviceBusQueueName = configuration.GetValue<string>("ServiceBus:QueueName");

        services.AddRebus(configure => configure
            .Serialization(s => s.UseSystemTextJson())
            .Transport(t =>
            {
                switch (queueType.ToUpperInvariant())
                {
                    case "RABBITMQ":
                        t.UseRabbitMq(BuildRabbitMqConnectionString(rabbitMqHost, rabbitMqUser, rabbitMqPass), "notifications");
                        break;
                    case "SERVICEBUS":
                        if (string.IsNullOrWhiteSpace(serviceBusConnectionString) ||
                            string.IsNullOrWhiteSpace(serviceBusQueueName))
                        {
                            throw new InvalidOperationException(
                                "ServiceBus:ConnectionString and ServiceBus:QueueName are required when QueueType is SERVICEBUS.");
                        }

                        t.UseAzureServiceBus(serviceBusConnectionString, serviceBusQueueName);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown QueueType: {queueType}");
                }
            }));

        services.AutoRegisterHandlersFromAssemblyOf<NotificationConsumer>();
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
