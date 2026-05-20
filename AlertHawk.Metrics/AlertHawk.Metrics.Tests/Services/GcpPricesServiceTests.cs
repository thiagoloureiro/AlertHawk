using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;

namespace AlertHawk.Metrics.Tests;

public class GcpPricesServiceTests
{
    [Theory]
    [InlineData("n1-standard-2", "N1")]
    [InlineData("e2-medium", "E2")]
    [InlineData("c3-highcpu-4", "C3")]
    public void ResolveResourceGroup_FromMachineType_ReturnsExpectedGroup(string machineType, string expected)
    {
        var request = new GcpPriceRequest { MachineType = machineType };

        var result = GcpPricesService.ResolveResourceGroup(request);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveResourceGroup_WithExplicitResourceGroup_ReturnsExplicitValue()
    {
        var request = new GcpPriceRequest
        {
            MachineType = "n1-standard-2",
            ResourceGroup = "Custom"
        };

        var result = GcpPricesService.ResolveResourceGroup(request);

        Assert.Equal("Custom", result);
    }

}
