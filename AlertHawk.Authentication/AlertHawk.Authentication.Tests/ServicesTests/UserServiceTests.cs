using AlertHawk.Application.Services;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AlertHawk.Authentication.Tests.Builders;
using Moq;

namespace AlertHawk.Authentication.Tests.ServicesTests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mockUserRepository.Object);
    }

    [Fact]
    public async Task Create_CallsRepositoryCreate()
    {
        // Arrange
        var userCreation = new UserCreation
        {
            Username = null,
            Password = null,
            RepeatPassword = null,
            UserEmail = null
        };

        // Act
        await _userService.Create(userCreation);

        // Assert
        _mockUserRepository.Verify(r => r.Create(userCreation), Times.Once);
    }

    [Fact]
    public async Task CreateFromAzure_SetsIsAdminForFirstUser()
    {
        // Arrange
        var userCreation = new UserCreationFromAzure("Test User", "test@example.com");
        _mockUserRepository.Setup(r => r.GetAll()).ReturnsAsync(new List<UserDto>());

        // Act
        await _userService.CreateFromAzure(userCreation);

        // Assert
        Assert.True(userCreation.IsAdmin);
        _mockUserRepository.Verify(r => r.CreateFromAzure(userCreation), Times.Once);
    }

    [Fact]
    public async Task CreateFromAzure_DoesNotSetIsAdminForNonFirstUser()
    {
        // Arrange
        var userCreation = new UserCreationFromAzure("Test User", "test@example.com");
        _mockUserRepository.Setup(r => r.GetAll()).ReturnsAsync(new List<UserDto>
            { new UsersBuilder().WithUserEmailAndAdminIsFalse(userCreation.Email) });

        // Act
        await _userService.CreateFromAzure(userCreation);

        // Assert
        Assert.False(userCreation.IsAdmin);
        _mockUserRepository.Verify(r => r.CreateFromAzure(userCreation), Times.Once);
    }

    [Fact]
    public async Task Update_CallsRepositoryUpdate()
    {
        // Arrange
        var userUpdate = new UsersBuilder().WithUserEmailAndAdminIsFalse("");

        // Act
        await _userService.Update(userUpdate);

        // Assert
        _mockUserRepository.Verify(r => r.Update(userUpdate), Times.Once);
    }

    [Fact]
    public async Task Login_CallsRepositoryLogin()
    {
        // Arrange
        var email = "testuser@user.com";
        var password = "testpassword";
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        _mockUserRepository.Setup(r => r.LoginWithEmail(email, password)).ReturnsAsync(user);

        // Act
        var result = await _userService.Login(email, password);

        // Assert
        Assert.Equal(user, result);
        _mockUserRepository.Verify(r => r.LoginWithEmail(email, password), Times.Once);
    }

    [Fact]
    public async Task LoginWithEmail_CallsRepositoryLogin()
    {
        // Arrange
        var email = "testuser@email.com";
        var password = "testpassword";
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        _mockUserRepository.Setup(r => r.LoginWithEmail(email, password)).ReturnsAsync(user);
        // Act
        var result = await _userService.Login(email, password);

        // Assert
        Assert.Equal(user, result);
        _mockUserRepository.Verify(r => r.LoginWithEmail(email, password), Times.Once);
    }

    [Fact]
    public async Task Get_CallsRepositoryGet()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse(null);
        _mockUserRepository.Setup(r => r.Get(userId)).ReturnsAsync(user);

        // Act
        var result = await _userService.Get(userId);

        // Assert
        Assert.Equal(user, result);
        _mockUserRepository.Verify(r => r.Get(userId), Times.Once);
    }

    [Fact]
    public async Task GetByEmail_CallsRepositoryGetByEmail()
    {
        // Arrange
        var email = "test@example.com";
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        _mockUserRepository.Setup(r => r.GetByEmail(email)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetByEmail(email);

        // Assert
        Assert.Equal(user, result);
        _mockUserRepository.Verify(r => r.GetByEmail(email), Times.Once);
    }

    [Fact]
    public async Task GetByUsername_CallsRepositoryGetByUsername()
    {
        // Arrange
        var username = "testuser";
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        _mockUserRepository.Setup(r => r.GetByUsername(username)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetByUsername(username);

        // Assert
        Assert.Equal(user, result);
        _mockUserRepository.Verify(r => r.GetByUsername(username), Times.Once);
    }

    [Fact]
    public async Task GetAll_CallsRepositoryGetAll()
    {
        // Arrange
        var users = new List<UserDto> { new UsersBuilder().WithUserEmailAndAdminIsFalse(null) };
        _mockUserRepository.Setup(r => r.GetAll()).ReturnsAsync(users);

        // Act
        var result = await _userService.GetAll();

        // Assert
        Assert.Equal(users, result);
        _mockUserRepository.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_UserExists_SendsEmail()
    {
        // Arrange
        var username = "testuser";
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("testuser@example.com");
        var newPassword = "newPassword123";

        _mockUserRepository.Setup(repo => repo.GetByEmail(username)).ReturnsAsync(user);
        _mockUserRepository.Setup(repo => repo.ResetPassword(username)).ReturnsAsync(newPassword);

        // Act
        await _userService.ResetPassword(username);

        // Assert

        _mockUserRepository.Verify(repo => repo.GetByEmail(username), Times.Once);
        _mockUserRepository.Verify(repo => repo.ResetPassword(username), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_UserDoesNotExist_DoesNotSendEmail()
    {
        // Arrange
        var username = "nonexistentuser";

        _mockUserRepository.Setup(repo => repo.GetByEmail(username)).ReturnsAsync(It.IsAny<UserDto>());

        // Act
        await _userService.ResetPassword(username);

        // Assert
        _mockUserRepository.Verify(repo => repo.GetByEmail(username), Times.Once);
        _mockUserRepository.Verify(repo => repo.ResetPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_CallsRepositoryDelete()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _userService.Delete(userId);

        // Assert
        _mockUserRepository.Verify(r => r.Delete(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByToken_CallsRepositoryGetUserByToken()
    {
        // Arrange
        var token = "token";
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");

        _mockUserRepository.Setup(x => x.GetUserByToken(token)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByToken(token);

        // Assert
        Assert.Equal(user, result);
    }

    [Fact]
    public async Task UpdateUserToken_CallsRepositoryUpdateUserToken()
    {
        // Arrange
        var token = "token";
        var username = "username";

        _mockUserRepository.Setup(x => x.UpdateUserToken(token, username));

        // Act
        await _userService.UpdateUserToken(token, username);

        // Assert
        _mockUserRepository.Verify(r => r.UpdateUserToken(token, username), Times.Once);
    }

    [Fact]
    public async Task LoginWithEmail_CallsRepositoryLoginWithEmail_ReturnsTrue()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password";
        var user = new UsersBuilder().WithUserEmailAndAdminIsFalse("");
        _mockUserRepository.Setup(r => r.GetByEmail(email)).ReturnsAsync(user);

        // Act
        await _userService.LoginWithEmail(email, password);

        // Assert
        _mockUserRepository.Verify(r => r.LoginWithEmail(email, password), Times.Once);
    }

    [Fact]
    public async Task LoginWithEmail_CallsRepositoryLoginWithEmail_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        _mockUserRepository.Setup(r => r.GetByEmail(email)).ReturnsAsync(It.IsAny<UserDto>());

        // Act
        var result = await _userService.LoginWithEmail(email, "wrongPassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePassword_CallsRepositoryUpdatePassword()
    {
        // Arrange
        var password = "password";
        var username = "username@user.com";

        _mockUserRepository.Setup(x => x.UpdatePassword(username, password));

        // Act
        await _userService.UpdatePassword(username, password);

        // Assert
        _mockUserRepository.Verify(r => r.UpdatePassword(username, password), Times.Once);
    }
}