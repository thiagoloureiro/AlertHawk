using System.Net;
using System.Text;
using System.Text.Json;
using FinOpsToolSample.Models;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class AIRecommendationServiceTests
{
    private static AzureResourceData SampleData() =>
        new()
        {
            SubscriptionName = "ProdSub",
            SubscriptionId = "sub-123",
            TotalMonthlyCost = 42.5m,
            CostsByResourceGroup = new Dictionary<string, decimal> { ["rg1"] = 10 },
            CostsByService =
            [
                new ServiceCostDetail { ServiceName = "Compute", ResourceGroup = "rg1", Cost = 10 }
            ],
            Resources =
            [
                new ResourceInfo
                {
                    Type = "VM",
                    Name = "vm1",
                    ResourceGroup = "rg1",
                    Location = "eastus"
                }
            ]
        };

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Handler { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Handler!(request, cancellationToken);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenApiReturnsContent_ReturnsTextAndResponse_WritesReportInCurrentDirectory()
    {
        var apiJson = JsonSerializer.Serialize(new
        {
            message_id = "mid",
            agent_id = "aid",
            model = "test-model",
            timestamp = 0d,
            conversation_id = "conv-1",
            application_id = "app",
            output = new { content = "Scale down vm1.", tools_called = Array.Empty<string>() }
        });

        var httpHandler = new TestHttpMessageHandler
        {
            Handler = async (req, ct) =>
            {
                Assert.Equal(HttpMethod.Post, req.Method);
                var body = await req.Content!.ReadAsStringAsync(ct);
                Assert.Contains("ProdSub", body);
                Assert.Contains("sub-123", body);
                Assert.Contains("\"input\":", body);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(apiJson, Encoding.UTF8, "application/json")
                };
            }
        };

        using var httpClient = new HttpClient(httpHandler);
        var tempDir = Path.Combine(Path.GetTempPath(), "FinOpsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var svc = new AIRecommendationService("secret-key", "https://api.example/ai", "x-api-key", httpClient);

            var (recommendations, response) = await svc.GetRecommendationsAsync(SampleData());

            Assert.Equal("Scale down vm1.", recommendations);
            Assert.NotNull(response);
            Assert.Equal("test-model", response!.model);
            Assert.Equal("conv-1", response.conversation_id);
            var report = Directory.GetFiles(tempDir, "FinOps_Report_*.md").SingleOrDefault();
            Assert.NotNull(report);
            var text = await File.ReadAllTextAsync(report, TestContext.Current.CancellationToken);
            Assert.Contains("ProdSub", text);
            Assert.Contains("Scale down vm1.", text);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // ignore cleanup races on CI
            }
        }
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenOutputContentMissing_ReturnsPlaceholderAndNullResponse()
    {
        var apiJson = JsonSerializer.Serialize(new
        {
            message_id = "mid",
            agent_id = "aid",
            model = "m",
            timestamp = 0d,
            conversation_id = "c",
            application_id = "a",
            output = new { content = (string?)null, tools_called = Array.Empty<string>() }
        });

        var httpHandler = new TestHttpMessageHandler
        {
            Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(apiJson, Encoding.UTF8, "application/json")
            })
        };

        using var httpClient = new HttpClient(httpHandler);
        var svc = new AIRecommendationService("k", "https://api.example/ai", "x-api-key", httpClient);

        var (recommendations, response) = await svc.GetRecommendationsAsync(SampleData());

        Assert.Equal("No recommendations received from AI AI.", recommendations);
        Assert.Null(response);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenApiReturnsError_ReturnsEmptyAndNullResponse()
    {
        var httpHandler = new TestHttpMessageHandler
        {
            Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad", Encoding.UTF8, "text/plain")
            })
        };

        using var httpClient = new HttpClient(httpHandler);
        var svc = new AIRecommendationService("k", "https://api.example/ai", "x-api-key", httpClient);

        var (recommendations, response) = await svc.GetRecommendationsAsync(SampleData());

        Assert.Equal(string.Empty, recommendations);
        Assert.Null(response);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WhenBodyIsInvalidJson_ReturnsEmptyAndNullResponse()
    {
        var httpHandler = new TestHttpMessageHandler
        {
            Handler = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "application/json")
            })
        };

        using var httpClient = new HttpClient(httpHandler);
        var svc = new AIRecommendationService("k", "https://api.example/ai", "x-api-key", httpClient);

        var (recommendations, response) = await svc.GetRecommendationsAsync(SampleData());

        Assert.Equal(string.Empty, recommendations);
        Assert.Null(response);
    }
}
