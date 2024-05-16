using AlertHawk.Monitoring.Infrastructure.Utils;

namespace AlertHawk.Monitoring.Tests.UtilsTests;

public class StringUtilsTests
{
    [Fact]
    public void ShouldGenerateRandomString()
    {
        // Arrange
        var randomString = StringUtils.RandomStringGenerator();
        
        // Act
        var result = randomString.Length;
        
        // Assert
        Assert.Equal(10, result);
    }
}