using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Controllers;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Tests.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AlertHawk.Authentication.Tests.ControllerTests;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthController _controller;
    private readonly Mock<IUsersMonitorGroupService> _mockUsersGroupService;
    private readonly Mock<IGetOrCreateUserService> _mockGetOrCreateUserService;
    private readonly Mock<IMobileAuthCodeService> _mockMobileAuthCodeService;

    public AuthControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockUsersGroupService = new Mock<IUsersMonitorGroupService>();
        _mockGetOrCreateUserService = new Mock<IGetOrCreateUserService>();
        _mockMobileAuthCodeService = new Mock<IMobileAuthCodeService>();
        _controller = new AuthController(_mockUserService.Object, _mockJwtTokenService.Object, _mockConfiguration.Object,
            _mockUsersGroupService.Object, _mockGetOrCreateUserService.Object, _mockMobileAuthCodeService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task PostUserAuth_ValidCredentials_ReturnsOkResultWithToken()
    {
        // Arrange
        var userAuth = new UsersBuilder().WithUserAuth();
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        var token = "test_token";
        _mockUserService.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(user);
        _mockJwtTokenService.Setup(x => x.GenerateToken(It.IsAny<UserDto>())).Returns(token);

        // Act
        var result = await _controller.PostUserAuth(userAuth);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedToken =
            Assert.IsType<string>(okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value));
        Assert.Equal(token, returnedToken);
    }

    [Fact]
    public async Task PostUserAuth_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var userAuth = new UsersBuilder().WithUserAuth();
        _mockUserService.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(It.IsAny<UserDto>());

        // Act
        var result = await _controller.PostUserAuth(userAuth);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);

        Assert.Equal("Invalid credentials.", message.Content);
    }

    [Fact]
    public async Task PostUserAuth_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var userAuth = new UsersBuilder().WithUserAuth();
        _mockUserService.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.PostUserAuth(userAuth);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        var message = Assert.IsType<Message>(internalServerErrorResult.Value);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        Assert.Equal("Something went wrong.", message.Content);
    }

    [Fact]
    public async Task RefreshUserToken_ValidToken_ReturnsOk()
    {
        // Arrange
        var token = "validToken";
        var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: false);

        _mockUserService.Setup(us => us.GetUserByToken(It.IsAny<string>())).ReturnsAsync(user);
        _mockJwtTokenService.Setup(js => js.GenerateToken(It.IsAny<UserDto>())).Returns(token);
        _controller.HttpContext.Request.Headers["Authorization"] = "Bearer validJwtToken";

        // Act
        var result = await _controller.RefreshUserToken();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task RefreshUserToken_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        _controller.HttpContext.Request.Headers["Authorization"] = "Bearer invalidJwtToken";

        _mockUserService.Setup(us => us.GetUserByToken(It.IsAny<string>())).ReturnsAsync(It.IsAny<UserDto>());

        // Act
        var result = await _controller.RefreshUserToken();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        var response = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal("Invalid token.", response.Content);
    }

    [Fact]
    public async Task RefreshUserToken_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        _controller.HttpContext.Request.Headers["Authorization"] = "Bearer validJwtToken";

        _mockUserService.Setup(us => us.GetUserByToken(It.IsAny<string>())).ThrowsAsync(new Exception());

        // Act
        var result = await _controller.RefreshUserToken();

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
        var response = Assert.IsType<Message>(internalServerErrorResult.Value);
        Assert.Equal("Something went wrong.", response.Content);
    }
    
    [Fact]
    public async Task PostUserAuth_DisabledAuth_ReturnsBadRequest()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ENABLED_LOGIN_AUTH", "false");

        // Act
        var response = await _controller.PostUserAuth(new UserAuth());

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
        Environment.SetEnvironmentVariable("ENABLED_LOGIN_AUTH", "true");
    }
    
    [Fact]
    public async Task PostUserAuth_AzureMobileAuth_ReturnsOkResultWithToken()
    {
        // Arrange
        var apikey = "your_auth_api_key";
        var userAuth = new AzureAuth
        {
            Email = "user@user.com",
            ApiKey = apikey
        };

        var expectedValue = apikey;
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(expectedValue);
        _mockConfiguration.Setup(c => c.GetSection("MOBILE_API_KEY")).Returns(mockSection.Object);
        
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        var token = "test_token";
        _mockUserService.Setup(x => x.GetByEmail(It.IsAny<string>())).ReturnsAsync(user);
        _mockJwtTokenService.Setup(x => x.GenerateToken(It.IsAny<UserDto>())).Returns(token);

        // Act
        var result = await _controller.AzureMobileAuth(userAuth);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedToken =
            Assert.IsType<string>(okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value));
        Assert.Equal(token, returnedToken);
    }
    
    [Fact]
    public async Task PostUserAuth_AzureMobileAuth_ReturnsBadRequestInvalidApiToken()
    {
        // Arrange
        var apikey = "your_auth_api_key";
        var invalidKey = "invalidkey";
       
        var userAuth = new AzureAuth
        {
            Email = "user@user.com",
            ApiKey = invalidKey
        };

        var expectedValue = apikey;
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(expectedValue);
        
        _mockConfiguration.Setup(c => c.GetSection("MOBILE_API_KEY")).Returns(mockSection.Object);
        
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        var token = "test_token";
        _mockUserService.Setup(x => x.GetByEmail(It.IsAny<string>())).ReturnsAsync(user);
        _mockJwtTokenService.Setup(x => x.GenerateToken(It.IsAny<UserDto>())).Returns(token);

        // Act
        var result = await _controller.AzureMobileAuth(userAuth);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal("Invalid API key.", message.Content);
    }
    
    [Fact]
    public async Task PostUserAuth_AzureMobileAuth_ReturnsBadRequestInvalidUser()
    {
        // Arrange
        var apikey = "your_auth_api_key";
        var invalidKey = "invalidkey";
       
        var userAuth = new AzureAuth
        {
            Email = "user@user.com",
            ApiKey = invalidKey
        };

        var expectedValue = apikey;
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(expectedValue);
        
        _mockConfiguration.Setup(c => c.GetSection("MOBILE_API_KEY")).Returns(mockSection.Object);
        
        var token = "test_token";
        _mockJwtTokenService.Setup(x => x.GenerateToken(It.IsAny<UserDto>())).Returns(token);

        // Act
        var result = await _controller.AzureMobileAuth(userAuth);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal("Invalid API key.", message.Content);
    }

    [Fact]
    public async Task IssueMobileAuthCode_WhenUserExists_ReturnsCode()
    {
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        _mockGetOrCreateUserService
            .Setup(x => x.GetUserOrCreateUser(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockMobileAuthCodeService
            .Setup(x => x.IssueAsync(user.Id))
            .ReturnsAsync(new MobileAuthCodeIssued("ABCD-2345", expiresAt));

        var result = await _controller.IssueMobileAuthCode();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ABCD-2345", okResult.Value?.GetType().GetProperty("code")?.GetValue(okResult.Value));
        Assert.Equal(expiresAt, okResult.Value?.GetType().GetProperty("expiresAt")?.GetValue(okResult.Value));
    }

    [Fact]
    public async Task IssueMobileAuthCode_WhenUserMissing_ReturnsBadRequest()
    {
        _mockGetOrCreateUserService
            .Setup(x => x.GetUserOrCreateUser(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _controller.IssueMobileAuthCode();

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal("User not found.", message.Content);
    }

    [Fact]
    public async Task RedeemMobileAuthCode_ValidCode_ReturnsTokenAndEmail()
    {
        var userId = Guid.NewGuid();
        var user = new UserDto(userId, "testuser", "user@user.com", false);
        var token = "test_token";
        SetupMobileApiKey("your_auth_api_key");
        _mockMobileAuthCodeService.Setup(x => x.ConsumeAsync("ABCD-2345")).ReturnsAsync(userId);
        _mockUserService.Setup(x => x.Get(userId)).ReturnsAsync(user);
        _mockJwtTokenService.Setup(x => x.GenerateToken(user)).Returns(token);

        var result = await _controller.RedeemMobileAuthCode(new MobileAuthCodeRedeem
        {
            Code = "ABCD-2345",
            ApiKey = "your_auth_api_key"
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(token, okResult.Value?.GetType().GetProperty("token")?.GetValue(okResult.Value));
        Assert.Equal(user.Email, okResult.Value?.GetType().GetProperty("email")?.GetValue(okResult.Value));
        _mockUserService.Verify(x => x.UpdateUserToken(token, user.Username.ToLower()), Times.Once);
    }

    [Fact]
    public async Task RedeemMobileAuthCode_InvalidApiKey_ReturnsBadRequest()
    {
        SetupMobileApiKey("your_auth_api_key");

        var result = await _controller.RedeemMobileAuthCode(new MobileAuthCodeRedeem
        {
            Code = "ABCD-2345",
            ApiKey = "invalid"
        });

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal("Invalid API key.", message.Content);
        _mockMobileAuthCodeService.Verify(x => x.ConsumeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RedeemMobileAuthCode_InvalidCode_ReturnsBadRequest()
    {
        SetupMobileApiKey("your_auth_api_key");
        _mockMobileAuthCodeService.Setup(x => x.ConsumeAsync(It.IsAny<string>())).ReturnsAsync((Guid?)null);

        var result = await _controller.RedeemMobileAuthCode(new MobileAuthCodeRedeem
        {
            Code = "ABCD-2345",
            ApiKey = "your_auth_api_key"
        });

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal("Invalid or expired code.", message.Content);
    }

    private void SetupMobileApiKey(string apiKey)
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(apiKey);
        _mockConfiguration.Setup(c => c.GetSection("MOBILE_API_KEY")).Returns(mockSection.Object);
    }
}