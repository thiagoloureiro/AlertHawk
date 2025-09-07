using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Monitoring.Controllers;
using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
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

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Request = { Headers = { ["Authorization"] = "Bearer " + jwtToken } }
                }
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
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
            UrlToCheck = "http://urltocheck.com",
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        monitorServiceMock.CreateMonitorHttp(monitorHttp).Returns(Task.FromResult(1));

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
            UrlToCheck = "http://urltocheck.com",
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0,
            Id = 1
        };
        var token = "Bearer valid.token.here";
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Id = 1,
                Name = "Name",
                Monitors   = new List<Monitor>
                {
                    new Monitor
                    {
                        Id = 1,
                        Name = "Name",
                        HeartBeatInterval = 0,
                        Retries = 0
                    }
                }
            }
        };

        var monitorId = 1;

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["Authorization"] = token;

        monitorGroupServiceMock.GetMonitorGroupList("valid.token.here").Returns(monitorGroups);
        monitorGroupServiceMock.GetMonitorGroupIdByMonitorId(monitorId).Returns(monitorId);

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
            IP = "1.1.1.1",
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0,
            Id = 1
        };

        var token = "Bearer valid.token.here";
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Id = 1,
                Name = "Name",
                Monitors   = new List<Monitor>
                {
                    new Monitor
                    {
                        Id = 1,
                        Name = "Name",
                        HeartBeatInterval = 0,
                        Retries = 0
                    }
                }
            }
        };

        var monitorId = 1;

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();
        monitorServiceMock.CreateMonitorTcp(monitorTcp).Returns(Task.FromResult(1));

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["Authorization"] = token;

        monitorGroupServiceMock.GetMonitorGroupList("valid.token.here").Returns(monitorGroups);
        monitorGroupServiceMock.GetMonitorGroupIdByMonitorId(monitorId).Returns(monitorId);

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
            IP = "1.1.1.1",
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0,
            Id = 1
        };

        var jwtToken = "Bearer valid.token.here";
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Id = 1,
                Name = "Name",
                Monitors   = new List<Monitor>
                {
                    new Monitor
                    {
                        Id = 1,
                        Name = "Name",
                        HeartBeatInterval = 0,
                        Retries = 0
                    }
                }
            }
        };

        var monitorId = 1;

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["Authorization"] = jwtToken;
        monitorGroupServiceMock.GetMonitorGroupList("valid.token.here").Returns(monitorGroups);
        monitorGroupServiceMock.GetMonitorGroupIdByMonitorId(monitorId).Returns(monitorId);

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
        var jwtToken = "Bearer valid.token.here";
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Id = 1,
                Name = "Name",
                Monitors   = new List<Monitor>
                {
                    new Monitor
                    {
                        Id = 1,
                        Name = "Name",
                        HeartBeatInterval = 0,
                        Retries = 0
                    }
                }
            }
        };

        var monitorId = 1;

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["Authorization"] = jwtToken;
        monitorGroupServiceMock.GetMonitorGroupList("valid.token.here").Returns(monitorGroups);
        monitorGroupServiceMock.GetMonitorGroupIdByMonitorId(monitorId).Returns(monitorId);

        // Act
        var result = await controller.DeleteMonitor(1);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        await monitorServiceMock.Received(1).DeleteMonitor(1, "valid.token.here");
    }

    [Fact]
    public async Task PauseMonitor_ReturnsOkResult()
    {
        // Arrange
        var token = "Bearer valid.token.here";
        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Id = 1,
                Name = "Name",
                Monitors   = new List<Monitor>
                {
                    new Monitor
                    {
                        Id = 1,
                        Name = "Name",
                        HeartBeatInterval = 0,
                        Retries = 0
                    }
                }
            }
        };

        var monitorId = 1;

        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["Authorization"] = token;

        monitorGroupServiceMock.GetMonitorGroupList("valid.token.here").Returns(monitorGroups);
        monitorGroupServiceMock.GetMonitorGroupIdByMonitorId(monitorId).Returns(monitorId);

        // Act
        var result = await controller.PauseMonitor(monitorId, true);

        // Assert
        await monitorServiceMock.Received(1).PauseMonitor(monitorId, true);
    }

    [Fact]
    public async Task PauseMonitorByGroupId_ReturnsOkResult()
    {
        // Arrange
        var jwtToken = "Bearer valid.token.here";
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var monitorGroups = new List<MonitorGroup>
        {
            new MonitorGroup
            {
                Id = 1,
                Name = "Name",
                Monitors   = new List<Monitor>
                {
                    new Monitor
                    {
                        Id = 1,
                        Name = "Name",
                        HeartBeatInterval = 0,
                        Retries = 0
                    }
                }
            }
        };

        var monitorId = 1;

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["Authorization"] = jwtToken;
        monitorGroupServiceMock.GetMonitorGroupList("valid.token.here").Returns(monitorGroups);
        monitorGroupServiceMock.GetMonitorGroupIdByMonitorId(monitorId).Returns(monitorId);

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

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
            UrlToCheck = "http://url.com",
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        monitorServiceMock.GetHttpMonitorByMonitorId(1).Returns(expectedMonitorHttp);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
            IP = "1.1.1.1",
            Timeout = 0,
            HeartBeatInterval = 0,
            Retries = 0
        };
        monitorServiceMock.GetTcpMonitorByMonitorId(1).Returns(expectedMonitorTcp);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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
                Name = "Name",
                HeartBeatInterval = 0,
                Retries = 0
            },
            new Monitor
            {
                Name = "Name",
                HeartBeatInterval = 0,
                Retries = 0
            }
        }; // Assuming Monitor is your model
        monitorServiceMock.GetMonitorList().Returns(monitorList);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

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

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);
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
        var monitorServiceMock = new Mock<IMonitorService>();
        var expectedJson = "json";
        var token = "Bearer valid.token.here";
        var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: true);

        monitorServiceMock.Setup(x => x.GetUserDetailsByToken(It.IsAny<string>())).ReturnsAsync(user);

        monitorServiceMock.Setup(x => x.GetMonitorBackupJson()).ReturnsAsync(expectedJson);

        var monitorAgentServiceMock = new Mock<IMonitorAgentService>();
        var monitorGroupServiceMock = new Mock<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock.Object, monitorAgentServiceMock.Object, monitorGroupServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["Authorization"] = token;

        // Act
        var result = await controller.GetMonitorBackupJson();

        // Assert
        var fileResult = result as FileResult;

        Assert.NotNull(fileResult);
    }

    [Fact]
    public async Task GetMonitorBackupJson_Returns_Forbidden()
    {
        // Arrange
        var monitorServiceMock = new Mock<IMonitorService>();
        var expectedJson = "json";
        var token = "";
        var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: false);

        monitorServiceMock.Setup(x => x.GetUserDetailsByToken(It.IsAny<string>())).ReturnsAsync(user);

        monitorServiceMock.Setup(x => x.GetMonitorBackupJson()).ReturnsAsync(expectedJson);

        var monitorAgentServiceMock = new Mock<IMonitorAgentService>();
        var monitorGroupServiceMock = new Mock<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock.Object, monitorAgentServiceMock.Object, monitorGroupServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["Authorization"] = token;

        // Act
        var result = await controller.GetMonitorBackupJson();

        // Assert
        var fileResult = result as FileResult;

        Assert.Null(fileResult);
    }

    [Fact]
    public async Task UploadMonitorJsonBackup_Returns_BadRequestResult()
    {
        // Arrange
        var monitorServiceMock = new Mock<IMonitorService>();
        var monitorAgentServiceMock = new Mock<IMonitorAgentService>();
        var monitorGroupServiceMock = new Mock<IMonitorGroupService>();
        var mockFile = new Mock<IFormFile>();
        var token = "Bearer valid.token.here";
        var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: true);

        monitorServiceMock.Setup(x => x.GetUserDetailsByToken(It.IsAny<string>())).ReturnsAsync(user);

        var controller = new MonitorController(monitorServiceMock.Object, monitorAgentServiceMock.Object, monitorGroupServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["Authorization"] = token;

        // Act
        var result = await controller.UploadMonitorJsonBackup(mockFile.Object);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadMonitorJsonBackup_Returns_OkObjectResult()
    {
        // Arrange
        var monitorServiceMock = new Mock<IMonitorService>();
        var monitorAgentServiceMock = new Mock<IMonitorAgentService>();
        var monitorGroupServiceMock = new Mock<IMonitorGroupService>();
        var monitorBackup = new MonitorBackup
        {
            MonitorGroupList = new List<MonitorGroup>()
        };
        var token = "Bearer valid.token.here";
        var user = new UserDto(Id: Guid.NewGuid(), Username: "testuser", Email: "user@user.com", IsAdmin: true);

        var contentJson = JsonConvert.SerializeObject(monitorBackup);

        IFormFile mockFile = CreateMockIFormFile("test.txt", contentJson);

        monitorServiceMock.Setup(x => x.GetUserDetailsByToken(It.IsAny<string>())).ReturnsAsync(user);

        var controller = new MonitorController(monitorServiceMock.Object, monitorAgentServiceMock.Object, monitorGroupServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["Authorization"] = token;

        // Act
        var result = await controller.UploadMonitorJsonBackup(mockFile);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    private IFormFile CreateMockIFormFile(string fileName, string content)
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
    
    [Fact]
    public async Task GetMonitorK8sByMonitorId_ReturnsOkResult_With_Expected_Result()
    {
        // Arrange
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var expectedMonitorK8s = new MonitorK8s
        {
            Name = "Test Monitor",
            ClusterName = "TestCluster",
            KubeConfig = "base64Config",
            HeartBeatInterval = 0,
            Retries = 0
        };
        monitorServiceMock.GetK8sMonitorByMonitorId(1).Returns(expectedMonitorK8s);

        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        // Act
        var result = await controller.getMonitorK8sByMonitorId(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultMonitorK8s = Assert.IsAssignableFrom<MonitorK8s>(okResult.Value);
        Assert.Equal(expectedMonitorK8s, resultMonitorK8s);
    }
    
    [Fact]
    public async Task CreateMonitorK8s_Returns_OkResult_With_Expected_Result()
    {
        // Arrange
        var monitorK8s = new MonitorK8s
        {
            Name = "Test Monitor",
            ClusterName = "TestCluster",
            KubeConfig = "base64Config",
            HeartBeatInterval = 0,
            Retries = 0
        };
        var monitorServiceMock = Substitute.For<IMonitorService>();
        var monitorAgentServiceMock = Substitute.For<IMonitorAgentService>();
        monitorServiceMock.CreateMonitorK8s(monitorK8s).Returns(Task.FromResult(1));

        var monitorGroupServiceMock = Substitute.For<IMonitorGroupService>();

        var controller = new MonitorController(monitorServiceMock, monitorAgentServiceMock, monitorGroupServiceMock);

        // Act
        var result = await controller.CreateMonitorK8s(monitorK8s);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, okResult.Value);
    }
}