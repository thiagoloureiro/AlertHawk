using AlertHawk.Monitoring.Infrastructure.Utils;

namespace AlertHawk.Monitoring.Tests.UtilsTests;

public class TokenUtilsTests
{
    [Fact]
    public void GetJwtToken_ReturnsJwtToken_WhenValidTokenIsProvided()
    {
        // Arrange
        string validToken = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        var result = TokenUtils.GetJwtToken(validToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", result);
    }

    [Fact]
    public void GetJwtToken_ReturnsNull_WhenInvalidTokenIsProvided()
    {
        // Arrange
        string invalidToken = "InvalidToken";

        // Act
        var result = TokenUtils.GetJwtToken(invalidToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetJwtToken_ReturnsNull_WhenTokenIsNullOrEmpty()
    {
        // Arrange
        string nullOrEmptyToken = null;

        // Act
        var result = TokenUtils.GetJwtToken(nullOrEmptyToken);

        // Assert
        Assert.Null(result);
    }
}