using AlertHawk.Metrics.API.Controllers;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Metrics.Tests;

public class ClickHouseControllerTests
{
    private readonly Mock<IClickHouseService> _mockClickHouseService;
    private readonly ClickHouseController _controller;

    public ClickHouseControllerTests()
    {
        _mockClickHouseService = new Mock<IClickHouseService>();
        _controller = new ClickHouseController(_mockClickHouseService.Object);
        
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

    #region GetTableSizes Tests

    [Fact]
    public async Task GetTableSizes_WithValidData_ReturnsOkWithTableSizes()
    {
        // Arrange
        var expectedTableSizes = new List<TableSizeDto>
        {
            new TableSizeDto
            {
                Database = "default",
                Table = "k8s_metrics",
                TotalSize = "1.23 GiB",
                TotalSizeBytes = 1320702443
            },
            new TableSizeDto
            {
                Database = "default",
                Table = "pod_logs",
                TotalSize = "456.78 MiB",
                TotalSizeBytes = 478888888
            },
            new TableSizeDto
            {
                Database = "system",
                Table = "query_log",
                TotalSize = "12.34 MiB",
                TotalSizeBytes = 12949913
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ReturnsAsync(expectedTableSizes);

        // Act
        var result = await _controller.GetTableSizes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tableSizes = Assert.IsAssignableFrom<List<TableSizeDto>>(okResult.Value);
        Assert.Equal(3, tableSizes.Count);
        Assert.Equal("default", tableSizes[0].Database);
        Assert.Equal("k8s_metrics", tableSizes[0].Table);
        Assert.Equal("1.23 GiB", tableSizes[0].TotalSize);
        Assert.Equal(1320702443, tableSizes[0].TotalSizeBytes);
        _mockClickHouseService.Verify(s => s.GetTableSizesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTableSizes_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var expectedTableSizes = new List<TableSizeDto>();

        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ReturnsAsync(expectedTableSizes);

        // Act
        var result = await _controller.GetTableSizes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tableSizes = Assert.IsAssignableFrom<List<TableSizeDto>>(okResult.Value);
        Assert.Empty(tableSizes);
        _mockClickHouseService.Verify(s => s.GetTableSizesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTableSizes_WithSingleTable_ReturnsOkWithSingleItem()
    {
        // Arrange
        var expectedTableSizes = new List<TableSizeDto>
        {
            new TableSizeDto
            {
                Database = "default",
                Table = "k8s_metrics",
                TotalSize = "500.00 MiB",
                TotalSizeBytes = 524288000
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ReturnsAsync(expectedTableSizes);

        // Act
        var result = await _controller.GetTableSizes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tableSizes = Assert.IsAssignableFrom<List<TableSizeDto>>(okResult.Value);
        Assert.Single(tableSizes);
        Assert.Equal("default", tableSizes[0].Database);
        Assert.Equal("k8s_metrics", tableSizes[0].Table);
        _mockClickHouseService.Verify(s => s.GetTableSizesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTableSizes_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionMessage = "Database connection failed";
        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.GetTableSizes();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.NotNull(statusResult.Value);
        var responseType = statusResult.Value.GetType();
        var errorProperty = responseType.GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal(exceptionMessage, errorProperty.GetValue(statusResult.Value)?.ToString());
        _mockClickHouseService.Verify(s => s.GetTableSizesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTableSizes_ServiceThrowsTimeoutException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionMessage = "The operation timed out";
        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ThrowsAsync(new TimeoutException(exceptionMessage));

        // Act
        var result = await _controller.GetTableSizes();

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
    public async Task GetTableSizes_WithMultipleDatabases_ReturnsAllTables()
    {
        // Arrange
        var expectedTableSizes = new List<TableSizeDto>
        {
            new TableSizeDto
            {
                Database = "default",
                Table = "k8s_metrics",
                TotalSize = "1.00 GiB",
                TotalSizeBytes = 1073741824
            },
            new TableSizeDto
            {
                Database = "production",
                Table = "k8s_metrics",
                TotalSize = "2.50 GiB",
                TotalSizeBytes = 2684354560
            },
            new TableSizeDto
            {
                Database = "staging",
                Table = "k8s_metrics",
                TotalSize = "500.00 MiB",
                TotalSizeBytes = 524288000
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ReturnsAsync(expectedTableSizes);

        // Act
        var result = await _controller.GetTableSizes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tableSizes = Assert.IsAssignableFrom<List<TableSizeDto>>(okResult.Value);
        Assert.Equal(3, tableSizes.Count);
        Assert.Contains(tableSizes, t => t.Database == "default");
        Assert.Contains(tableSizes, t => t.Database == "production");
        Assert.Contains(tableSizes, t => t.Database == "staging");
    }

    [Fact]
    public async Task GetTableSizes_WithLargeTableSizes_HandlesCorrectly()
    {
        // Arrange
        var expectedTableSizes = new List<TableSizeDto>
        {
            new TableSizeDto
            {
                Database = "default",
                Table = "large_table",
                TotalSize = "10.00 TiB",
                TotalSizeBytes = 10995116277760
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ReturnsAsync(expectedTableSizes);

        // Act
        var result = await _controller.GetTableSizes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tableSizes = Assert.IsAssignableFrom<List<TableSizeDto>>(okResult.Value);
        Assert.Single(tableSizes);
        Assert.Equal(10995116277760, tableSizes[0].TotalSizeBytes);
        Assert.Equal("10.00 TiB", tableSizes[0].TotalSize);
    }

    [Fact]
    public async Task GetTableSizes_WithZeroSizeTables_ReturnsCorrectly()
    {
        // Arrange
        var expectedTableSizes = new List<TableSizeDto>
        {
            new TableSizeDto
            {
                Database = "default",
                Table = "empty_table",
                TotalSize = "0.00 B",
                TotalSizeBytes = 0
            }
        };

        _mockClickHouseService
            .Setup(s => s.GetTableSizesAsync())
            .ReturnsAsync(expectedTableSizes);

        // Act
        var result = await _controller.GetTableSizes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tableSizes = Assert.IsAssignableFrom<List<TableSizeDto>>(okResult.Value);
        Assert.Single(tableSizes);
        Assert.Equal(0, tableSizes[0].TotalSizeBytes);
        Assert.Equal("0.00 B", tableSizes[0].TotalSize);
    }

    #endregion
}
