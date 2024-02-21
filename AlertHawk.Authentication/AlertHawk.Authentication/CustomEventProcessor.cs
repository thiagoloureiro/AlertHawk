using Sentry.Extensibility;

namespace AlertHawk.Authentication;

public class CustomEventProcessor : ISentryEventProcessor
{
    public SentryEvent? Process(SentryEvent? @event)
    {
        // Check if the event is not null and it has an exception
        if (@event != null && @event.Exception != null)
        {
            // Check if the exception message contains the error code you want to ignore
            if (@event.Exception.Message.Contains("IDX10223"))
            {
                // Return null to indicate that this event should be ignored
                return null;
            }
        }
        
        // Return the event as-is if it should not be ignored
        return @event;
    }
}