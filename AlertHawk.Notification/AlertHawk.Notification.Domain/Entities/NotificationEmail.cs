namespace AlertHawk.Notification.Domain.Entities
{
    public class NotificationEmail
    {
        public string FromEmail { get; set; }
        public string ToEmail { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ToCCEmail { get; set; }
        public string ToBCCEmail { get; set; }
        public bool EnableSsl { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtmlBody { get; set; }
    }
}