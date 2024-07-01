using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Controllers;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Tests.Builders;
using Moq;

namespace AlertHawk.Authentication.Tests.ControllerTests;


public class AuthControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IGetOrCreateUserService> _mockGetOrCreateUserService;
    private readonly AuthController _controller;

    public AuthControllerTests(Mock<IGetOrCreateUserService> mockGetOrCreateUserService)
    {
        _mockGetOrCreateUserService = mockGetOrCreateUserService;
        _mockUserService = new Mock<IUserService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        
        _controller = new AuthController(_mockUserService.Object, _mockJwtTokenService.Object, _mockGetOrCreateUserService.Object);
    }

    [Fact]
    public async Task PostUserAuth_ValidCredentials_ReturnsOkResultWithToken()
    {
        // Arrange
        var userAuth = new UsersBuilder().WithUserAuth();
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse(null);
        var token = "test_token";
        _mockUserService.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(user);
        _mockJwtTokenService.Setup(x => x.GenerateToken(It.IsAny<UserDto>())).Returns(token);

        // Act
        var result = await _controller.PostUserAuth(userAuth);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedToken = Assert.IsType<string>(okResult.Value.GetType().GetProperty("token").GetValue(okResult.Value));
        Assert.Equal(token, returnedToken);
    }

    [Fact]
    public async Task PostUserAuth_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var userAuth = new UsersBuilder().WithUserAuth();
        _mockUserService.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((UserDto)null);

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
        _mockUserService.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new System.Exception("Test exception"));

        // Act
        var result = await _controller.PostUserAuth(userAuth);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        var message = Assert.IsType<Message>(internalServerErrorResult.Value);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        Assert.Equal("Something went wrong.", message.Content);
    }
}