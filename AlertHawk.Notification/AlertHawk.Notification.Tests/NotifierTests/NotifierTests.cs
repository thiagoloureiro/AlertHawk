using System.Net;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Infrastructure.Notifiers;

namespace AlertHawk.Notification.Tests.NotifierTests;

public class NotifierTests
{
    private const string TelegramToken = "123456789:AAFakeTokenForTests";

    [Fact]
    public async Task Should_Send_Telegram_Notification()
    {
        var handler = TelegramHandler();
        var notifier = new TelegramNotifier(new HttpClient(handler));

        var result = await notifier.SendNotification(42, "Message", TelegramToken);

        Assert.NotNull(result);
        Assert.Equal(42, result.MessageId);
        Assert.Contains("Message", handler.RequestBody);
        Assert.DoesNotContain("\u2705", handler.RequestBody);
    }

    [Fact]
    public async Task Should_Send_Success_Telegram_Notification()
    {
        var handler = TelegramHandler();
        var notifier = new TelegramNotifier(new HttpClient(handler));

        var result = await notifier.SendNotification(42, "Success", TelegramToken);

        Assert.NotNull(result);
        Assert.Contains("Success", handler.RequestBody);
        Assert.Contains("\\u2705", handler.RequestBody);
    }

    [Fact]
    public async Task Should_Send_Slack_Notification()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "ok");
        var notifier = new SlackNotifier(new HttpClient(handler));

        await notifier.SendNotification("alerthawk-test", "Message", "https://example.test/slack");

        Assert.Contains("alerthawk-test", handler.RequestBody);
        Assert.Contains("Message", handler.RequestBody);
        Assert.DoesNotContain(":white_check_mark:", handler.RequestBody);
    }

    [Fact]
    public async Task Should_Send_Success_Slack_Notification()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "ok");
        var notifier = new SlackNotifier(new HttpClient(handler));

        await notifier.SendNotification("alerthawk-test", "Success", "https://example.test/slack");

        Assert.Contains(":white_check_mark: Success", handler.RequestBody);
    }

    [Fact]
    public async Task Should_Send_Teams_Notification()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "1");
        var notifier = new TeamsNotifier(new HttpClient(handler));

        await notifier.SendNotification("Test from Unit testing", "https://example.test/teams");

        Assert.Contains("Test from Unit testing", handler.RequestBody);
        Assert.DoesNotContain("\u2705", handler.RequestBody);
    }

    [Fact]
    public async Task Should_Send_Success_Teams_Notification()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "1");
        var notifier = new TeamsNotifier(new HttpClient(handler));

        await notifier.SendNotification("Success Test from Unit testing", "https://example.test/teams");

        Assert.Contains("Success Test from Unit testing", handler.RequestBody);
        Assert.Contains("\\u2705", handler.RequestBody);
    }

    [Fact]
    public async Task Should_Send_Push_Notification()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.pushy.me") };
        var notifier = new PushNotifier(client);
        Environment.SetEnvironmentVariable("PUSHY_API_KEY", "test-push-key");

        var notificationPush = new NotificationPush
        {
            NotificationId = 1,
            PushNotificationBody = new PushNotificationBody
            {
                to = "device-token",
                data = new PushNotificationData { message = "message" },
                notification = new PushNotificationItem
                {
                    title = "title",
                    body = "body",
                    badge = 1,
                    sound = "ping.aiff"
                }
            }
        };

        await notifier.SendNotification("from backend", notificationPush);

        Assert.Contains("/push", handler.RequestUri?.AbsolutePath);
        Assert.Contains("test-push-key", handler.RequestUri?.Query);
        Assert.Contains("from backend", handler.RequestBody);
    }

    private static RecordingHandler TelegramHandler()
    {
        const string body = """
            {"ok":true,"result":{"message_id":42,"date":1700000000,"chat":{"id":42,"type":"private"},"text":"ok"}}
            """;
        return new RecordingHandler(HttpStatusCode.OK, body);
    }
}
