namespace SharedModels
{
    public interface NotificationAlert
    {
        int NotificationId { get; set; }

        DateTime TimeStamp { get; set; }

        public string Message { get; set; }
        public string ReasonPhrase { get; set; }
        public int StatusCode { get; set; }
    }   
}