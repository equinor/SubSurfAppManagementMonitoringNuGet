using Equinor.SubSurfAppManagementMonitoringNuGet.Constants;
using Equinor.SubSurfAppManagementMonitoringNuGet.Helpers;
using Equinor.SubSurfAppManagementMonitoringNuGet.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Services;

/// <summary>
/// Provides an abstraction for executing health checks within an application.
/// </summary>
public interface IHealthCheckerService
{
    /// <summary>
    /// Executes all registered health checks and returns a comprehensive health report.
    /// </summary>
    /// <param name="appName">The name our your application</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains a <see cref="HealthReport"/> detailing the status of all health checks.</returns>
    Task<ApplicationHealth> CheckHealthAsync(string appName, CancellationToken cancellationToken = default);

}

public class DefaultHealthCheckerService : IHealthCheckerService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<DefaultHealthCheckerService> _logger;
    private readonly IEnvironment _env;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHealthCheckerService"/> class.
    /// </summary>
    /// <param name="healthCheckService">The underlying <see cref="HealthCheckService"/> 
    /// used to perform health checks.</param>
    /// <param name="logger"></param>
    /// <param name="env"></param>
    public DefaultHealthCheckerService(HealthCheckService healthCheckService, ILogger<DefaultHealthCheckerService> logger, IEnvironment env)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Executes all registered health checks and returns a detailed health report.
    /// </summary>
    /// <param name="appName">The name our your application</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains a <see cref="HealthReport"/> detailing the status of all health checks.</returns>
    public async Task<ApplicationHealth> CheckHealthAsync(string appName, CancellationToken cancellationToken = default)
    {
        var applicationHealth = new ApplicationHealth
        {
            ApplicationName = $"{appName}-{_env.EnvironmentName}",
            RequestDate = DateTimeOffset.Now.DateTime
        };

        var resources = new List<Resource>();
        var reports = await _healthCheckService.CheckHealthAsync(cancellationToken); 

        // Add application resource status
        var resourceApi = new Resource
        {
            ResourceName = _env.ApplicationName,
            Status = reports.Status.ToString(), 
            Message = HealthCheckDescriptions.Descriptions[HealthCheckNames.Api]
        };
        resources.Add(resourceApi);
        
        foreach (var report in reports.Entries)
        {
            if (report.Value.Exception is not null)
            {
                _logger.LogError(message: report.Value.Exception.Message, exception: report.Value.Exception);
            }

            HealthCheckDescriptions.Descriptions.TryGetValue(report.Key, out string? description);

            var resource = new Resource
            {
                Status = report.Value.Status.ToString(),
                ResourceName = report.Key,
                Message = description ?? "No description available."
            };

            resources.Add(resource);
        }
        applicationHealth.SetResouces(resources);
        
        return applicationHealth;
    }
}
