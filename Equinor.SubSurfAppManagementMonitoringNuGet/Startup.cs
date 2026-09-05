using Equinor.SubSurfAppManagementMonitoringNuGet.Config;
using Equinor.SubSurfAppManagementMonitoringNuGet.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Equinor.SubSurfAppManagementMonitoringNuGet;

public static class Startup
{
    /// <summary>
    /// Configures the default services and dependencies for the nuget package.
    /// </summary>
    /// <param name="services"></param>
    /// <remarks>
    /// This legacy path only wires <see cref="AuditActivityProcessor"/> as a raw OpenTelemetry
    /// <c>BaseProcessor&lt;Activity&gt;</c>; it does not configure an exporter, so callers must have already
    /// wired their own <c>UseAzureMonitor()</c> or classic Application Insights SDK. It does not read the
    /// activity tags this processor sets, meaning audit enrichment silently never reaches Application Insights
    /// for apps still on the classic <c>Microsoft.ApplicationInsights.AspNetCore</c> SDK - this is the exact bug
    /// <see cref="MonitoringServiceCollectionExtensions.AddSamMonitoring"/> was introduced to fix.
    /// </remarks>
    [Obsolete(
        "Use AddSamMonitoring() instead, which also configures the Azure Monitor exporter and correctly "
            + "reads audit enrichment tags. Do not call both in the same application: AuditActivityProcessor "
            + "would then double-tag every activity alongside AddSamMonitoring()'s enrichment."
    )]
    public static void Configure(IServiceCollection services)
    {
        ConfigureDependencyInjection(services);
    }

    private static void ConfigureDependencyInjection(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
#pragma warning disable CS0618 // Type or member is obsolete - this legacy method's whole purpose is wiring it.
        services.AddOpenTelemetry()
            .WithTracing(builder => builder.AddProcessor<AuditActivityProcessor>());
#pragma warning restore CS0618
    }

}