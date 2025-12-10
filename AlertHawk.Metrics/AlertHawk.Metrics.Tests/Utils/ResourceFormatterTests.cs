using AlertHawk.Metrics;
using Xunit;

namespace AlertHawk.Metrics.Tests.Utils;

public class ResourceFormatterTests
{
    #region ParseCpuToCores Tests

    [Fact]
    public void ParseCpuToCores_WithNull_ReturnsZero()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores(null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithEmptyString_ReturnsZero()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithWhitespace_ReturnsZero()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("   ");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithPlainCores_ReturnsValue()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("2.5");

        // Assert
        Assert.Equal(2.5, result);
    }

    [Fact]
    public void ParseCpuToCores_WithMillicoresLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("500m");

        // Assert
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void ParseCpuToCores_WithMillicoresUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("1000M");

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithMillicoresMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("250m");

        // Assert
        Assert.Equal(0.25, result);
    }

    [Fact]
    public void ParseCpuToCores_WithNanocoresLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("1000000000n");

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithNanocoresUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("500000000N");

        // Assert
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void ParseCpuToCores_WithNanocoresMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("250000000n");

        // Assert
        Assert.Equal(0.25, result);
    }

    [Fact]
    public void ParseCpuToCores_WithWhitespaceAroundValue_TrimsAndConverts()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("  500m  ");

        // Assert
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void ParseCpuToCores_WithInvalidFormat_ReturnsZero()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("invalid");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithZero_ReturnsZero()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("0");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithZeroMillicores_ReturnsZero()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("0m");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseCpuToCores_WithDecimalMillicores_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("125.5m");

        // Assert
        Assert.Equal(0.1255, result);
    }

    [Fact]
    public void ParseCpuToCores_WithDecimalNanocores_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.ParseCpuToCores("125000000.5n");

        // Assert
        Assert.Equal(0.1250000005, result, 10);
    }

    #endregion

    #region CalculateCpuPercentage Tests

    [Fact]
    public void CalculateCpuPercentage_WithNullUsage_ReturnsNull()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage(null, "1000m");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateCpuPercentage_WithNullLimit_ReturnsNull()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("500m", null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateCpuPercentage_WithEmptyUsage_ReturnsNull()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage(string.Empty, "1000m");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateCpuPercentage_WithEmptyLimit_ReturnsNull()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("500m", string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateCpuPercentage_WithZeroLimit_ReturnsNull()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("500m", "0");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateCpuPercentage_WithNegativeLimit_ReturnsNull()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("500m", "-1000m");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateCpuPercentage_WithFiftyPercentUsage_ReturnsFifty()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("500m", "1000m");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50.0, result.Value);
    }

    [Fact]
    public void CalculateCpuPercentage_WithHundredPercentUsage_ReturnsHundred()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("1000m", "1000m");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100.0, result.Value);
    }

    [Fact]
    public void CalculateCpuPercentage_WithTwentyFivePercentUsage_ReturnsTwentyFive()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("250m", "1000m");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25.0, result.Value);
    }

    [Fact]
    public void CalculateCpuPercentage_WithPlainCores_CalculatesCorrectly()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("1.5", "2.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(75.0, result.Value);
    }

    [Fact]
    public void CalculateCpuPercentage_WithNanocores_CalculatesCorrectly()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("500000000n", "1000000000n");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50.0, result.Value);
    }

    [Fact]
    public void CalculateCpuPercentage_WithMixedUnits_CalculatesCorrectly()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("500m", "2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25.0, result.Value);
    }

    [Fact]
    public void CalculateCpuPercentage_WithOverLimit_ReturnsOverHundred()
    {
        // Act
        var result = ResourceFormatter.CalculateCpuPercentage("2000m", "1000m");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200.0, result.Value);
    }

    #endregion

    #region FormatCpu Tests

    [Fact]
    public void FormatCpu_WithNull_ReturnsN_A()
    {
        // Act
        var result = ResourceFormatter.FormatCpu(null);

        // Assert
        Assert.Equal("N/A", result);
    }

    [Fact]
    public void FormatCpu_WithEmptyString_ReturnsN_A()
    {
        // Act
        var result = ResourceFormatter.FormatCpu(string.Empty);

        // Assert
        Assert.Equal("N/A", result);
    }

    [Fact]
    public void FormatCpu_WithWhitespace_ReturnsN_A()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("   ");

        // Assert
        Assert.Equal("N/A", result);
    }

    [Fact]
    public void FormatCpu_WithPlainCoresGreaterThanOne_FormatsAsCores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("2.5");

        // Assert
        Assert.Equal("2.50 cores", result);
    }

    [Fact]
    public void FormatCpu_WithPlainCoresLessThanOneButGreaterThanPointZeroZeroOne_FormatsAsMillicores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("0.5");

        // Assert
        Assert.Equal("500.00 m", result);
    }

    [Fact]
    public void FormatCpu_WithPlainCoresLessThanPointZeroZeroOne_FormatsAsNanocores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("0.0005");

        // Assert
        Assert.Equal("500 n", result);
    }

    [Fact]
    public void FormatCpu_WithMillicoresLessThanThousand_FormatsAsMillicores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("500m");

        // Assert
        Assert.Equal("500.00 m", result);
    }

    [Fact]
    public void FormatCpu_WithMillicoresGreaterThanOrEqualThousand_FormatsAsCores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("2000m");

        // Assert
        Assert.Equal("2.00 cores", result);
    }

    [Fact]
    public void FormatCpu_WithNanocores_ConvertsToMillicores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("500000n");

        // Assert
        Assert.Equal("0.500 m", result);
    }

    [Fact]
    public void FormatCpu_WithInvalidFormat_ReturnsOriginal()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("invalid");

        // Assert
        Assert.Equal("invalid", result);
    }

    [Fact]
    public void FormatCpu_WithCpuLimit_IncludesPercentage()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("500m", "1000m");

        // Assert
        Assert.Contains("50.0%", result);
    }

    [Fact]
    public void FormatCpu_WithCpuLimitAndPlainCores_IncludesPercentage()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("1.5", "2.0");

        // Assert
        Assert.Contains("75.0%", result);
    }

    [Fact]
    public void FormatCpu_WithNullLimit_DoesNotIncludePercentage()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("500m", null);

        // Assert
        Assert.DoesNotContain("%", result);
    }

    [Fact]
    public void FormatCpu_WithEmptyLimit_DoesNotIncludePercentage()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("500m", string.Empty);

        // Assert
        Assert.DoesNotContain("%", result);
    }

    [Fact]
    public void FormatCpu_WithZeroLimit_DoesNotIncludePercentage()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("500m", "0");

        // Assert
        Assert.DoesNotContain("%", result);
    }

    [Fact]
    public void FormatCpu_WithWhitespaceAroundValue_TrimsAndFormats()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("  500m  ");

        // Assert
        Assert.Equal("500.00 m", result);
    }

    [Fact]
    public void FormatCpu_WithOneCore_FormatsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("1");

        // Assert
        Assert.Equal("1.00 cores", result);
    }

    [Fact]
    public void FormatCpu_WithExactlyPointZeroZeroOne_FormatsAsMillicores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("0.001");

        // Assert
        Assert.Equal("1.00 m", result);
    }

    [Fact]
    public void FormatCpu_WithExactlyThousandMillicores_FormatsAsCores()
    {
        // Act
        var result = ResourceFormatter.FormatCpu("1000m");

        // Assert
        Assert.Equal("1.00 cores", result);
    }

    #endregion

    #region FormatMemory Tests

    [Fact]
    public void FormatMemory_WithNull_ReturnsN_A()
    {
        // Act
        var result = ResourceFormatter.FormatMemory(null);

        // Assert
        Assert.Equal("N/A", result);
    }

    [Fact]
    public void FormatMemory_WithEmptyString_ReturnsN_A()
    {
        // Act
        var result = ResourceFormatter.FormatMemory(string.Empty);

        // Assert
        Assert.Equal("N/A", result);
    }

    [Fact]
    public void FormatMemory_WithWhitespace_ReturnsN_A()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("   ");

        // Assert
        Assert.Equal("N/A", result);
    }

    [Fact]
    public void FormatMemory_WithPlainBytes_FormatsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1024");

        // Assert
        Assert.Equal("1.00 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithKibibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1024ki");

        // Assert
        Assert.Equal("1.00 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithKibibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1024KI");

        // Assert
        Assert.Equal("1.00 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithKibibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("512Ki");

        // Assert
        Assert.Equal("512.00 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithMebibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1024mi");

        // Assert
        Assert.Equal("1.00 GiB", result);
    }

    [Fact]
    public void FormatMemory_WithMebibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("512MI");

        // Assert
        Assert.Equal("512.00 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithMebibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("256Mi");

        // Assert
        Assert.Equal("256.00 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithGibibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1024gi");

        // Assert
        Assert.Equal("1.00 TiB", result);
    }

    [Fact]
    public void FormatMemory_WithGibibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("8GI");

        // Assert
        Assert.Equal("8.00 GiB", result);
    }

    [Fact]
    public void FormatMemory_WithGibibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("4Gi");

        // Assert
        Assert.Equal("4.00 GiB", result);
    }

    [Fact]
    public void FormatMemory_WithTebibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1ti");

        // Assert
        Assert.Equal("1.00 TiB", result);
    }

    [Fact]
    public void FormatMemory_WithTebibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("2TI");

        // Assert
        Assert.Equal("2.00 TiB", result);
    }

    [Fact]
    public void FormatMemory_WithTebibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1Ti");

        // Assert
        Assert.Equal("1.00 TiB", result);
    }

    [Fact]
    public void FormatMemory_WithKLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1000k");

        // Assert
        Assert.Equal("976.56 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithKUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1000K");

        // Assert
        Assert.Equal("976.56 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithMLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1000m");

        // Assert
        Assert.Equal("953.67 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithMUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1000M");

        // Assert
        Assert.Equal("953.67 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithGLowercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1g");

        // Assert
        Assert.Equal("953.67 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithGUppercase_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1G");

        // Assert
        Assert.Equal("953.67 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithLessThanKibibyte_FormatsAsBytes()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("512");

        // Assert
        Assert.Equal("512 B", result);
    }

    [Fact]
    public void FormatMemory_WithExactlyOneKibibyte_FormatsAsKibibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1024");

        // Assert
        Assert.Equal("1.00 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithInvalidFormat_ReturnsOriginal()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("invalid");

        // Assert
        Assert.Equal("invalid", result);
    }

    [Fact]
    public void FormatMemory_WithWhitespaceAroundValue_TrimsAndFormats()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("  512Mi  ");

        // Assert
        Assert.Equal("512.00 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithZero_FormatsAsZeroBytes()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("0");

        // Assert
        Assert.Equal("0 B", result);
    }

    [Fact]
    public void FormatMemory_WithVeryLargeTebibytes_FormatsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("10Ti");

        // Assert
        Assert.Equal("10.00 TiB", result);
    }

    [Fact]
    public void FormatMemory_WithDecimalKibibytes_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1.5Ki");

        // Assert
        Assert.Equal("1.50 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithDecimalMebibytes_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1.5Mi");

        // Assert
        Assert.Equal("1.50 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithDecimalGibibytes_ConvertsCorrectly()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("2.5Gi");

        // Assert
        Assert.Equal("2.50 GiB", result);
    }

    #endregion

    #region FormatBinaryBytes Tests (indirectly tested through FormatMemory)

    [Fact]
    public void FormatMemory_WithBytesLessThanKibibyte_FormatsAsBytes()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("100");

        // Assert
        Assert.Equal("100 B", result);
    }

    [Fact]
    public void FormatMemory_WithBytesExactlyKibibyte_FormatsAsKibibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1024");

        // Assert
        Assert.Equal("1.00 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithBytesExactlyMebibyte_FormatsAsMebibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1048576");

        // Assert
        Assert.Equal("1.00 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithBytesExactlyGibibyte_FormatsAsGibibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1073741824");

        // Assert
        Assert.Equal("1.00 GiB", result);
    }

    [Fact]
    public void FormatMemory_WithBytesExactlyTebibyte_FormatsAsTebibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("1099511627776");

        // Assert
        Assert.Equal("1.00 TiB", result);
    }

    [Fact]
    public void FormatMemory_WithBytesBetweenKibibyteAndMebibyte_FormatsAsKibibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("2048");

        // Assert
        Assert.Equal("2.00 KiB", result);
    }

    [Fact]
    public void FormatMemory_WithBytesBetweenMebibyteAndGibibyte_FormatsAsMebibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("2097152");

        // Assert
        Assert.Equal("2.00 MiB", result);
    }

    [Fact]
    public void FormatMemory_WithBytesBetweenGibibyteAndTebibyte_FormatsAsGibibyte()
    {
        // Act
        var result = ResourceFormatter.FormatMemory("2147483648");

        // Assert
        Assert.Equal("2.00 GiB", result);
    }

    #endregion
}
