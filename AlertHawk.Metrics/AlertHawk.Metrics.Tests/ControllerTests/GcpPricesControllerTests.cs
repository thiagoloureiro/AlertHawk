using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertHawk.Metrics.Tests;

public class GcpPricesControllerTests
{
    private readonly Mock<IGcpPricesService> _mockGcpPricesService;
    private readonly GcpPricesController _controller;

    public GcpPricesControllerTests()
    {
        _mockGcpPricesService = new Mock<IGcpPricesService>();
        _controller = new GcpPricesController(_mockGcpPricesService.Object);
    }

    [Fact]
    public async Task GetGcpPrices_WithValidRequest_ReturnsOkWithResponse()
    {
        var request = new GcpPriceRequest
        {
            CurrencyCode = "USD",
            Region = "us-central1",
            MachineType = "n1-standard-2"
        };

        var expectedResponse = new GcpPriceResponse
        {
            CurrencyCode = "USD",
            Count = 1,
            Items = new List<GcpPriceItem>
            {
                new()
                {
                    SkuId = "sku-1",
                    Description = "N1 Predefined Instance Core running in Americas",
                    ResourceGroup = "N1",
                    UsageType = "OnDemand",
                    CurrencyCode = "USD",
                    UnitPrice = 0.031611
                }
            }
        };

        _mockGcpPricesService
            .Setup(s => s.GetPricesAsync(request))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetGcpPrices(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GcpPriceResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Count, response.Count);
        Assert.Single(response.Items);
        _mockGcpPricesService.Verify(s => s.GetPricesAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetGcpPrices_WithNullRequest_ReturnsBadRequest()
    {
        var result = await _controller.GetGcpPrices(null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
        var responseType = badRequestResult.Value.GetType();
        var errorProperty = responseType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Request body is required", errorProperty.GetValue(badRequestResult.Value)?.ToString());
        _mockGcpPricesService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGcpPrices_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        var request = new GcpPriceRequest
        {
            Region = "us-central1",
            MachineType = "e2-medium"
        };

        _mockGcpPricesService
            .Setup(s => s.GetPricesAsync(request))
            .ThrowsAsync(new InvalidOperationException("GCP Billing API key is not configured"));

        var result = await _controller.GetGcpPrices(request);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.NotNull(statusResult.Value);
        var responseType = statusResult.Value.GetType();
        var errorProperty = responseType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Contains("GCP Billing API key", errorProperty.GetValue(statusResult.Value)?.ToString());
    }
}
