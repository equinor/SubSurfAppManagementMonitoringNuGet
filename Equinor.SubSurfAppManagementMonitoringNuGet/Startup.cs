using Equinor.SubSurfAppManagementMonitoringNuGet.Controllers;
using Equinor.SubSurfAppManagementMonitoringNuGet.Helpers;
using Equinor.SubSurfAppManagementMonitoringNuGet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Equinor.SubSurfAppManagementMonitoringNuGet;

public static class Startup
{
    /// <summary>
    /// Configure all services for the nuget package
    /// </summary>
    /// <param name="services"></param>
    /// <param name="Configuration"></param>
    public static void Configure(IServiceCollection services, IConfiguration Configuration)
    {
        ConfigureServices(services, Configuration);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEnvironment, EnvironmentWrapper>();
        services.AddTransient<IHealthCheckerService, DefaultHealthCheckerService>();
        services.AddMvc().AddApplicationPart(typeof(HealthController).Assembly);
    }
}