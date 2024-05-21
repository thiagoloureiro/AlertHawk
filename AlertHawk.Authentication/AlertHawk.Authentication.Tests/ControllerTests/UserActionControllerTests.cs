using System.Security.Claims;
using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Controllers;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertHawk.Authentication.Tests.ControllerTests;


public class UserActionControllerTests
{
    private readonly Mock<IUserActionService> _mockUserActionService;
    private readonly UserActionController _controller;
    private readonly Mock<IGetOrCreateUserService> _mockGetOrCreateUserHelper;

    public UserActionControllerTests()
    {
        _mockUserActionService = new Mock<IUserActionService>();
        _mockGetOrCreateUserHelper = new Mock<IGetOrCreateUserService>();
        _controller = new UserActionController(_mockUserActionService.Object, _mockGetOrCreateUserHelper.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task PostUserActionCreation_EmptyAction_ReturnsBadRequest()
    {
        // Arrange
        var userAction = new UserAction { Action = "" };

        // Act
        var result = await _controller.PostUserActionCreation(userAction);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Action is required", badRequestResult.Value);
    }

    [Fact]
    public async Task PostUserActionCreation_UserNotFound_ReturnsBadRequest()
    {
        // Arrange
        var userAction = new UserAction { Action = "TestAction" };
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
    
        // Act
        var result = await _controller.PostUserActionCreation(userAction);
    
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User/Token not found", badRequestResult.Value);
    }

    [Fact]
    public async Task PostUserActionCreation_ValidUserAction_ReturnsOk()
    {
        // Arrange
        var userAction = new UserAction { Action = "TestAction" };
        var userDto = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: null, IsAdmin: false);
        _mockGetOrCreateUserHelper.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(userDto);


        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        // Act
        var result = await _controller.PostUserActionCreation(userAction);

        // Assert
        _mockUserActionService.Verify(x => x.CreateAsync(It.Is<UserAction>(ua => ua.UserId == userDto.Id && ua.Action == userAction.Action)), Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetUserActions_ReturnsOkWithUserActions()
    {
        // Arrange
        var userActions = new List<UserAction> { new UserAction { Action = "TestAction" } };
        _mockUserActionService.Setup(x => x.GetAsync()).ReturnsAsync(userActions);

        // Act
        var result = await _controller.GetUserActions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUserActions = Assert.IsType<List<UserAction>>(okResult.Value);
        Assert.Equal(userActions, returnedUserActions);
    }
}