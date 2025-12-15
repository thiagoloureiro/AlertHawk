using AlertHawk.Application.Services;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Moq;

namespace AlertHawk.Authentication.Tests.ServicesTests;

public class UserClustersServiceTests
{
    private readonly Mock<IUserClustersRepository> _mockRepository;
    private readonly UserClustersService _service;

    public UserClustersServiceTests()
    {
        _mockRepository = new Mock<IUserClustersRepository>();
        _service = new UserClustersService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryMethod()
    {
        // Arrange
        var userCluster = new UserClusters
        {
            UserId = Guid.NewGuid(),
            ClusterName = "cluster1"
        };

        // Act
        await _service.CreateAsync(userCluster);

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(userCluster), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithClusters_CallsRepositoryMethodsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clusterNames = new List<string> { "cluster1", "cluster2", "cluster3" };

        // Act
        await _service.CreateOrUpdateAsync(userId, clusterNames);

        // Assert
        _mockRepository.Verify(r => r.DeleteAllByUserIdAsync(userId), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<UserClusters>(uc => 
            uc.UserId == userId && uc.ClusterName == "cluster1")), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<UserClusters>(uc => 
            uc.UserId == userId && uc.ClusterName == "cluster2")), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<UserClusters>(uc => 
            uc.UserId == userId && uc.ClusterName == "cluster3")), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserClusters>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithEmptyList_OnlyDeletesClusters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clusterNames = new List<string>();

        // Act
        await _service.CreateOrUpdateAsync(userId, clusterNames);

        // Assert
        _mockRepository.Verify(r => r.DeleteAllByUserIdAsync(userId), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserClusters>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithNullOrWhiteSpaceClusters_IgnoresThem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clusterNames = new List<string> { "cluster1", "", "   ", null!, "cluster2" };

        // Act
        await _service.CreateOrUpdateAsync(userId, clusterNames);

        // Assert
        _mockRepository.Verify(r => r.DeleteAllByUserIdAsync(userId), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<UserClusters>(uc => 
            uc.UserId == userId && uc.ClusterName == "cluster1")), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<UserClusters>(uc => 
            uc.UserId == userId && uc.ClusterName == "cluster2")), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserClusters>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_DeletesBeforeCreating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clusterNames = new List<string> { "cluster1" };
        var callOrder = new List<string>();

        _mockRepository.Setup(r => r.DeleteAllByUserIdAsync(userId))
            .Callback(() => callOrder.Add("Delete"));
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UserClusters>()))
            .Callback(() => callOrder.Add("Create"));

        // Act
        await _service.CreateOrUpdateAsync(userId, clusterNames);

        // Assert
        Assert.Equal("Delete", callOrder[0]);
        Assert.Equal("Create", callOrder[1]);
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
    public async Task GetByUserIdAsync_CallsRepositoryMethodAndReturnsResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clusters = new List<UserClusters>
        {
            new UserClusters { UserId = userId, ClusterName = "cluster1" },
            new UserClusters { UserId = userId, ClusterName = "cluster2" }
        };
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(clusters);

        // Act
        var result = await _service.GetByUserIdAsync(userId);

        // Assert
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        Assert.Equal(clusters, result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByUserIdAsync_NoClusters_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyClusters = new List<UserClusters>();
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(emptyClusters);

        // Act
        var result = await _service.GetByUserIdAsync(userId);

        // Assert
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        Assert.Empty(result);
    }
}


