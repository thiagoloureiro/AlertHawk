using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Monitor = System.Threading.Monitor;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class MonitorGroupControllerTests
{
    private readonly Mock<IMonitorGroupService> _mockMonitorGroupService;
    private readonly MonitorGroupController _controller;

    public MonitorGroupControllerTests()
    {
        _mockMonitorGroupService = new Mock<IMonitorGroupService>();
        _controller = new MonitorGroupController(_mockMonitorGroupService.Object);
    }

    [Fact]
    public async Task GetMonitorGroupList_ReturnsOk()
    {
        // Arrange
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Name = "name"
            }
        };
        _mockMonitorGroupService.Setup(service => service.GetMonitorGroupList())
            .ReturnsAsync(monitorGroups);

        // Act
        var result = await _controller.GetMonitorGroupList();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorGroups, okResult.Value);
    }

    [Fact]
    public async Task GetMonitorGroupListByUser_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var invalidToken = string.Empty;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = invalidToken;

        // Act
        var result = await _controller.GetMonitorGroupListByUser();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", badRequestResult.Value);
    }

    [Fact]
    public async Task GetMonitorGroupListByUser_ValidToken_ReturnsOk()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Name = "Name"
            }
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;

        _mockMonitorGroupService.Setup(service => service.GetMonitorGroupList(validToken))
            .ReturnsAsync(monitorGroups);

        // Act
        var result = await _controller.GetMonitorGroupListByUser();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMonitorDashboardGroupListByUser_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var invalidToken = string.Empty;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = invalidToken;

        // Act
        var result = await _controller.GetMonitorDashboardGroupListByUser();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", badRequestResult.Value);
    }

    [Fact]
    public async Task GetMonitorDashboardGroupListByUser_ValidToken_ReturnsOk()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Name = "Name"
            }
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;

        _mockMonitorGroupService.Setup(service => service.GetMonitorDashboardGroupListByUser(validToken))
            .ReturnsAsync(monitorGroups);

        // Act
        var result = await _controller.GetMonitorDashboardGroupListByUser();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMonitorDashboardGroupListByUserWithEnvironment_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var invalidToken = string.Empty;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = invalidToken;

        // Act
        var result = await _controller.GetMonitorDashboardGroupListByUser(MonitorEnvironment.Production);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", badRequestResult.Value);
    }

    [Fact]
    public async Task GetMonitorDashboardGroupListByUserWithEnvironment_ValidToken_ReturnsOk()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Name = "Name"
            }
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;

        _mockMonitorGroupService.Setup(service =>
                service.GetMonitorGroupListByEnvironment(validToken, MonitorEnvironment.Production))
            .ReturnsAsync(monitorGroups);

        // Act
        var result = await _controller.GetMonitorDashboardGroupListByUser(MonitorEnvironment.Production);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMonitorGroupById_ReturnsOk()
    {
        // Arrange
        var monitorGroup = new MonitorGroup
        {
            Name = "Name"
        };
        _mockMonitorGroupService.Setup(service => service.GetMonitorGroupById(It.IsAny<int>()))
            .ReturnsAsync(monitorGroup);

        // Act
        var result = await _controller.GetMonitorGroupById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(monitorGroup, okResult.Value);
    }

    [Fact]
    public async Task AddMonitorToGroup_ReturnsOk()
    {
        // Arrange
        var monitorGroupItems = new MonitorGroupItems();

        // Act
        var result = await _controller.AddMonitorToGroup(monitorGroupItems);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockMonitorGroupService.Verify(service => service.AddMonitorToGroup(monitorGroupItems), Times.Once);
    }

    [Fact]
    public async Task AddMonitorGroup_ReturnsOk()
    {
        // Arrange
        var monitorGroup = new MonitorGroup
        {
            Name = "name"
        };
        var validToken = "Bearer valid.token.here";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;

        // Act
        var result = await _controller.AddMonitorGroup(monitorGroup);

        // Assert
        Assert.IsType<OkResult>(result);
        
    }
    [Fact]
    public async Task AddMonitorGroup_WithoutToken_Returns_BadRequestObjectResult()
    {
        // Arrange
        var monitorGroup = new MonitorGroup
        {
            Name = "name"
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
      

        // Act
        var result = await _controller.AddMonitorGroup(monitorGroup);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        
    }

    [Fact]
    public async Task UpdateMonitorGroup_ReturnsOk()
    {
        // Arrange
        var monitorGroup = new MonitorGroup
        {
            Name = "name"
        };
        // Act
        var result = await _controller.UpdateMonitorGroup(monitorGroup);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockMonitorGroupService.Verify(service => service.UpdateMonitorGroup(monitorGroup), Times.Once);
    }

    [Fact]
    public async Task DeleteMonitorGroup_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var invalidToken = string.Empty;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = invalidToken;

        // Act
        var result = await _controller.DeleteMonitorGroup(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteMonitorGroup_MonitorGroupNotFound_ReturnsBadRequest()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var monitorGroup = new MonitorGroup
        {
            Name = "name",
            Id = 0
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;

        _mockMonitorGroupService.Setup(service => service.GetMonitorGroupById(It.IsAny<int>()))
            .ReturnsAsync(monitorGroup);

        // Act
        var result = await _controller.DeleteMonitorGroup(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("monitorGroups.monitorNotFound", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteMonitorGroup_MonitorGroupHasItems_ReturnsBadRequest()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var monitorGroup = new MonitorGroup
        {
            Id = 1,
            Name = "Name",
            Monitors = new List<Domain.Entities.Monitor>
            {
                new Domain.Entities.Monitor
                {
                    Name = "name",
                    HeartBeatInterval = 0,
                    Retries = 0
                }
            }
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;

        _mockMonitorGroupService.Setup(service => service.GetMonitorGroupById(It.IsAny<int>()))
            .ReturnsAsync(monitorGroup);

        // Act
        var result = await _controller.DeleteMonitorGroup(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("monitorGroups.hasItemsFound", badRequestResult.Value);
    }

    [Fact]
    public async Task DeleteMonitorGroup_Valid_ReturnsOk()
    {
        // Arrange
        var validToken = "Bearer valid.token.here";
        var monitorGroup = new MonitorGroup
        {
            Id = 1,
            Name = "Name",
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.Request.Headers["Authorization"] = validToken;

        _mockMonitorGroupService.Setup(service => service.GetMonitorGroupById(It.IsAny<int>()))
            .ReturnsAsync(monitorGroup);

        // Act
        var result = await _controller.DeleteMonitorGroup(1);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveMonitorFromGroup_ReturnsOk()
    {
        // Arrange
        var monitorGroupItems = new MonitorGroupItems();

        // Act
        var result = await _controller.RemoveMonitorFromGroup(monitorGroupItems);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockMonitorGroupService.Verify(service => service.RemoveMonitorFromGroup(monitorGroupItems), Times.Once);
    }
}