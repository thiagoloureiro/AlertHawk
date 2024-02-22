using AlertHawk.Monitoring.Domain.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlertHawk.Monitoring.Domain.Utils
{
    public static class JsonUtils
    {
        public static string ConvertTupleToJson(List<Tuple<string, string>> headers)
        {
            var dict = headers.ToDictionary(t => t.Item1, t => t.Item2);
            return JsonConvert.SerializeObject(dict, Formatting.Indented);
        }
        public static void ConvertJsonToTuple(MonitorHttp monitorHttp)
        {
            try
            {
                if (monitorHttp.HeadersJson != null)
                {
                    JObject jsonObj = JObject.Parse(monitorHttp.HeadersJson);

                    // Extract values and create Tuple
                    List<Tuple<string, string>> properties = new List<Tuple<string, string>>();

                    foreach (var property in jsonObj.Properties())
                    {
                        properties.Add(Tuple.Create(property.Name, property.Value.ToString()));
                    }

                    monitorHttp.Headers = properties;
                }
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        }
    }
}
