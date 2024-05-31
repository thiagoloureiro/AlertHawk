using AlertHawk.Notification.Domain.Entities;
using Newtonsoft.Json.Linq;

namespace AlertHawk.Notification.Domain.Utils;

public static class JsonUtils
{
    public static void ConvertJsonToTuple(NotificationWebHook webHook)
    {
        if (webHook.HeadersJson != null)
        {
            JObject jsonObj = JObject.Parse(webHook.HeadersJson);

            // Extract values and create Tuple
            List<Tuple<string, string>>? properties = new List<Tuple<string, string>>();

            foreach (var property in jsonObj.Properties())
            {
                properties.Add(Tuple.Create(property.Name, property.Value.ToString()));
            }

            webHook.Headers = properties;
        }
    }
}