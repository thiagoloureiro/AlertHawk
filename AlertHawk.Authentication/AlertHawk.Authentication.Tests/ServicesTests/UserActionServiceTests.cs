using AlertHawk.Application.Services;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Moq;

namespace AlertHawk.Authentication.Tests.ServicesTests;

public class UserActionServiceTests
{
    private readonly Mock<IUserActionRepository> _mockUserActionRepository;
    private readonly UserActionService _userActionService;

    public UserActionServiceTests()
    {
        _mockUserActionRepository = new Mock<IUserActionRepository>();
        _userActionService = new UserActionService(_mockUserActionRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryCreateAsync()
    {
        // Arrange
        var userAction = new UserAction { Id = 1, Action = "TestAction" };

        // Act
        await _userActionService.CreateAsync(userAction);

        // Assert
        _mockUserActionRepository.Verify(r => r.CreateAsync(userAction), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsListOfUserActions()
    {
        // Arrange
        var userActions = new List<UserAction>
        {
            new UserAction { Id = 1, Action = "TestAction1" },
            new UserAction { Id = 2, Action = "TestAction2" }
        };
        _mockUserActionRepository.Setup(r => r.GetAsync()).ReturnsAsync(userActions);

        // Act
        var result = await _userActionService.GetAsync();

        // Assert
        Assert.Equal(userActions, result);
    }
}
