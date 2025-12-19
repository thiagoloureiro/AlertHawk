using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Controllers;
using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Tests.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AlertHawk.Authentication.Tests.ControllerTests;

public class UserClustersControllerTests
{
    private readonly Mock<IUserClustersService> _mockUserClustersService;
    private readonly Mock<IGetOrCreateUserService> _mockGetOrCreateUserService;
    private readonly UserClustersController _controller;

    public UserClustersControllerTests()
    {
        _mockUserClustersService = new Mock<IUserClustersService>();
        _mockGetOrCreateUserService = new Mock<IGetOrCreateUserService>();

        _controller = new UserClustersController(_mockUserClustersService.Object,
            _mockGetOrCreateUserService.Object)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userCluster = new UserClusters
        {
            UserId = Guid.NewGuid(),
            ClusterName = "cluster1"
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Create(userCluster);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUserClustersService.Verify(s => s.CreateAsync(userCluster), Times.Once);
    }

    [Fact]
    public async Task Create_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var userCluster = new UserClusters
        {
            UserId = Guid.NewGuid(),
            ClusterName = "cluster1"
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.Create(userCluster);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        var message = Assert.IsType<Message>(forbiddenResult.Value);
        Assert.Equal("This user is not authorized to do this operation", message.Content);
        _mockUserClustersService.Verify(s => s.CreateAsync(It.IsAny<UserClusters>()), Times.Never);
    }

    [Fact]
    public async Task Create_ModelStateInvalid_ReturnsBadRequest()
    {
        // Arrange
        var userCluster = new UserClusters
        {
            UserId = Guid.NewGuid(),
            ClusterName = "cluster1"
        };
        _controller.ModelState.AddModelError("Error", "Invalid model state");

        // Act
        var result = await _controller.Create(userCluster);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockUserClustersService.Verify(s => s.CreateAsync(It.IsAny<UserClusters>()), Times.Never);
    }

    [Fact]
    public async Task Create_ThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var userCluster = new UserClusters
        {
            UserId = Guid.NewGuid(),
            ClusterName = "cluster1"
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        var exceptionMessage = "Cluster already exists";
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserClustersService.Setup(s => s.CreateAsync(userCluster))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _controller.Create(userCluster);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal(exceptionMessage, message.Content);
    }

    [Fact]
    public async Task Create_ThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var userCluster = new UserClusters
        {
            UserId = Guid.NewGuid(),
            ClusterName = "cluster1"
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserClustersService.Setup(s => s.CreateAsync(userCluster))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Create(userCluster);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var message = Assert.IsType<Message>(objectResult.Value);
        Assert.Equal("Something went wrong.", message.Content);
    }

    [Fact]
    public async Task CreateOrUpdate_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new CreateOrUpdateUserClustersRequest
        {
            UserId = Guid.NewGuid(),
            Clusters = new List<string> { "cluster1", "cluster2" }
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.CreateOrUpdate(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUserClustersService.Verify(s => s.CreateOrUpdateAsync(request.UserId, request.Clusters), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdate_EmptyList_ReturnsOk()
    {
        // Arrange
        var request = new CreateOrUpdateUserClustersRequest
        {
            UserId = Guid.NewGuid(),
            Clusters = new List<string>()
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.CreateOrUpdate(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUserClustersService.Verify(s => s.CreateOrUpdateAsync(request.UserId, request.Clusters), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdate_NullClusters_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrUpdateUserClustersRequest
        {
            UserId = Guid.NewGuid(),
            Clusters = null!
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.CreateOrUpdate(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal("Clusters list cannot be null", message.Content);
        _mockUserClustersService.Verify(s => s.CreateOrUpdateAsync(It.IsAny<Guid>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdate_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var request = new CreateOrUpdateUserClustersRequest
        {
            UserId = Guid.NewGuid(),
            Clusters = new List<string> { "cluster1" }
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.CreateOrUpdate(request);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        _mockUserClustersService.Verify(s => s.CreateOrUpdateAsync(It.IsAny<Guid>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdate_ModelStateInvalid_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrUpdateUserClustersRequest
        {
            UserId = Guid.NewGuid(),
            Clusters = new List<string> { "cluster1" }
        };
        _controller.ModelState.AddModelError("Error", "Invalid model state");

        // Act
        var result = await _controller.CreateOrUpdate(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockUserClustersService.Verify(s => s.CreateOrUpdateAsync(It.IsAny<Guid>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdate_ThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrUpdateUserClustersRequest
        {
            UserId = Guid.NewGuid(),
            Clusters = new List<string> { "cluster1" }
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        var exceptionMessage = "Invalid operation";
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserClustersService.Setup(s => s.CreateOrUpdateAsync(request.UserId, request.Clusters))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _controller.CreateOrUpdate(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var message = Assert.IsType<Message>(badRequestResult.Value);
        Assert.Equal(exceptionMessage, message.Content);
    }

    [Fact]
    public async Task CreateOrUpdate_ThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateOrUpdateUserClustersRequest
        {
            UserId = Guid.NewGuid(),
            Clusters = new List<string> { "cluster1" }
        };
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserClustersService.Setup(s => s.CreateOrUpdateAsync(request.UserId, request.Clusters))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.CreateOrUpdate(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var message = Assert.IsType<Message>(objectResult.Value);
        Assert.Equal("Something went wrong.", message.Content);
    }

    [Fact]
    public async Task GetAllByUserId_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        var clusters = new List<UserClusters>
        {
            new UserClusters { UserId = userId, ClusterName = "cluster1" },
            new UserClusters { UserId = userId, ClusterName = "cluster2" }
        };
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserClustersService.Setup(s => s.GetByUserIdAsync(userId))
            .ReturnsAsync(clusters);

        // Act
        var result = await _controller.GetAllByUserId(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(clusters, okResult.Value);
        _mockUserClustersService.Verify(s => s.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAllByUserId_UserOwnsClusters_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserDto(userId, "testuser", "test@test.com", false);
        var clusters = new List<UserClusters>
        {
            new UserClusters { UserId = userId, ClusterName = "cluster1" }
        };
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserClustersService.Setup(s => s.GetByUserIdAsync(userId))
            .ReturnsAsync(clusters);

        // Act
        var result = await _controller.GetAllByUserId(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(clusters, okResult.Value);
    }

    [Fact]
    public async Task GetAllByUserId_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserDto(Guid.NewGuid(), "testuser", "test@test.com", false);
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetAllByUserId(userId);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        var message = Assert.IsType<Message>(forbiddenResult.Value);
        Assert.Equal("This user is not authorized to view this operation", message.Content);
        _mockUserClustersService.Verify(s => s.GetByUserIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetAllByUserId_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetAllByUserId(userId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var message = Assert.IsType<Message>(unauthorizedResult.Value);
        Assert.Equal("User not found", message.Content);
        _mockUserClustersService.Verify(s => s.GetByUserIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetAllByUserId_ThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UsersBuilder().WithUserEmailAndAdminIsTrue("");
        _mockGetOrCreateUserService.Setup(x => x.GetUserOrCreateUser(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _mockUserClustersService.Setup(s => s.GetByUserIdAsync(userId))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetAllByUserId(userId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var message = Assert.IsType<Message>(objectResult.Value);
        Assert.Equal("Something went wrong.", message.Content);
    }
}

