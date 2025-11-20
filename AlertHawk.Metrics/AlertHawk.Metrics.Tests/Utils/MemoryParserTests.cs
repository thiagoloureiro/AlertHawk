using AlertHawk.Metrics.Utils;
using Xunit;

namespace AlertHawk.Metrics.Tests.Utils;

public class MemoryParserTests
{
    [Fact]
    public void ParseToBytes_WithNull_ReturnsZero()
    {
        // Act
        var result = MemoryParser.ParseToBytes(null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseToBytes_WithEmptyString_ReturnsZero()
    {
        // Act
        var result = MemoryParser.ParseToBytes(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseToBytes_WithWhitespace_ReturnsZero()
    {
        // Act
        var result = MemoryParser.ParseToBytes("   ");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseToBytes_WithPlainBytes_ReturnsValue()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1024");

        // Assert
        Assert.Equal(1024, result);
    }

    [Fact]
    public void ParseToBytes_WithPlainBytesDecimal_ReturnsValue()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1024.5");

        // Assert
        Assert.Equal(1024.5, result);
    }

    [Fact]
    public void ParseToBytes_WithZero_ReturnsZero()
    {
        // Act
        var result = MemoryParser.ParseToBytes("0");

        // Assert
        Assert.Equal(0, result);
    }

    #region Kibibytes (Ki) Tests

    [Fact]
    public void ParseToBytes_WithKibibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1024ki");

        // Assert
        Assert.Equal(1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithKibibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1024KI");

        // Assert
        Assert.Equal(1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithKibibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1024Ki");

        // Assert
        Assert.Equal(1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithKibibytesDecimal_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1.5Ki");

        // Assert
        Assert.Equal(1.5 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithOneKibibyte_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1Ki");

        // Assert
        Assert.Equal(1024, result);
    }

    #endregion

    #region Mebibytes (Mi) Tests

    [Fact]
    public void ParseToBytes_WithMebibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("128mi");

        // Assert
        Assert.Equal(128 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithMebibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("128MI");

        // Assert
        Assert.Equal(128 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithMebibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("128Mi");

        // Assert
        Assert.Equal(128 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithMebibytesDecimal_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1.5Mi");

        // Assert
        Assert.Equal(1.5 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithOneMebibyte_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1Mi");

        // Assert
        Assert.Equal(1024 * 1024, result);
    }

    #endregion

    #region Gibibytes (Gi) Tests

    [Fact]
    public void ParseToBytes_WithGibibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("8gi");

        // Assert
        Assert.Equal(8L * 1024L * 1024L * 1024L, result);
    }

    [Fact]
    public void ParseToBytes_WithGibibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("8GI");

        // Assert
        Assert.Equal(8L * 1024L * 1024L * 1024L, result);
    }

    [Fact]
    public void ParseToBytes_WithGibibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("8Gi");

        // Assert
        Assert.Equal(8L * 1024L * 1024L * 1024L, result);
    }

    [Fact]
    public void ParseToBytes_WithGibibytesDecimal_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("2.5Gi");

        // Assert
        Assert.Equal(2.5 * 1024 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithOneGibibyte_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1Gi");

        // Assert
        Assert.Equal(1024 * 1024 * 1024, result);
    }

    #endregion

    #region Tebibytes (Ti) Tests

    [Fact]
    public void ParseToBytes_WithTebibytesLowercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1ti");

        // Assert
        Assert.Equal(1L * 1024L * 1024L * 1024L * 1024L, result);
    }

    [Fact]
    public void ParseToBytes_WithTebibytesUppercase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1TI");

        // Assert
        Assert.Equal(1L * 1024L * 1024L * 1024L * 1024L, result);
    }

    [Fact]
    public void ParseToBytes_WithTebibytesMixedCase_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1Ti");

        // Assert
        Assert.Equal(1L * 1024L * 1024L * 1024L * 1024L, result);
    }

    [Fact]
    public void ParseToBytes_WithTebibytesDecimal_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("0.5Ti");

        // Assert
        Assert.Equal(0.5 * 1024L * 1024 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithMultipleTebibytes_ConvertsCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("2Ti");

        // Assert
        Assert.Equal(2L * 1024 * 1024 * 1024 * 1024, result);
    }

    #endregion

    #region Edge Cases and Invalid Input Tests

    [Fact]
    public void ParseToBytes_WithWhitespaceAroundValue_TrimsAndConverts()
    {
        // Act
        var result = MemoryParser.ParseToBytes("  128Mi  ");

        // Assert
        Assert.Equal(128 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithInvalidFormat_ReturnsZero()
    {
        // Act
        var result = MemoryParser.ParseToBytes("invalid");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseToBytes_WithInvalidUnit_ReturnsZero()
    {
        // Act
        var result = MemoryParser.ParseToBytes("128Mb");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseToBytes_WithOnlyUnit_ReturnsZero()
    {
        // Act
        var result = MemoryParser.ParseToBytes("Mi");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseToBytes_WithNegativeValue_ReturnsNegative()
    {
        // Act
        var result = MemoryParser.ParseToBytes("-128Mi");

        // Assert
        Assert.Equal(-128 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithVeryLargeValue_HandlesCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("1000Gi");

        // Assert
        Assert.Equal(1000.0 * 1024 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithVerySmallDecimal_HandlesCorrectly()
    {
        // Act
        var result = MemoryParser.ParseToBytes("0.001Mi");

        // Assert
        Assert.Equal(0.001 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseToBytes_WithScientificNotation_ParsesAsPlainBytes()
    {
        // Act - Scientific notation is parsed as a plain number
        var result = MemoryParser.ParseToBytes("1e3");

        // Assert - double.TryParse handles scientific notation, so it returns 1000
        Assert.Equal(1000, result);
    }

    [Fact]
    public void ParseToBytes_WithMultipleUnits_ReturnsZero()
    {
        // Act - Only the last unit should be recognized, but this is invalid
        var result = MemoryParser.ParseToBytes("128MiGi");

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Real-World Examples

    [Fact]
    public void ParseToBytes_WithCommonKubernetesMemoryValues_ConvertsCorrectly()
    {
        // Act & Assert - Common Kubernetes memory values
        Assert.Equal(128L * 1024L, MemoryParser.ParseToBytes("128Ki"));
        Assert.Equal(256L * 1024L * 1024L, MemoryParser.ParseToBytes("256Mi"));
        Assert.Equal(4L * 1024L * 1024L * 1024L, MemoryParser.ParseToBytes("4Gi"));
        Assert.Equal(16L * 1024L * 1024L * 1024L, MemoryParser.ParseToBytes("16Gi"));
    }

    [Fact]
    public void ParseToBytes_WithExactByteValues_ReturnsCorrectly()
    {
        // Act & Assert
        Assert.Equal(1073741824L, MemoryParser.ParseToBytes("1073741824")); // 1Gi in bytes
        Assert.Equal(1048576L, MemoryParser.ParseToBytes("1048576")); // 1Mi in bytes
        Assert.Equal(1024L, MemoryParser.ParseToBytes("1024")); // 1Ki in bytes
    }

    #endregion
}

