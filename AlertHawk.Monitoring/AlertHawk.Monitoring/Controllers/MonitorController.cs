using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System.Text;
using k8s;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MonitorController : ControllerBase
    {
        private readonly IMonitorService _monitorService;
        private readonly IMonitorAgentService _monitorAgentService;
        private readonly IMonitorGroupService _monitorGroupService;

        public MonitorController(IMonitorService monitorService, IMonitorAgentService monitorAgentService,
            IMonitorGroupService monitorGroupService)
        {
            _monitorService = monitorService;
            _monitorAgentService = monitorAgentService;
            _monitorGroupService = monitorGroupService;
        }

        [SwaggerOperation(Summary = "Get MonitorById")]
        [ProducesResponseType(typeof(Domain.Entities.Monitor), StatusCodes.Status200OK)]
        [HttpGet("monitor/{id}")]
        public async Task<IActionResult> GetMonitorById(int id)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (string.IsNullOrEmpty(jwtToken))
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorService.GetMonitorById(id);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves detailed status for the current monitor Agent")]
        [ProducesResponseType(typeof(MonitorStatusDashboard), StatusCodes.Status200OK)]
        [HttpGet("monitorStatusDashboard/{environment}")]
        public async Task<IActionResult> GetMonitorStatusDashboard(
            MonitorEnvironment environment = MonitorEnvironment.Production)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (string.IsNullOrEmpty(jwtToken))
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorService.GetMonitorStatusDashboard(jwtToken, environment);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves detailed status for the current monitor Agent")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet("monitorAgentStatus")]
        public IActionResult GetMonitorAgentStatus()
        {
            return Ok(
                $"Master Node: {GlobalVariables.MasterNode}, MonitorId: {GlobalVariables.NodeId}, HttpTasksList Count: {GlobalVariables.HttpTaskList?.Count()}, TcpTasksList Count: {GlobalVariables.TcpTaskList?.Count()}");
        }

        [SwaggerOperation(Summary = "Retrieves a List of items to be Monitored")]
        [ProducesResponseType(typeof(IEnumerable<Domain.Entities.Monitor>), StatusCodes.Status200OK)]
        [HttpGet("monitorList")]
        public async Task<IActionResult> GetMonitorList()
        {
            var result = await _monitorService.GetMonitorList();
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a List of items to be Monitored, filtered by Tag")]
        [ProducesResponseType(typeof(IEnumerable<Domain.Entities.Monitor>), StatusCodes.Status200OK)]
        [HttpGet("monitorListByTag/{tag}")]
        public async Task<IActionResult> GetMonitorListByTag(string tag)
        {
            var result = await _monitorService.GetMonitorListByTag(tag);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a List (string) of monitor Tags")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [HttpGet("monitorTagList")]
        public async Task<IActionResult> GetMonitorTagList()
        {
            var result = await _monitorService.GetMonitorTagList();
            return Ok(result);
        }

        [SwaggerOperation(Summary =
            "Retrieves a List of items to be Monitored by Monitor Group Ids (JWT Token required)")]
        [ProducesResponseType(typeof(IEnumerable<Domain.Entities.Monitor>), StatusCodes.Status200OK)]
        [HttpGet("monitorListByMonitorGroupIds/{environment}")]
        public async Task<IActionResult> GetMonitorListByMonitorGroupIds(
            MonitorEnvironment environment = MonitorEnvironment.Production)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorService.GetMonitorListByMonitorGroupIds(jwtToken, environment);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a List of all Monitor Agents")]
        [ProducesResponseType(typeof(IEnumerable<MonitorAgent>), StatusCodes.Status200OK)]
        [HttpGet("allMonitorAgents")]
        public async Task<IActionResult> GetAllMonitorAgents()
        {
            var result = await _monitorAgentService.GetAllMonitorAgents();
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Create a new monitor Http")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("createMonitorHttp")]
        public async Task<IActionResult> CreateMonitorHttp([FromBody] MonitorHttp monitorHttp)
        {
            var monitorId = await _monitorService.CreateMonitorHttp(monitorHttp);
            await _monitorGroupService.AddMonitorToGroup(new MonitorGroupItems
            {
                MonitorId = monitorId,
                MonitorGroupId = monitorHttp.MonitorGroup
            });
            return Ok(monitorId);
        }

        [SwaggerOperation(Summary = "Clone a Monitor")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("clone/{id}")]
        public async Task<IActionResult> CloneMonitor(int id)
        {
            var monitor = await _monitorService.GetMonitorById(id);
            monitor.Name += "-clone";

            if (monitor.MonitorHttpItem != null)
            {
                monitor.MonitorHttpItem.Name += "-clone";
                var monitorId = await _monitorService.CreateMonitorHttp(monitor.MonitorHttpItem);
                await _monitorGroupService.AddMonitorToGroup(new MonitorGroupItems
                {
                    MonitorId = monitorId,
                    MonitorGroupId = monitor.MonitorGroup
                });
            }
            else if (monitor.MonitorTcpItem != null)
            {
                monitor.MonitorTcpItem.Name += "-clone";
                var monitorId = await _monitorService.CreateMonitorTcp(monitor.MonitorTcpItem);
                await _monitorGroupService.AddMonitorToGroup(new MonitorGroupItems
                {
                    MonitorId = monitorId,
                    MonitorGroupId = monitor.MonitorGroup
                });
            }
            else if (monitor.MonitorK8sItem != null)
            {
                monitor.MonitorK8sItem.Name += "-clone";
                var monitorId = await _monitorService.CreateMonitorK8s(monitor.MonitorK8sItem);
                await _monitorGroupService.AddMonitorToGroup(new MonitorGroupItems
                {
                    MonitorId = monitorId,
                    MonitorGroupId = monitor.MonitorGroup
                });
            }
            else
            {
                return BadRequest();
            }

            return Ok();
        }

        [SwaggerOperation(Summary = "Update monitor Http")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("updateMonitorHttp")]
        public async Task<IActionResult> UpdateMonitorHttp([FromBody] MonitorHttp monitorHttp)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (!await VerifyUserGroupPermissions(monitorHttp.Id, jwtToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            await _monitorService.UpdateMonitorHttp(monitorHttp);
            return Ok();
        }
        
        [SwaggerOperation(Summary = "Update monitor K8s")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("UpdateMonitorK8s")]
        public async Task<IActionResult> UpdateMonitorK8s([FromBody] MonitorK8s monitorK8S)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (!await VerifyUserGroupPermissions(monitorK8S.Id, jwtToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            await _monitorService.UpdateMonitorK8s(monitorK8S);
            return Ok();
        }

        [SwaggerOperation(Summary = "Create a new monitor TCP")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("createMonitorTcp")]
        public async Task<IActionResult> CreateMonitorTcp([FromBody] MonitorTcp monitorTcp)
        {
            var monitorId = await _monitorService.CreateMonitorTcp(monitorTcp);
            await _monitorGroupService.AddMonitorToGroup(new MonitorGroupItems
            {
                MonitorId = monitorId,
                MonitorGroupId = monitorTcp.MonitorGroup
            });
            return Ok(monitorId);
        }

        [SwaggerOperation(Summary = "Create a new monitor K8S")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("createMonitorK8s")]
        public async Task<IActionResult> CreateMonitorK8s([FromBody] MonitorK8s monitorK8s)
        {
            if (!string.IsNullOrEmpty(monitorK8s.Base64Content))
            {
                var fileBytes = Convert.FromBase64String(monitorK8s.Base64Content); // Decode base64 string
                var filePath = Path.Combine("kubeconfig", "config.yaml"); // Define file path

                Directory.CreateDirectory("kubeconfig"); // Ensure directory exists

                await System.IO.File.WriteAllBytesAsync(filePath, fileBytes); // Write decoded bytes to file
                
                monitorK8s.KubeConfig = monitorK8s.Base64Content;
            }

            var monitorId = await _monitorService.CreateMonitorK8s(monitorK8s);
            await _monitorGroupService.AddMonitorToGroup(new MonitorGroupItems
            {
                MonitorId = monitorId,
                MonitorGroupId = monitorK8s.MonitorGroup
            });
            return Ok(monitorId);
        }

        [SwaggerOperation(Summary = "Update monitor TCP")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("updateMonitorTcp")]
        public async Task<IActionResult> UpdateMonitorTcp([FromBody] MonitorTcp monitorTcp)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (!await VerifyUserGroupPermissions(monitorTcp.Id, jwtToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            await _monitorService.UpdateMonitorTcp(monitorTcp);
            return Ok();
        }

        [SwaggerOperation(Summary = "Delete Monitor")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpDelete("deleteMonitor/{id}")]
        public async Task<IActionResult> DeleteMonitor(int id)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());

            if (!await VerifyUserGroupPermissions(id, jwtToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            await _monitorService.DeleteMonitor(id, jwtToken);
            return Ok();
        }

        private async Task<bool> VerifyUserGroupPermissions(int id, string? jwtToken)
        {
            var userGroups = await _monitorGroupService.GetMonitorGroupList(jwtToken);
            var monitorGroupId = await _monitorGroupService.GetMonitorGroupIdByMonitorId(id);
            if (userGroups == null || userGroups.All(x => x.Id != monitorGroupId))
            {
                return false;
            }

            return true;
        }

        [SwaggerOperation(Summary = "Pause or resume the monitoring for the specified monitorId")]
        [HttpPut("pauseMonitor/{id}/{paused}")]
        public async Task<IActionResult> PauseMonitor(int id, bool paused)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());

            if (!await VerifyUserGroupPermissions(id, jwtToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            await _monitorService.PauseMonitor(id, paused);
            return Ok();
        }

        [SwaggerOperation(Summary = "Pause or resume the monitoring for the specified Monitor Group Id")]
        [HttpPut("pauseMonitorByGroupId/{groupId}/{paused}")]
        public async Task<IActionResult> PauseMonitorByGroupId(int groupId, bool paused)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());

            var userGroups = await _monitorGroupService.GetMonitorGroupList(jwtToken);

            if (userGroups == null || userGroups.All(x => x.Id != groupId))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            await _monitorService.PauseMonitorByGroupId(groupId, paused);
            return Ok();
        }

        [SwaggerOperation(Summary = "Retrieves Failure Count")]
        [ProducesResponseType(typeof(IEnumerable<MonitorAgent>), StatusCodes.Status200OK)]
        [HttpGet("getMonitorFailureCount/{days}")]
        public async Task<IActionResult> GetMonitorFailureCount(int days)
        {
            var result = await _monitorService.GetMonitorFailureCount(days);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves monitor http by monitorId")]
        [ProducesResponseType(typeof(MonitorHttp), StatusCodes.Status200OK)]
        [HttpGet("getMonitorHttpByMonitorId/{monitorId}")]
        public async Task<IActionResult> GetMonitorHttpByMonitorId(int monitorId)
        {
            var result = await _monitorService.GetHttpMonitorByMonitorId(monitorId);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves monitor tcp by monitorId")]
        [ProducesResponseType(typeof(MonitorTcp), StatusCodes.Status200OK)]
        [HttpGet("getMonitorTcpByMonitorId/{monitorId}")]
        public async Task<IActionResult> GetMonitorTcpByMonitorId(int monitorId)
        {
            var result = await _monitorService.GetTcpMonitorByMonitorId(monitorId);
            return Ok(result);
        }
        
        [SwaggerOperation(Summary = "Retrieves monitor tcp by monitorId")]
        [ProducesResponseType(typeof(MonitorTcp), StatusCodes.Status200OK)]
        [HttpGet("getMonitorK8sByMonitorId/{monitorId}")]
        public async Task<IActionResult> getMonitorK8sByMonitorId(int monitorId)
        {
            var result = await _monitorService.GetK8sMonitorByMonitorId(monitorId);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Monitor Count")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet("GetMonitorCount")]
        public async Task<IActionResult> GetMonitorCount()
        {
            var monitorList = await _monitorService.GetMonitorList();
            return Ok(monitorList.Count());
        }

        [SwaggerOperation(Summary = "Monitor Backup Json")]
        [ProducesResponseType(typeof(File), StatusCodes.Status200OK)]
        [HttpGet("GetMonitorJsonBackup")]
        public async Task<IActionResult> GetMonitorBackupJson()
        {
            var isAdmin = await IsUserAdmin();

            if (!isAdmin)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            var json = await _monitorService.GetMonitorBackupJson();
            var byteArray = Encoding.UTF8.GetBytes(json);

            return File(byteArray, "application/json", "MonitorBackup.json");
        }

        [SwaggerOperation(Summary = "UploadMonitor Backup Json")]
        [ProducesResponseType(typeof(File), StatusCodes.Status200OK)]
        [HttpPost("UploadMonitorJsonBackup")]
        public async Task<IActionResult> UploadMonitorJsonBackup(IFormFile? file)
        {
            var isAdmin = await IsUserAdmin();

            if (!isAdmin)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Message("This user is not authorized to do this operation"));
            }

            if (file == null || file.Length == 0)
                return BadRequest("Upload a valid JSON file.");

            MonitorBackup? data;

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                var json = await stream.ReadToEndAsync();
                data = JsonConvert.DeserializeObject<MonitorBackup>(json);
            }

            if (data == null)
            {
                return BadRequest("Invalid file/format");
            }

            await _monitorService.UploadMonitorJsonBackup(data);

            return Ok();
        }

        private async Task<bool> IsUserAdmin()
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (string.IsNullOrEmpty(jwtToken))
            {
                return false;
            }

            var user = await _monitorService.GetUserDetailsByToken(jwtToken);

            return user != null && user.IsAdmin;
        }
    }
}