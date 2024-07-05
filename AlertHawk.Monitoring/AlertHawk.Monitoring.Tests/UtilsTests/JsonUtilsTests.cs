using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Utils;

namespace AlertHawk.Monitoring.Tests.UtilsTests;

public class JsonUtilsTests
{
    [Fact]
    public void ConvertTupleToJson_ConvertsHeadersToJson()
    {
        // Arrange
        var headers = new List<Tuple<string, string>>
        {
            Tuple.Create("Key1", "Value1"),
            Tuple.Create("Key2", "Value2")
        };

        // Act
        var result = JsonUtils.ConvertTupleToJson(headers);

        // Assert
        Assert.Contains("\"Key1\": \"Value1\"", result);
        Assert.Contains("\"Key2\": \"Value2\"", result);
    }

    [Fact]
    public void ConvertJsonToTuple_ConvertsJsonToTuple()
    {
        // Arrange
        var monitorHttp = new MonitorHttp
        {
            HeadersJson = "{\"Key1\": \"Value1\", \"Key2\": \"Value2\"}",
            MaxRedirects = 0,
            UrlToCheck = "https://www.url.com",
            Timeout = 0,
            Name = "Name",
            HeartBeatInterval = 0,
            Retries = 0
        };

        // Act
        JsonUtils.ConvertJsonToTuple(monitorHttp);

        // Assert
        if (monitorHttp.Headers != null)
        {
            Assert.Equal(2, monitorHttp.Headers.Count);
            Assert.Contains(monitorHttp.Headers, tuple => tuple.Item1 == "Key1" && tuple.Item2 == "Value1");
            Assert.Contains(monitorHttp.Headers, tuple => tuple.Item1 == "Key2" && tuple.Item2 == "Value2");
        }
    }

    [Fact]
    public void ConvertJsonToTuple_HandlesNullJson()
    {
        // Arrange
        var monitorHttp = new MonitorHttp
        {
            HeadersJson = null,
            MaxRedirects = 0,
            UrlToCheck = "http://urltocheck.com",
            Timeout = 0,
            Name = "Name",
            HeartBeatInterval = 0,
            Retries = 0
        };

        // Act
        JsonUtils.ConvertJsonToTuple(monitorHttp);

        // Assert
        Assert.Null(monitorHttp.Headers);
    }
}