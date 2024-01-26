using MassTransit;

namespace SharedModels;

public class NotificationConsumer : IConsumer<NotificationAlert>
{
    public async Task Consume(ConsumeContext<NotificationAlert> context)
    {
        // Handle the received message
        Console.WriteLine(
            $"NotificationId: {context.Message.NotificationId} Notification Message: {context.Message.Message}");
    }
}