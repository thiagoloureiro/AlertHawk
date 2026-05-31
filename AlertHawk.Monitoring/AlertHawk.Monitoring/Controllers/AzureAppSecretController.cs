using AlertHawk.Authentication.Domain.Custom;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AzureAppSecretController : ControllerBase
{
    private readonly IAzureAppSecretService _azureAppSecretService;
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly IMonitorService _monitorService;

    public AzureAppSecretController(
        IAzureAppSecretService azureAppSecretService,
        IAzureSecretsSettingsProvider settingsProvider,
        IMonitorService monitorService)
    {
        _azureAppSecretService = azureAppSecretService;
        _settingsProvider = settingsProvider;
        _monitorService = monitorService;
    }

    [HttpGet("registrations")]
    [SwaggerOperation(Summary = "List app registrations registered for monitoring")]
    [ProducesResponseType(typeof(IEnumerable<AzureAppRegistrationWatch>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegistrations()
    {
        return Ok(await _azureAppSecretService.GetRegistrationsAsync());
    }

    [HttpGet("discover")]
    [SwaggerOperation(Summary = "Discover app registrations from Azure AD tenant")]
    [ProducesResponseType(typeof(IEnumerable<AzureAppRegistrationSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DiscoverApplications()
    {
        return Ok(await _azureAppSecretService.DiscoverApplicationsAsync());
    }

    [HttpPost("registrations")]
    [SwaggerOperation(Summary = "Register an app registration for secret expiry monitoring")]
    [ProducesResponseType(typeof(AzureAppRegistrationWatch), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterApplication([FromBody] RegisterAzureAppRegistrationRequest request)
    {
        var result = await _azureAppSecretService.RegisterApplicationAsync(request);
        return Ok(result);
    }

    [HttpDelete("registrations/{id}")]
    [SwaggerOperation(Summary = "Unregister an app registration from monitoring")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UnregisterApplication(int id)
    {
        await _azureAppSecretService.UnregisterApplicationAsync(id);
        return Ok();
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List secrets for registered app registrations")]
    [ProducesResponseType(typeof(IEnumerable<AzureAppSecret>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecrets([FromQuery] bool expiringOnly = false)
    {
        var result = await _azureAppSecretService.GetSecretsAsync(expiringOnly);
        return Ok(result);
    }

    [HttpGet("status")]
    [SwaggerOperation(Summary = "Azure app secrets monitoring status summary")]
    [ProducesResponseType(typeof(AzureSecretsStatusSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        return Ok(await _azureAppSecretService.GetStatusAsync());
    }

    [HttpGet("config")]
    [SwaggerOperation(Summary = "Get Azure secrets monitoring configuration")]
    [ProducesResponseType(typeof(AzureSecretsConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig()
    {
        return Ok(await _settingsProvider.GetConfigDtoAsync());
    }

    [HttpPut("config")]
    [SwaggerOperation(Summary = "Update Azure secrets monitoring configuration (admin)")]
    [ProducesResponseType(typeof(AzureSecretsConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateConfig([FromBody] AzureSecretsConfigUpdateDto update)
    {
        if (!await IsUserAdmin())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new Message("Admin access required to update Azure secrets configuration."));
        }

        await _settingsProvider.UpdateConfigAsync(update);
        return Ok(await _settingsProvider.GetConfigDtoAsync());
    }

    [HttpPost("sync")]
    [SwaggerOperation(Summary = "Trigger Azure secrets sync immediately (admin)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Sync()
    {
        if (!await IsUserAdmin())
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new Message("Admin access required to trigger sync."));
        }

        await _azureAppSecretService.TriggerSyncAsync();
        return Ok(new { message = "Azure secrets sync completed." });
    }

    [HttpGet("history/{days}")]
    [SwaggerOperation(Summary = "Check history for the Azure secrets anchor monitor")]
    [ProducesResponseType(typeof(IEnumerable<MonitorHistory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(int days = 7)
    {
        return Ok(await _azureAppSecretService.GetHistoryAsync(days));
    }

    [HttpGet("alerts/{days}")]
    [SwaggerOperation(Summary = "Alerts for the Azure secrets anchor monitor")]
    [ProducesResponseType(typeof(IEnumerable<MonitorAlert>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(int days = 30)
    {
        var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
        if (jwtToken == null)
        {
            return BadRequest("Invalid Token");
        }

        return Ok(await _azureAppSecretService.GetAlertsAsync(days, jwtToken));
    }

    [HttpGet("monitor")]
    [SwaggerOperation(Summary = "Get the anchor monitor used for notifications")]
    [ProducesResponseType(typeof(Domain.Entities.Monitor), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMonitor()
    {
        var monitor = await _azureAppSecretService.GetAnchorMonitorAsync();
        return monitor == null ? NotFound() : Ok(monitor);
    }

    [HttpPost("monitor")]
    [SwaggerOperation(Summary = "Create anchor monitor for Azure secrets notifications")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateMonitor([FromBody] AzureAppSecretMonitorRequest request)
    {
        var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
        var monitorId = await _azureAppSecretService.CreateAnchorMonitorAsync(request, jwtToken);
        return Ok(monitorId);
    }

    [HttpPut("monitor")]
    [SwaggerOperation(Summary = "Update anchor monitor settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMonitor([FromBody] AzureAppSecretMonitorUpdateRequest request)
    {
        var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
        await _azureAppSecretService.UpdateAnchorMonitorAsync(request, jwtToken);
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
