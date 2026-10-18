using System.Net;
using System.Net.Http;
using System.Text;
using Azure;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class AzureThrottledRequestRetryTests
{
    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt_ReturnsResult()
    {
        var result = await AzureThrottledRequestRetry.ExecuteAsync(() => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_NonRetriableFailure_ThrowsImmediately()
    {
        await Assert.ThrowsAsync<RequestFailedException>(async () =>
            await AzureThrottledRequestRetry.ExecuteAsync<int>(() =>
                throw new RequestFailedException(400, "Bad Request")));
    }

    [Fact]
    public async Task SendPostWithRetryAsync_Success_ReturnsResponse()
    {
        using var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
            });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

        using var response = await AzureThrottledRequestRetry.SendPostWithRetryAsync(
            client,
            "https://example.test/arm",
            () => new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SendPostWithRetryAsync_NonRetriableError_DoesNotRetry()
    {
        using var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("nope", Encoding.UTF8, "application/json")
            });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

        using var response = await AzureThrottledRequestRetry.SendPostWithRetryAsync(
            client,
            "https://example.test/arm",
            () => new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SendCostManagementPostWithRetryAsync_EmptyToken_Throws()
    {
        using var client = new HttpClient();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await AzureThrottledRequestRetry.SendCostManagementPostWithRetryAsync(
                client,
                "https://example.test/query",
                "   ",
                () => new StringContent("{}", Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendCostManagementPostWithRetryAsync_Success_SetsBearerAndReturnsOk()
    {
        string? authHeader = null;
        using var handler = new StubHttpMessageHandler(request =>
        {
            authHeader = request.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });
        using var client = new HttpClient(handler);

        using var response = await AzureThrottledRequestRetry.SendCostManagementPostWithRetryAsync(
            client,
            "https://example.test/query",
            "test-token",
            () => new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer test-token", authHeader);
    }

    [Fact]
    public async Task SendPostWithRetryAsync_RetriableThenSuccess_RetriesOnce()
    {
        var attempt = 0;
        using var handler = new StubHttpMessageHandler(_ =>
        {
            attempt++;
            if (attempt == 1)
            {
                var throttled = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                throttled.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
                return throttled;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });
        using var client = new HttpClient(handler);

        using var response = await AzureThrottledRequestRetry.SendPostWithRetryAsync(
            client,
            "https://example.test/arm",
            () => new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.CallCount);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responder(request));
        }
    }
}
