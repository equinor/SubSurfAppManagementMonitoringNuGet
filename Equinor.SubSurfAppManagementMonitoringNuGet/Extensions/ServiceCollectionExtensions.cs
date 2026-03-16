using Microsoft.Extensions.DependencyInjection;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSubSurfAppManagementMonitoring(this IServiceCollection services)
    {
        Startup.Configure(services);
        return services;
    }
}
