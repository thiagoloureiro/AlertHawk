using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Metrics.Tests;

public class ClusterPricesControllerTests
{
    private readonly Mock<IClickHouseService> _mockClickHouseService;
    private readonly Mock<ILogger<ClusterPricesController>> _mockLogger;
    private readonly ClusterPricesController _controller;

    public ClusterPricesControllerTests()
    {
        _mockClickHouseService = new Mock<IClickHouseService>();
        _mockLogger = new Mock<ILogger<ClusterPricesController>>();
        
        _controller = new ClusterPricesController(
            _mockClickHouseService.Object,
            _mockLogger.Object);
        
        // Setup controller context for authorization
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region GetClusterPrices Tests

    [Fact]
    public async Task GetClusterPrices_WithAllFilters_ReturnsOkWithPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                OperatingSystem = "Linux",
                CloudProvider = "Azure",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096,
                MeterName = "D2s v3",
                ProductName = "Virtual Machines D2s v3 - Compute",
                SkuName = "D2s v3",
                ServiceName = "Virtual Machines",
                ArmRegionName = "eastus",
                EffectiveStartDate = DateTime.UtcNow.AddDays(-30)
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "node-1", "eastus", "Standard_D2s_v3", 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPrices("test-cluster", "node-1", "eastus", "Standard_D2s_v3", 1440);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("test-cluster", prices[0].ClusterName);
        Assert.Equal("node-1", prices[0].NodeName);
        Assert.Equal("eastus", prices[0].Region);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", "node-1", "eastus", "Standard_D2s_v3", 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPrices_WithNoFilters_ReturnsOkWithAllPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "cluster-1",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            },
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "cluster-2",
                NodeName = "node-2",
                Region = "westus",
                InstanceType = "Standard_D4s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.192,
                RetailPrice = 0.192
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync(null, null, null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPrices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Equal(2, prices.Count);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync(null, null, null, null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPrices_WithClusterNameFilter_ReturnsFilteredPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "production-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("production-cluster", null, null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPrices("production-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("production-cluster", prices[0].ClusterName);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("production-cluster", null, null, null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPrices_WithCustomMinutes_RespectsTimeWindow()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>();

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync(null, null, null, null, 60))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPrices(null, null, null, null, 60);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync(null, null, null, null, 60), Times.Once);
    }

    [Fact]
    public async Task GetClusterPrices_WithRegionFilter_ReturnsFilteredPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync(null, null, "eastus", null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPrices(null, null, "eastus");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("eastus", prices[0].Region);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync(null, null, "eastus", null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPrices_WithInstanceTypeFilter_ReturnsFilteredPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D4s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.192,
                RetailPrice = 0.192
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync(null, null, null, "Standard_D4s_v3", 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPrices(null, null, null, "Standard_D4s_v3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("Standard_D4s_v3", prices[0].InstanceType);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync(null, null, null, "Standard_D4s_v3", 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPrices_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>();

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("non-existent", null, null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPrices("non-existent");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Empty(prices);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("non-existent", null, null, null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPrices_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync(null, null, null, null, 1440))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetClusterPrices();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.NotNull(statusResult.Value);
        var responseType = statusResult.Value.GetType();
        var errorProperty = responseType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Database connection failed", errorProperty.GetValue(statusResult.Value)?.ToString());
    }

    #endregion

    #region GetClusterPricesByCluster Tests

    [Fact]
    public async Task GetClusterPricesByCluster_WithValidClusterName_ReturnsOkWithPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "production-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("production-cluster", null, null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByCluster("production-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("production-cluster", prices[0].ClusterName);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("production-cluster", null, null, null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPricesByCluster_WithNodeNameFilter_ReturnsFilteredPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByCluster("test-cluster", "node-1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("node-1", prices[0].NodeName);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPricesByCluster_WithRegionAndInstanceTypeFilters_ReturnsFilteredPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "westus",
                InstanceType = "Standard_D4s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.192,
                RetailPrice = 0.192
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", null, "westus", "Standard_D4s_v3", 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByCluster("test-cluster", null, "westus", "Standard_D4s_v3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("westus", prices[0].Region);
        Assert.Equal("Standard_D4s_v3", prices[0].InstanceType);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", null, "westus", "Standard_D4s_v3", 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPricesByCluster_WithCustomMinutes_RespectsTimeWindow()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>();

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", null, null, null, 30))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByCluster("test-cluster", null, null, null, 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", null, null, null, 30), Times.Once);
    }

    [Fact]
    public async Task GetClusterPricesByCluster_WithMultiplePrices_ReturnsAllPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            },
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                ClusterName = "test-cluster",
                NodeName = "node-2",
                Region = "eastus",
                InstanceType = "Standard_D4s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.192,
                RetailPrice = 0.192
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", null, null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByCluster("test-cluster");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Equal(2, prices.Count);
        Assert.Equal("node-1", prices[0].NodeName);
        Assert.Equal("node-2", prices[1].NodeName);
    }

    [Fact]
    public async Task GetClusterPricesByCluster_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionMessage = "Query failed";
        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", null, null, null, 1440))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.GetClusterPricesByCluster("test-cluster");

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.NotNull(statusResult.Value);
        var responseType = statusResult.Value.GetType();
        var errorProperty = responseType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal(exceptionMessage, errorProperty.GetValue(statusResult.Value)?.ToString());
    }

    #endregion

    #region GetClusterPricesByNode Tests

    [Fact]
    public async Task GetClusterPricesByNode_WithValidClusterAndNode_ReturnsOkWithPrices()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                OperatingSystem = "Linux",
                CloudProvider = "Azure",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096,
                MeterName = "D2s v3",
                ProductName = "Virtual Machines D2s v3 - Compute",
                SkuName = "D2s v3",
                ServiceName = "Virtual Machines",
                ArmRegionName = "eastus",
                EffectiveStartDate = DateTime.UtcNow.AddDays(-30)
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByNode("test-cluster", "node-1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Single(prices);
        Assert.Equal("test-cluster", prices[0].ClusterName);
        Assert.Equal("node-1", prices[0].NodeName);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPricesByNode_WithCustomMinutes_RespectsTimeWindow()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>();

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 60))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByNode("test-cluster", "node-1", 60);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 60), Times.Once);
    }

    [Fact]
    public async Task GetClusterPricesByNode_WithMultiplePriceRecords_ReturnsAllRecords()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>
        {
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow.AddHours(-2),
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            },
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow.AddHours(-1),
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            },
            new ClusterPriceDto
            {
                Timestamp = DateTime.UtcNow,
                ClusterName = "test-cluster",
                NodeName = "node-1",
                Region = "eastus",
                InstanceType = "Standard_D2s_v3",
                CurrencyCode = "USD",
                UnitPrice = 0.096,
                RetailPrice = 0.096
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByNode("test-cluster", "node-1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Equal(3, prices.Count);
        Assert.All(prices, p => 
        {
            Assert.Equal("test-cluster", p.ClusterName);
            Assert.Equal("node-1", p.NodeName);
        });
    }

    [Fact]
    public async Task GetClusterPricesByNode_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>();

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "non-existent-node", null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByNode("test-cluster", "non-existent-node");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<List<ClusterPriceDto>>(okResult.Value);
        Assert.Empty(prices);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", "non-existent-node", null, null, 1440), Times.Once);
    }

    [Fact]
    public async Task GetClusterPricesByNode_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionMessage = "Database error";
        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.GetClusterPricesByNode("test-cluster", "node-1");

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
    public async Task GetClusterPricesByNode_WithDefaultMinutes_UsesDefaultValue()
    {
        // Arrange
        var expectedPrices = new List<ClusterPriceDto>();

        _mockClickHouseService
            .Setup(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440))
            .ReturnsAsync(expectedPrices);

        // Act
        var result = await _controller.GetClusterPricesByNode("test-cluster", "node-1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockClickHouseService.Verify(s => s.GetClusterPricesAsync("test-cluster", "node-1", null, null, 1440), Times.Once);
    }

    #endregion
}
