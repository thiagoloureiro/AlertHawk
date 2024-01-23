namespace AlertHawk.Notification.Tests
{
    public static class GlobalVariables
    {
        public static string TelegramWebHook { get; set; } = "telegramwebhook-replace";
        public static long TelegramChatId { get; set; } = 0000000000;

        public static string EmailPassword { get; set; } = "emailpassword-replace";
        public static string SlackWebHookUrl { get; set; } = "slackwebhookurl-replace";
    }
}