using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;

namespace AlertHawk.Monitoring.Tests.RunnerTests;

public class HttpClientRunnerTests : IClassFixture<HttpClientRunner>
{
    private readonly IHttpClientRunner _httpClientRunner;

    public HttpClientRunnerTests(IHttpClientRunner httpClientRunner)
    {
        _httpClientRunner = httpClientRunner;
    }

    [Theory]
    [InlineData("https://httpbin.org/get", MonitorHttpMethod.Get)]
    [InlineData("https://httpbin.org/post", MonitorHttpMethod.Post)]
    [InlineData("https://httpbin.org/put", MonitorHttpMethod.Put)]
    public async Task Should_Make_HttpClient_Call_OK_Result(string url, MonitorHttpMethod method)
    {
        // Arrange
        var monitorHttp = new MonitorHttp
        {
            UrlToCheck = url,
            MonitorId = 1,
            Name = "Test",
            Id = 1,
            CheckCertExpiry = true,
            IgnoreTlsSsl = false,
            Timeout = 10,
            MonitorHttpMethod = method,
            MaxRedirects = 5,
            HeartBeatInterval = 1,
            Retries = 0,
            LastStatus = true,
            ResponseTime = 10
        };

        // Act
        var result = await _httpClientRunner.MakeHttpClientCall(monitorHttp);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.ReasonPhrase);
        Assert.NotNull(result.Content);
    }

    [Theory]
    [InlineData("https://httpbin.org/get1", MonitorHttpMethod.Get)]
    [InlineData("https://httpbin.org/post1", MonitorHttpMethod.Post)]
    [InlineData("https://httpbin.org/put1", MonitorHttpMethod.Put)]
    public async Task Should_Make_HttpClient_Call_NotFound_Result(string url, MonitorHttpMethod method)
    {
        // Arrange
        var monitorHttp = new MonitorHttp
        {
            UrlToCheck = url,
            MonitorId = 1,
            Name = "Test",
            Id = 1,
            CheckCertExpiry = true,
            IgnoreTlsSsl = false,
            Timeout = 10,
            MonitorHttpMethod = method,
            MaxRedirects = 5,
            HeartBeatInterval = 1,
            Retries = 0,
        };

        // Act
        var result = await _httpClientRunner.MakeHttpClientCall(monitorHttp);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
    }
}