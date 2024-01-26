namespace SharedModels
{
    public interface NotificationAlert
    {
        int NotificationId { get; set; }

        DateTime TimeStamp { get; set; }

        public string Message { get; set; }
    }   
}