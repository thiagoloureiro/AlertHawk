using AlertHawk.Authentication.Infrastructure.Utils;
using Moq;

namespace AlertHawk.Authentication.Tests.UtilsTests;

public class UtilsTests
{
    [Fact]
    public void TokenUtilsTests_GetJwtTokenFromRequest_ReturnsNull()
    {
        // Arrange
        string? request = null;
        
        // Act
        var result = TokenUtils.GetJwtToken(request);

        // Assert
        Assert.Equal(request, result);
    }
    
    [Fact]
    public void TokenUtilsTests_GetJwtTokenFromRequest_ReturnsToken()
    {
        // Arrange
        string token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c\n";
        string[] tokenParts = token.Split(' ');
        
        // Act
        var result = TokenUtils.GetJwtToken(token);

        // Assert
        Assert.Equal(tokenParts[1], result);
    }
}