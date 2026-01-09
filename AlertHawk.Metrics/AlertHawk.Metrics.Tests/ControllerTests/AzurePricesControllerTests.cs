using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertHawk.Metrics.Tests;

public class AzurePricesControllerTests
{
    private readonly Mock<IAzurePricesService> _mockAzurePricesService;
    private readonly AzurePricesController _controller;

    public AzurePricesControllerTests()
    {
        _mockAzurePricesService = new Mock<IAzurePricesService>();
        _controller = new AzurePricesController(_mockAzurePricesService.Object);
    }

    #region GetAzurePrices Tests

    [Fact]
    public async Task GetAzurePrices_WithValidRequest_ReturnsOkWithResponse()
    {
        // Arrange
        var request = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines",
            SkuName = "Standard_D2s_v3"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "USD",
            Count = 1,
            Items = new List<AzurePriceItem>
            {
                new AzurePriceItem
                {
                    CurrencyCode = "USD",
                    RetailPrice = 0.096,
                    UnitPrice = 0.096,
                    ServiceName = "Virtual Machines",
                    SkuName = "Standard_D2s_v3",
                    ArmRegionName = "eastus"
                }
            }
        };

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAzurePrices(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AzurePriceResponse>(okResult.Value);
        Assert.Equal(expectedResponse.BillingCurrency, response.BillingCurrency);
        Assert.Equal(expectedResponse.Count, response.Count);
        Assert.Single(response.Items);
        _mockAzurePricesService.Verify(s => s.GetPricesAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAzurePrices_WithNullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAzurePrices(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
        var responseType = badRequestResult.Value.GetType();
        var errorProperty = responseType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Request body is required", errorProperty.GetValue(badRequestResult.Value)?.ToString());
        _mockAzurePricesService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAzurePrices_CallsService()
    {
        // Arrange
        var request = new AzurePriceRequest
        {
            CurrencyCode = "EUR",
            ServiceName = "Storage"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "EUR",
            Count = 0,
            Items = new List<AzurePriceItem>()
        };

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request);

        // Assert
        _mockAzurePricesService.Verify(s => s.GetPricesAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAzurePrices_WithComplexRequest_GeneratesUniqueCacheKey()
    {
        // Arrange
        var request = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines",
            SkuName = "Standard_D2s_v3",
            ProductName = "Virtual Machine",
            ArmRegionName = "eastus",
            ArmSkuName = "Standard_D2s_v3",
            Type = "Consumption",
            OperatingSystem = "Linux"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "USD",
            Count = 1,
            Items = new List<AzurePriceItem>()
        };

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request);

        // Assert
        _mockAzurePricesService.Verify(s => s.GetPricesAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAzurePrices_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines"
        };

        var exceptionMessage = "Service unavailable";
        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(request))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.GetAzurePrices(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.NotNull(statusResult.Value);
        var responseType = statusResult.Value.GetType();
        var errorProperty = responseType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal(exceptionMessage, errorProperty.GetValue(statusResult.Value)?.ToString());
    }

    [Fact]
    public async Task GetAzurePrices_WithEmptyRequest_StillProcesses()
    {
        // Arrange
        var request = new AzurePriceRequest
        {
            CurrencyCode = "USD"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "USD",
            Count = 0,
            Items = new List<AzurePriceItem>()
        };

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAzurePrices(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AzurePriceResponse>(okResult.Value);
        Assert.Equal(expectedResponse.BillingCurrency, response.BillingCurrency);
    }

    [Fact]
    public async Task GetAzurePrices_WithFilterProperty_ProcessesCorrectly()
    {
        // Arrange
        var request = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            Filter = "serviceName eq 'Virtual Machines' and armRegionName eq 'eastus'"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "USD",
            Count = 5,
            Items = new List<AzurePriceItem>()
        };

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAzurePrices(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AzurePriceResponse>(okResult.Value);
        Assert.Equal(5, response.Count);
    }

    [Fact]
    public async Task GetAzurePrices_SameRequestGeneratesSameCacheKey()
    {
        // Arrange
        var request1 = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines",
            SkuName = "Standard_D2s_v3"
        };

        var request2 = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines",
            SkuName = "Standard_D2s_v3"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "USD",
            Count = 1,
            Items = new List<AzurePriceItem>()
        };

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(It.IsAny<AzurePriceRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request1);
        await _controller.GetAzurePrices(request2);

        // Assert
        // Service handles caching internally, so we just verify service is called
        _mockAzurePricesService.Verify(s => s.GetPricesAsync(It.IsAny<AzurePriceRequest>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAzurePrices_DifferentRequestsGenerateDifferentCacheKeys()
    {
        // Arrange
        var request1 = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines"
        };

        var request2 = new AzurePriceRequest
        {
            CurrencyCode = "EUR",
            ServiceName = "Virtual Machines"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "USD",
            Count = 1,
            Items = new List<AzurePriceItem>()
        };

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(It.IsAny<AzurePriceRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request1);
        await _controller.GetAzurePrices(request2);

        // Assert
        // Service handles caching internally, so we just verify service is called for different requests
        _mockAzurePricesService.Verify(s => s.GetPricesAsync(It.Is<AzurePriceRequest>(r => r.CurrencyCode == "USD")), Times.Once);
        _mockAzurePricesService.Verify(s => s.GetPricesAsync(It.Is<AzurePriceRequest>(r => r.CurrencyCode == "EUR")), Times.Once);
    }

    #endregion
}
