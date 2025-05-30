using Equinor.SubSurfAppManagementMonitoringNuGet.Config;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace Equinor.SubSurfAppManagementMonitoringNuGet;

public static class Startup
{
    /// <summary>
    /// Configures the default services and dependencies for the nuget package
    /// </summary>
    /// <param name="services"></param>
    public static void Configure(IServiceCollection services)
    {
        ConfigureDependencyInjection(services);
    }
    
    private static void ConfigureDependencyInjection(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ITelemetryInitializer, AuditTelemetryInitializer>();
    }

}