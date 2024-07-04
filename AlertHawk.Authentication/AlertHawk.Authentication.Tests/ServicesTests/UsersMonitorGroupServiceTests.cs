using AlertHawk.Application.Services;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Moq;

namespace AlertHawk.Authentication.Tests.ServicesTests;

public class UsersMonitorGroupServiceTests
{
     private readonly Mock<IUsersMonitorGroupRepository> _mockRepository;
    private readonly UsersMonitorGroupService _service;

    public UsersMonitorGroupServiceTests()
    {
        _mockRepository = new Mock<IUsersMonitorGroupRepository>();
        _service = new UsersMonitorGroupService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CallsRepositoryMethodsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var usersMonitorGroups = new List<UsersMonitorGroup>
        {
            new UsersMonitorGroup { UserId = userId, GroupMonitorId = 1 },
            new UsersMonitorGroup { UserId = userId, GroupMonitorId = 2 }
        };

        // Act
        await _service.CreateOrUpdateAsync(usersMonitorGroups);

        // Assert
        _mockRepository.Verify(r => r.DeleteAllByUserIdAsync(userId), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UsersMonitorGroup>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_DoesNotCallCreateAsync_WhenGroupMonitorIdIsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var usersMonitorGroups = new List<UsersMonitorGroup>
        {
            new UsersMonitorGroup { UserId = userId, GroupMonitorId = 0 },
            new UsersMonitorGroup { UserId = userId, GroupMonitorId = 2 }
        };

        // Act
        await _service.CreateOrUpdateAsync(usersMonitorGroups);

        // Assert
        _mockRepository.Verify(r => r.DeleteAllByUserIdAsync(userId), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UsersMonitorGroup>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAllByUserIdAsync_CallsRepositoryMethod()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _service.DeleteAllByUserIdAsync(userId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAllByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAsync_CallsRepositoryMethodAndReturnsResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var usersMonitorGroups = new List<UsersMonitorGroup>
        {
            new UsersMonitorGroup { UserId = userId, GroupMonitorId = 1 },
            new UsersMonitorGroup { UserId = userId, GroupMonitorId = 2 }
        };
        _mockRepository.Setup(r => r.GetAsync(userId)).ReturnsAsync(usersMonitorGroups);

        // Act
        var result = await _service.GetAsync(userId);

        // Assert
        _mockRepository.Verify(r => r.GetAsync(userId), Times.Once);
        Assert.Equal(usersMonitorGroups, result);
    }

    [Fact]
    public async Task DeleteAllByGroupMonitorIdAsync_CallsRepositoryMethod()
    {
        // Arrange
        var groupId = 1;

        // Act
        await _service.DeleteAllByGroupMonitorIdAsync(groupId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAllByGroupMonitorIdAsync(groupId), Times.Once);
    }
    [Fact]
    public async Task AssignUserToGroup_CallsRepositoryMethod()
    {
        // Arrange
        var userMonitorGroup = new UsersMonitorGroup { UserId = Guid.NewGuid(), GroupMonitorId = 1 };

        // Act
        await _service.AssignUserToGroup(userMonitorGroup);

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(userMonitorGroup), Times.Once);
    }
}
