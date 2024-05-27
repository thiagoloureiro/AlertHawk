using AlertHawk.Application.Interfaces;
using AlertHawk.Application.Services;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Tests.Builders;
using Moq;

namespace AlertHawk.Authentication.Tests.ServicesTests;

public class GetOrCreateUserServiceTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly GetOrCreateUserService _getOrCreateUserService;
    private readonly string _email = "test@example.com";
    public GetOrCreateUserServiceTests()
    {
        _mockUserService = new Mock<IUserService>();
        _getOrCreateUserService = new GetOrCreateUserService(_mockUserService.Object);
    }

    [Fact]
    public async Task GetUserOrCreateUser_UserExists_ReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().DefaulClaimsPrincipal(_email);
        var existingUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.Setup(s => s.GetByEmail(_email)).ReturnsAsync(existingUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser, result);
    }
    [Fact]
    public async Task GetUserOrCreateUser_UserExists_FetchDataFromNameLoggedReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().NameTypeClaimsPrincipal(_email);
        var existingUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.Setup(s => s.GetByEmail(_email)).ReturnsAsync(existingUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser, result);
    }


    [Fact]
    public async Task GetUserOrCreateUser_FechEmailFromEmailClaim_UserExists_ReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().EmailTypeClaimsPrincipal(_email);

        var existingUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.Setup(s => s.GetByEmail(_email)).ReturnsAsync(existingUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser, result);
    }

    [Fact]
    public async Task GetUserOrCreateUser_FechEmailFromPreferredUserNameClaim_UserExists_ReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().PreferredUsernameClaimsPrincipal(_email);
        var existingUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.Setup(s => s.GetByEmail(_email)).ReturnsAsync(existingUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser, result);
    }

    [Fact]
    public async Task GetUserOrCreateUser_UserDoesNotExist_CreatesAndReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().DefaulClaimsPrincipal(_email);
        var returnUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.SetupSequence(s => s.GetByEmail(_email)).ReturnsAsync((UserDto?)null).ReturnsAsync(returnUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        _mockUserService.Verify(s => s.CreateFromAzure(It.IsAny<UserCreationFromAzure>()), Times.Once);
        _mockUserService.Verify(s => s.GetByEmail(_email), Times.Exactly(2));
        Assert.NotNull(result);
        Assert.Equal(_email, result.Email);
    }

    [Fact]
    public async Task GetUserOrCreateUser_EmailNotPresentInClaims_ThrowsException()
    {
        // Arrange
        var claims = new ClaimsBuilder().EmptyClaimsPrincipal();
        
        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);
        
        // Assert
        Assert.Null(result);
    }
    [Fact]
    public async Task GetUserOrCreateUser_UserDoesNotExist_CreatesOnlyWithGivenNameAndReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().DefaulClaimsPrincipalWithgivenName(_email);
        var returnUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.SetupSequence(s => s.GetByEmail(_email)).ReturnsAsync((UserDto?)null).ReturnsAsync(returnUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        _mockUserService.Verify(s => s.CreateFromAzure(It.IsAny<UserCreationFromAzure>()), Times.Once);
        _mockUserService.Verify(s => s.GetByEmail(_email), Times.Exactly(2));
        Assert.NotNull(result);
        Assert.Equal(_email, result.Email);
    }
    [Fact]
    public async Task GetUserOrCreateUser_UserDoesNotExist_CreatesOnlyWithSurNameAndReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().DefaulClaimsPrincipalWithsurName(_email);
        var returnUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.SetupSequence(s => s.GetByEmail(_email)).ReturnsAsync((UserDto?)null).ReturnsAsync(returnUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        _mockUserService.Verify(s => s.CreateFromAzure(It.IsAny<UserCreationFromAzure>()), Times.Once);
        _mockUserService.Verify(s => s.GetByEmail(_email), Times.Exactly(2));
        Assert.NotNull(result);
        Assert.Equal(_email, result.Email);
    } [Fact]
    public async Task GetUserOrCreateUser_UserDoesNotExist_CreatesOnlyWithGivenNameAndSurNameAndReturnsUser()
    {
        // Arrange
        var claims = new ClaimsBuilder().DefaulClaimsPrincipalWithsurNameAndGivenName(_email);
        var returnUser = new UsersBuilder().WithUserEmailAndAdminIsFalse(_email);
        _mockUserService.SetupSequence(s => s.GetByEmail(_email)).ReturnsAsync((UserDto?)null).ReturnsAsync(returnUser);

        // Act
        var result = await _getOrCreateUserService.GetUserOrCreateUser(claims);

        // Assert
        _mockUserService.Verify(s => s.CreateFromAzure(It.IsAny<UserCreationFromAzure>()), Times.Once);
        _mockUserService.Verify(s => s.GetByEmail(_email), Times.Exactly(2));
        Assert.NotNull(result);
        Assert.Equal(_email, result.Email);
    }
}