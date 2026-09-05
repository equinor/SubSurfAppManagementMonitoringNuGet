using Microsoft.Extensions.DependencyInjection;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Legacy entry point wrapping <see cref="Startup.Configure"/>.
    /// </summary>
    [Obsolete(
        "Use AddSamMonitoring() instead, which also configures the Azure Monitor exporter and correctly "
            + "reads audit enrichment tags. Do not call both in the same application."
    )]
    public static IServiceCollection AddSubSurfAppManagementMonitoring(this IServiceCollection services)
    {
#pragma warning disable CS0618 // Type or member is obsolete - this method itself is the legacy entry point.
        Startup.Configure(services);
#pragma warning restore CS0618
        return services;
    }
}
