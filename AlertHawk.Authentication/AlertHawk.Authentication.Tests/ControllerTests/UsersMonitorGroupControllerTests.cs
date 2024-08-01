using System.Security.Claims;
using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Controllers;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Tests.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AlertHawk.Authentication.Tests.ControllerTests
{
    public class UsersMonitorGroupControllerTests
    {
        private readonly Mock<IUsersMonitorGroupService> _mockUsersMonitorGroupService;
        private readonly Mock<IGetOrCreateUserService> _mockGetOrCreateUserService;
        private readonly UsersMonitorGroupController _controller;

        public UsersMonitorGroupControllerTests()
        {
            _mockUsersMonitorGroupService = new Mock<IUsersMonitorGroupService>();
            _mockGetOrCreateUserService = new Mock<IGetOrCreateUserService>();

            _controller = new UsersMonitorGroupController(_mockUsersMonitorGroupService.Object,
                _mockGetOrCreateUserService.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task AssignUserToGroups_ValidRequest_ReturnsOk()
        {
            // Arrange
            var usersMonitorGroup = new List<UsersMonitorGroup>();
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AssignUserToGroups_InvalidUser_ReturnsForbidden()
        {
            // Arrange
            var usersMonitorGroup = new List<UsersMonitorGroup>();
            var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            var message = Assert.IsType<Message>(forbiddenResult.Value);
            Assert.Equal((string?)"This user is not authorized to do this operation", message.Content);
        }

        [Fact]
        public async Task AssignUserToGroups_ModelStateInvalid_ReturnsBadRequest()
        {
            // Arrange
            var usersMonitorGroup = new List<UsersMonitorGroup>();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert   
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AssignUserToGroups_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var usersMonitorGroup = new List<UsersMonitorGroup>();
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _mockUsersMonitorGroupService.Setup(s => s.CreateOrUpdateAsync(usersMonitorGroup))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var message = Assert.IsType<Message>(objectResult.Value);
            Assert.Equal("Something went wrong.", message.Content);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithUserGroups()
        {
            // Arrange
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            var userGroups = new List<UsersMonitorGroup>();
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _mockUsersMonitorGroupService.Setup(s => s.GetAsync(user.Id))
                .ReturnsAsync(userGroups);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(userGroups, okResult.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithNoUserGroups()
        {
            // Arrange
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            var userGroups = new List<UsersMonitorGroup>();
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()));
            _mockUsersMonitorGroupService.Setup(s => s.GetAsync(user.Id))
                .ReturnsAsync(userGroups);

            // Act
            var result = await _controller.GetAll();

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task GetAllByUserId_UnauthorizedUser_ReturnsForbidden()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "", IsAdmin: false);
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetAllByUserId(userId);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            var message = Assert.IsType<Message>(forbiddenResult.Value);
            Assert.Equal("This user is not authorized to do this operation", message.Content);
        }

        [Fact]
        public async Task GetAllByUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetAllByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }

        [Fact]
        public async Task AssignUserToGroups_ThrowsInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var usersMonitorGroup = new List<UsersMonitorGroup>();
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var exceptionMessage = "User already exists";
            _mockUsersMonitorGroupService.Setup(s => s.CreateOrUpdateAsync(usersMonitorGroup))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = Assert.IsType<Message>(badRequestResult.Value);
            Assert.Equal(exceptionMessage, message.Content);
        }

        [Fact]
        public async Task DeleteMonitorGroupByGroupMonitorId_UnauthorizedUser_ReturnsForbidden()
        {
            // Arrange
            var groupMonitorId = 1;
            var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.DeleteMonitorGroupByGroupMonitorId(groupMonitorId);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
            var message = Assert.IsType<Message>(forbiddenResult.Value);
            Assert.Equal("This user is not authorized to do this operation", message.Content);
        }

        [Fact]
        public async Task DeleteMonitorGroupByGroupMonitorId_AuthorizedUser_ReturnsOk()
        {
            // Arrange
            var groupMonitorId = 1;
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");

            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.DeleteMonitorGroupByGroupMonitorId(groupMonitorId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AssignUserToGroup_ValidRequest_ReturnsOk()
        {
            // Arrange
            var usersMonitorGroup = new UsersMonitorGroup();
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            Assert.IsType<OkResult>(result);
        }
        
        [Fact]
        public async Task AssignUserToGroup_BadRequest_ReturnsBadRequest()
        {
            // Arrange
            var usersMonitorGroup = new UsersMonitorGroup();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        
        [Fact]
        public async Task AssignUserToGroup_ThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var usersMonitorGroup = new UsersMonitorGroup();
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _mockUsersMonitorGroupService.Setup(s => s.AssignUserToGroup(usersMonitorGroup))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var message = Assert.IsType<Message>(objectResult.Value);
            Assert.Equal("Something went wrong.", message.Content);
        }
        
        [Fact]
        public async Task AssignUserToGroup_ThrowsInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var usersMonitorGroup = new UsersMonitorGroup();
            var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
            _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var exceptionMessage = "User already exists";
            _mockUsersMonitorGroupService.Setup(s => s.AssignUserToGroup(usersMonitorGroup))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.AssignUserToGroup(usersMonitorGroup);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = Assert.IsType<Message>(badRequestResult.Value);
            Assert.Equal(exceptionMessage, message.Content);
        }
    }
}