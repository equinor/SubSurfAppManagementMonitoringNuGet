using Equinor.SubSurfAppManagementMonitoringNuGet.Controllers;
using Equinor.SubSurfAppManagementMonitoringNuGet.Helpers;
using Equinor.SubSurfAppManagementMonitoringNuGet.Models;
using Equinor.SubSurfAppManagementMonitoringNuGet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Extensions;

public static class HealthCheckerServiceCollectionExtensions
{
    /// <summary>
    /// Adds health checks and a health controller to the application, while configuring a custom health checker service.
    /// </summary>
    /// <typeparam name="T">The type of the custom health checker service that implements <see cref="IHealthCheckerService"/>.</typeparam>
    /// <param name="services">The service collection to add the health services to.</param>
    /// <param name="configuration">The application configuration used to configure the health services.</param>
    /// <returns>An <see cref="IHealthChecksBuilder"/> to further configure health checks.</returns>
    /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> does not implement <see cref="IHealthCheckerService"/>.</exception>
    public static IHealthChecksBuilder AddCustomHealthController<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class, IHealthCheckerService
    {
        services.AddMvc().AddApplicationPart(typeof(HealthController).Assembly);
        var healthChecksBuilder = services.ConfigureHealthServices<T>(configuration);
        return healthChecksBuilder;
    }

    /// <summary>
    /// Adds the default health checker service and integrates it with the health check builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration"></param>
    /// <param name="applicationName">The name of your application, will show up as the applicationName in the <see cref="ApplicationHealth"/> response</param>
    public static IHealthChecksBuilder AddDefaultHealthController(this IServiceCollection services, IConfiguration configuration, string applicationName)
    {
        services.AddSingleton<IEnvironment, EnvironmentWrapper>();
        services.AddSingleton(new HealthControllerOptions { AppName = applicationName });
        services.AddMvc().AddApplicationPart(typeof(HealthController).Assembly);
        var healthChecksBuilder = services.ConfigureHealthServices<DefaultHealthCheckerService>(configuration);
        return healthChecksBuilder;
    }
    
    /// <summary>
    /// Configures health-related services, including a custom health checker service and the health controller.
    /// </summary>
    /// <typeparam name="T">The type of the custom health checker service that implements <see cref="IHealthCheckerService"/>.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration used to configure the health services.</param>
    public static IHealthChecksBuilder ConfigureHealthServices<T>(this IServiceCollection services, IConfiguration configuration) 
        where T : class, IHealthCheckerService
    {
        services.AddTransient<IHealthCheckerService, T>();
        var healthChecksBuilder = services.AddHealthChecks();
        return healthChecksBuilder;
    }
    
    
}