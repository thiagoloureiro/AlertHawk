using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using EasyMemoryCache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AlertHawk.Monitoring.Tests.ControllerTests;

public class MonitorControllerTests
{
    [Fact]
    public async Task GetMonitorStatusDashboard_ReturnsOkResult()
    {
        // Arrange
        var jwtToken = "validJwtToken";
        var expectedDashboardData = new MonitorStatusDashboard
        {
            MonitorDown = 1,
            MonitorPaused = 1,
            MonitorUp = 1
        };

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        monitorServiceMock.GetMonitorStatusDashboard(jwtToken).Returns(Task.FromResult(expectedDashboardData));

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Headers = { ["Authorization"] = "Bearer " + jwtToken } }
            }
        };

        // Act
        var result = await controller.GetMonitorStatusDashboard();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedDashboardData, okResult.Value);
    }
    
    [Fact]
    public async Task GetMonitorStatusDashboard_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var jwtToken = "invalidJwtToken";

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        
        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Headers = { ["Authorization"] = "zzzz " + jwtToken } }
            }
        };

        // Act
        var result = await controller.GetMonitorStatusDashboard();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", badRequestResult.Value);
    }
    
    [Fact]
    public static void GetMonitorAgentStatus_Returns_OkResult_With_Expected_Message()
    {
        // Arrange
        GlobalVariables.NodeId = 1;
        GlobalVariables.HttpTaskList = new List<int>();
        GlobalVariables.TcpTaskList = new List<int>();
        GlobalVariables.MasterNode = true;

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        
        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = controller.GetMonitorAgentStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var message = Assert.IsAssignableFrom<string>(okResult.Value);
        Assert.Contains("Master Node: True", message);
        Assert.Contains("MonitorId: 1", message);
        Assert.Contains("HttpTasksList Count: 0", message); // Assuming no items in the list
        Assert.Contains("TcpTasksList Count: 0", message); // Assuming no items in the list
    }
}