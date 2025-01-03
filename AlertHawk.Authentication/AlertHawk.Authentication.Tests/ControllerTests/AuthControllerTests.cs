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

    public AuthControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _controller = new AuthController(_mockUserService.Object, _mockJwtTokenService.Object, _mockConfiguration.Object)
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
        Assert.Equal("Invalid user.", message.Content);
    }
}