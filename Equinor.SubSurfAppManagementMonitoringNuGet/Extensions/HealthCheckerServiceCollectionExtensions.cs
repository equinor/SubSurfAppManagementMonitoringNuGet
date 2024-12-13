using Equinor.SubSurfAppManagementMonitoringNuget.Helpers;
using Equinor.SubSurfAppManagementMonitoringNuget.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Equinor.SubSurfAppManagementMonitoringNuget.Extensions;

public static class HealthCheckerServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default health checker service and integrates it with the health check builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IHealthChecksBuilder AddDefaultHealthChecker(this IServiceCollection services)
    {
        var healthChecksBuilder = services.AddHealthChecks();
        return healthChecksBuilder;
    }
}