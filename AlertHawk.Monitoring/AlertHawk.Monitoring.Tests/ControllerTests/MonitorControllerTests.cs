using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using NSubstitute;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

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
        monitorServiceMock.GetMonitorStatusDashboard(jwtToken, MonitorEnvironment.Production)
            .Returns(Task.FromResult(expectedDashboardData));

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

    [Fact]
    public async Task GetMonitorList_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorService = Substitute.For<IMonitorService>();
        var expectedList = new List<Monitor>(); // Assuming Monitor is your model
        monitorService.GetMonitorList().Returns(expectedList);

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorList();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultList = Assert.IsAssignableFrom<IEnumerable<Monitor>>(okResult.Value);
        Assert.Equal(expectedList, resultList);
    }

    [Fact]
    public async Task GetMonitorListByTag_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorService = Substitute.For<IMonitorService>();
        var expectedList = new List<Monitor>(); // Assuming Monitor is your model
        monitorService.GetMonitorListByTag("tag").Returns(expectedList);

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorListByTag("tag");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultList = Assert.IsAssignableFrom<IEnumerable<Monitor>>(okResult.Value);
        Assert.Equal(expectedList, resultList);
    }

    [Fact]
    public async Task GetMonitorTagList_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorService = Substitute.For<IMonitorService>();
        var expectedList = new List<string>(); // Assuming string is your model
        monitorService.GetMonitorTagList().Returns(expectedList);

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorTagList();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultList = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.Equal(expectedList, resultList);
    }

    [Fact]
    public async Task GetAllMonitorAgents_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorService = Substitute.For<IMonitorAgentService>();
        var expectedList = new List<MonitorAgent>(); // Assuming MonitorAgent is your model
        monitorService.GetAllMonitorAgents().Returns(expectedList);

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetAllMonitorAgents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultList = Assert.IsAssignableFrom<IEnumerable<MonitorAgent>>(okResult.Value);
        Assert.Equal(expectedList, resultList);
    }

    [Fact]
    public async Task GetMonitorListByMonitorGroupIds_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var jwtToken = "validJwtToken";
        var expectedList = new List<Monitor>();

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        monitorServiceMock.GetMonitorListByMonitorGroupIds(jwtToken, MonitorEnvironment.Production)
            .Returns(expectedList);

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Headers = { ["Authorization"] = "Bearer " + jwtToken } }
            }
        };

        // Act
        var result = await controller.GetMonitorListByMonitorGroupIds(MonitorEnvironment.Production);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultList = Assert.IsAssignableFrom<IEnumerable<Monitor>>(okResult.Value);
        Assert.Equal(expectedList, resultList);
    }

    [Fact]
    public async Task CreateMonitorHttp_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorHttp = new MonitorHttp
        {
            Name = "Test Monitor",
            MaxRedirects = 0,
            UrlToCheck = null,
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        monitorServiceMock.CreateMonitorHttp(monitorHttp).Returns(Task.FromResult(1));

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.CreateMonitorHttp(monitorHttp);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, okResult.Value);
    }

    [Fact]
    public async Task UpdateMonitorHttp_Returns_OkResult()
    {
        // Arrange
        var monitorHttp = new MonitorHttp
        {
            Name = "Test Monitor",
            MaxRedirects = 0,
            UrlToCheck = null,
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.UpdateMonitorHttp(monitorHttp);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        await monitorServiceMock.Received(1).UpdateMonitorHttp(monitorHttp);
    }

    [Fact]
    public async Task CreateMonitorTcp_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorTcp = new MonitorTcp
        {
            Name = "Test Monitor",
            Port = 0,
            IP = null,
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        monitorServiceMock.CreateMonitorTcp(monitorTcp).Returns(Task.FromResult(1));

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.CreateMonitorTcp(monitorTcp);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, okResult.Value);
    }

    [Fact]
    public async Task UpdateMonitorTcp_Returns_OkResult()
    {
        // Arrange
        var monitorTcp = new MonitorTcp
        {
            Name = "Test Monitor",
            Port = 0,
            IP = null,
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.UpdateMonitorTcp(monitorTcp);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        await monitorServiceMock.Received(1).UpdateMonitorTcp(monitorTcp);
    }

    [Fact]
    public async Task DeleteMonitor_ReturnsOkResult()
    {
        // Arrange
        var jwtToken = "validJwtToken";
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Headers = { ["Authorization"] = "Bearer " + jwtToken } }
            }
        };

        // Act
        var result = await controller.DeleteMonitor(1);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        await monitorServiceMock.Received(1).DeleteMonitor(1, jwtToken);
    }

    [Fact]
    public async Task PauseMonitor_ReturnsOkResult()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.PauseMonitor(1, true);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        await monitorServiceMock.Received(1).PauseMonitor(1, true);
    }

    [Fact]
    public async Task PauseMonitorByGroupId_ReturnsOkResult()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.PauseMonitorByGroupId(1, true);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        await monitorServiceMock.Received(1).PauseMonitorByGroupId(1, true);
    }

    [Fact]
    public async Task GetMonitorFailureCount_ReturnsOkResult_With_Expected_Result()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var expectedList = new List<MonitorFailureCount>(); // Assuming MonitorAgent is your model
        monitorServiceMock.GetMonitorFailureCount(7).Returns(expectedList);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorFailureCount(7);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMonitorHttpByMonitorId_ReturnsOkResult_With_Expected_Result()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var expectedMonitorHttp = new MonitorHttp
        {
            Name = "Test Monitor",
            MaxRedirects = 0,
            UrlToCheck = null,
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        monitorServiceMock.GetHttpMonitorByMonitorId(1).Returns(expectedMonitorHttp);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorHttpByMonitorId(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultMonitorHttp = Assert.IsAssignableFrom<MonitorHttp>(okResult.Value);
        Assert.Equal(expectedMonitorHttp, resultMonitorHttp);
    }

    [Fact]
    public async Task GetMonitorTcpByMonitorId_ReturnsOkResult_With_Expected_Result()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var expectedMonitorTcp = new MonitorTcp
        {
            Name = "Test Monitor",
            Port = 0,
            IP = null,
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        monitorServiceMock.GetTcpMonitorByMonitorId(1).Returns(expectedMonitorTcp);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorTcpByMonitorId(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultMonitorTcp = Assert.IsAssignableFrom<MonitorTcp>(okResult.Value);
        Assert.Equal(expectedMonitorTcp, resultMonitorTcp);
    }

    [Fact]
    public async Task GetMonitorCount_ReturnsOkResult_With_Expected_Result()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorList = new List<Monitor>
        {
            new Monitor
            {
                Name = null,
                HeartBeatInterval = 0,
                Retries = 0
            },
            new Monitor
            {
                Name = null,
                HeartBeatInterval = 0,
                Retries = 0
            }
        }; // Assuming Monitor is your model
        monitorServiceMock.GetMonitorList().Returns(monitorList);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorCount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var count = Assert.IsAssignableFrom<int>(okResult.Value);
        Assert.Equal(monitorList.Count, count);
    }

    [Fact]
    public async Task GetMonitorAlertsByEnvironment_NullToken_ReturnsBadRequest()
    {
        // Arrange
        var invalidToken = string.Empty;
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        var monitorServiceMock = Substitute.For<IMonitorService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["Authorization"] = invalidToken;

        // Act
        var result = await controller.GetMonitorListByMonitorGroupIds(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid Token", badRequestResult.Value);
    }

    [Fact]
    public async Task GetMonitorBackupJson_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var expectedJson = "json";
        monitorServiceMock.GetMonitorBackupJson().Returns(expectedJson);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.GetMonitorBackupJson();

        // Assert
        var fileResult = result as FileResult;

        Assert.NotNull(fileResult);
    }

    [Fact]
    public async Task UploadMonitorJsonBackup_Returns_BadRequestResult()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        var mockFile = new Mock<IFormFile>();
        var monitorBackup = new MonitorBackup();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.UploadMonitorJsonBackup(mockFile.Object);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
    
    [Fact]
    public async Task UploadMonitorJsonBackup_Returns_OkObjectResult()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        var monitorBackup = new MonitorBackup
        {
            MonitorGroupList = new List<MonitorGroup>()
        };

        var contentJson = JsonConvert.SerializeObject(monitorBackup);
        
        IFormFile mockFile = CreateMockIFormFile("test.txt", contentJson);

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock);

        // Act
        var result = await controller.UploadMonitorJsonBackup(mockFile);

        // Assert
        Assert.IsType<OkResult>(result);
    }
    
    public IFormFile CreateMockIFormFile(string fileName, string content)
    {
        var fileMock = new Mock<IFormFile>();

        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);

        return fileMock.Object;
    }
}