using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Utils;
using AlertHawk.Notification.Infrastructure.Utils;

namespace AlertHawk.Notification.Tests.UtilsTests
{
    public class UtilsTests
    {
        [Fact]
        public void EncryptAndDecrypt_ReturnsOriginalString()
        {
            // Arrange
            var keys = Utils.GenerateAesKeyAndIv();

            string originalString = "encrypted-thing";
            Environment.SetEnvironmentVariable("AesKey", keys.Item1);
            Environment.SetEnvironmentVariable("AesIV", keys.Item2);

            // Act
            string? encrypted = AesEncryption.EncryptString(originalString);
            string? decryptedString = AesEncryption.DecryptString(encrypted);

            // Assert
            Assert.Equal(originalString, decryptedString);
        }
        
        [Fact]
        public void EncryptAndDecrypt_ReturnsArgumentNullException()
        {
            // Arrange
            var keys = Utils.GenerateAesKeyAndIv();

            string originalString = "";
            Environment.SetEnvironmentVariable("AesKey", keys.Item1);
            Environment.SetEnvironmentVariable("AesIV", keys.Item2);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => AesEncryption.EncryptString(originalString));
        }
        
        [Fact]
        public void EncryptAndDecrypt_ReturnsArgumentNullExceptionOnDecrypt()
        {
            // Arrange
            var keys = Utils.GenerateAesKeyAndIv();

            string originalString = "test123";
            Environment.SetEnvironmentVariable("AesKey", keys.Item1);
            Environment.SetEnvironmentVariable("AesIV", keys.Item2);

            // Act 
            string? encrypted = AesEncryption.EncryptString(originalString);
            
            // Assert
            Assert.Throws<ArgumentNullException>(() => AesEncryption.DecryptString(null));
        }

        [Fact]
        public void GetJwtToken_WithValidBearerToken_ReturnsJwt()
        {
            // Arrange
            string expectedJwt = "jwt.token.here";
            string bearerToken = $"Bearer {expectedJwt}";

            // Act
            string? result = TokenUtils.GetJwtToken(bearerToken);

            // Assert
            Assert.Equal(expectedJwt, result);
        }

        [Fact]
        public void GetJwtToken_WithInvalidToken_NotStartingWithBearer_ReturnsNull()
        {
            // Arrange
            string token = "Basic jwt.token.here";

            // Act
            string? result = TokenUtils.GetJwtToken(token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetJwtToken_WithInvalidTokenFormat_ReturnsNull()
        {
            // Arrange
            string token = "Bearer";

            // Act
            string? result = TokenUtils.GetJwtToken(token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetJwtToken_WithNullInput_ReturnsNull()
        {
            // Act
            string? result = TokenUtils.GetJwtToken(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetJwtToken_WithEmptyString_ReturnsNull()
        {
            // Act
            string? result = TokenUtils.GetJwtToken("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetJwtToken_WithWhitespaceOnly_ReturnsNull()
        {
            // Act
            string? result = TokenUtils.GetJwtToken("   ");

            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void ConvertJsonToTuple_ShouldHandleNullHeadersJson()
        {
            // Arrange
            var webHook = new NotificationWebHook { HeadersJson = null };

            // Act
            JsonUtils.ConvertJsonToTuple(webHook);

            // Assert
            Assert.Null(webHook.Headers);
        }

        [Fact]
        public void ConvertJsonToTuple_ShouldConvertValidJsonToTuples()
        {
            // Arrange
            var webHook = new NotificationWebHook
            {
                HeadersJson = "{ \"Header1\": \"Value1\", \"Header2\": \"Value2\" }"
            };

            // Act
            JsonUtils.ConvertJsonToTuple(webHook);

            // Assert
            Assert.NotNull(webHook.Headers);
            Assert.Equal(2, webHook.Headers.Count);
            Assert.Contains(Tuple.Create("Header1", "Value1"), webHook.Headers);
            Assert.Contains(Tuple.Create("Header2", "Value2"), webHook.Headers);
        }
    }
}