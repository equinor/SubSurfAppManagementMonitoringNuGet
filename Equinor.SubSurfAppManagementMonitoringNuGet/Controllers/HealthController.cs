using Asp.Versioning;
using Equinor.SubSurfAppManagementMonitoringNuGet.Models;
using Equinor.SubSurfAppManagementMonitoringNuGet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Controllers;

/// <summary>
/// Controller for healthchecks
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckerService _healthCheckerService;
    
    private readonly HealthControllerOptions _options;

    
    public HealthController(IHealthCheckerService healthCheckerService, HealthControllerOptions options)
    {
        _healthCheckerService = healthCheckerService;
        _options = options;
    }

    /// <summary>
    /// Checks the health of the API
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ApplicationHealth> Get() 
    {
        var applicationHealth = await _healthCheckerService.CheckHealthAsync(_options.AppName);

        return applicationHealth;
    }

}