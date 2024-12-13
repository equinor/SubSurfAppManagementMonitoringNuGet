using Asp.Versioning;
using Equinor.SubSurfAppManagementMonitoringNuGet.Config;
using Equinor.SubSurfAppManagementMonitoringNuget.Models;
using Equinor.SubSurfAppManagementMonitoringNuget.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    
    private readonly ApplicationConfig _applicationConfig;

    public HealthController(IHealthCheckerService healthCheckerService, IOptions<ApplicationConfig> applicationConfig)
    {
        _healthCheckerService = healthCheckerService;
        _applicationConfig = applicationConfig.Value;
    }

    /// <summary>
    /// Checks the health of the API
    /// </summary>
    /// <returns></returns>
    public async Task<ApplicationHealth> Get()
    {
        var applicationHealth = await _healthCheckerService.CheckHealthAsync(_applicationConfig.AppName);

        return applicationHealth;
    }

}