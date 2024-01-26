using System;
using System.Threading.Tasks;
using ConsoleApp1;
using MassTransit;

public class NotificationConsumer : IConsumer<NotificationMessage>
{
    public async Task Consume(ConsumeContext<NotificationMessage> context)
    {
        var message = context.Message;
        Console.WriteLine($"Received notification: {message.Content}");
    }
}