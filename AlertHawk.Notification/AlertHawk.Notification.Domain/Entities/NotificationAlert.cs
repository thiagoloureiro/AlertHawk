namespace SharedModels
{
    public interface NotificationAlert
    {
        int NotificationId { get; set; }
        public int MonitorId { get; set; }
        public string Service { get; set; }
        public int Region { get; set; }
        public int Environment { get; set; }
        public string URL { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public bool Success { get; set; }
        DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public string ReasonPhrase { get; set; }
        public int StatusCode { get; set; }
    }
}