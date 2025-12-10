using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using EasyMemoryCache;
using EasyMemoryCache.Configuration;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertHawk.Metrics.Tests;

public class AzurePricesControllerTests
{
    private readonly Mock<IAzurePricesService> _mockAzurePricesService;
    private readonly Mock<ICaching> _mockCaching;
    private readonly AzurePricesController _controller;

    public AzurePricesControllerTests()
    {
        _mockAzurePricesService = new Mock<IAzurePricesService>();
        _mockCaching = new Mock<ICaching>();
        _controller = new AzurePricesController(_mockAzurePricesService.Object, _mockCaching.Object);
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

        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAzurePrices(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AzurePriceResponse>(okResult.Value);
        Assert.Equal(expectedResponse.BillingCurrency, response.BillingCurrency);
        Assert.Equal(expectedResponse.Count, response.Count);
        Assert.Single(response.Items);
        _mockCaching.Verify(c => c.GetOrSetObjectFromCacheAsync(
            It.IsAny<string>(),
            It.Is<int>(t => t == 60),
            It.IsAny<Func<Task<AzurePriceResponse>>>(),
            It.IsAny<bool>(),
            It.IsAny<CacheTimeInterval>()), Times.Once);
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
        _mockCaching.VerifyNoOtherCalls();
        _mockAzurePricesService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAzurePrices_UsesCachingWithCorrectKey()
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

        string? capturedCacheKey = null;
        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
            .Callback<string, int, Func<Task<AzurePriceResponse>>, bool, CacheTimeInterval>((key, time, func, useSlidingExpiration, cacheTimeInterval) =>
            {
                capturedCacheKey = key;
            })
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request);

        // Assert
        Assert.NotNull(capturedCacheKey);
        Assert.StartsWith("azure_prices_", capturedCacheKey);
        _mockCaching.Verify(c => c.GetOrSetObjectFromCacheAsync(
            It.IsAny<string>(),
            It.Is<int>(t => t == 60),
            It.IsAny<Func<Task<AzurePriceResponse>>>(),
            It.IsAny<bool>(),
            It.IsAny<CacheTimeInterval>()), Times.Once);
    }

    [Fact]
    public async Task GetAzurePrices_CallsServiceWhenCacheMiss()
    {
        // Arrange
        var request = new AzurePriceRequest
        {
            CurrencyCode = "USD",
            ServiceName = "Virtual Machines"
        };

        var expectedResponse = new AzurePriceResponse
        {
            BillingCurrency = "USD",
            Count = 1,
            Items = new List<AzurePriceItem>()
        };

        Func<Task<AzurePriceResponse>>? capturedFactory = null;
        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
            .Callback<string, int, Func<Task<AzurePriceResponse>>, bool, CacheTimeInterval>((key, time, func, useSlidingExpiration, cacheTimeInterval) =>
            {
                capturedFactory = func;
            })
            .ReturnsAsync(expectedResponse);

        _mockAzurePricesService
            .Setup(s => s.GetPricesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request);

        // Assert
        Assert.NotNull(capturedFactory);
        // Verify the factory function calls the service
        var result = await capturedFactory();
        Assert.Equal(expectedResponse, result);
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

        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request);

        // Assert
        _mockCaching.Verify(c => c.GetOrSetObjectFromCacheAsync(
            It.Is<string>(k => k.StartsWith("azure_prices_")),
            It.Is<int>(t => t == 60),
            It.IsAny<Func<Task<AzurePriceResponse>>>(),
            It.IsAny<bool>(),
            It.IsAny<CacheTimeInterval>()), Times.Once);
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
        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
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

        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
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

        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
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

        var cacheKeys = new List<string>();
        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
            .Callback<string, int, Func<Task<AzurePriceResponse>>, bool, CacheTimeInterval>((key, time, func, useSlidingExpiration, cacheTimeInterval) =>
            {
                cacheKeys.Add(key);
            })
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request1);
        await _controller.GetAzurePrices(request2);

        // Assert
        Assert.Equal(2, cacheKeys.Count);
        Assert.Equal(cacheKeys[0], cacheKeys[1]); // Same request should generate same cache key
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

        var cacheKeys = new List<string>();
        _mockCaching
            .Setup(c => c.GetOrSetObjectFromCacheAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Func<Task<AzurePriceResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CacheTimeInterval>()))
            .Callback<string, int, Func<Task<AzurePriceResponse>>, bool, CacheTimeInterval>((key, time, func, useSlidingExpiration, cacheTimeInterval) =>
            {
                cacheKeys.Add(key);
            })
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetAzurePrices(request1);
        await _controller.GetAzurePrices(request2);

        // Assert
        Assert.Equal(2, cacheKeys.Count);
        Assert.NotEqual(cacheKeys[0], cacheKeys[1]); // Different requests should generate different cache keys
    }

    #endregion
}
