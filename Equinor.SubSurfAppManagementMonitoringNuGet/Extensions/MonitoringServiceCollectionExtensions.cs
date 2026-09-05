using Ardalis.GuardClauses;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Equinor.SubSurfAppManagementMonitoringNuGet.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Extensions;

public static class MonitoringServiceCollectionExtensions
{
    /// <summary>
    /// Configuration key read for the Azure Monitor connection string when the standard
    /// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> environment variable is not set.
    /// </summary>
    public const string ConnectionStringConfigKey = "ApplicationInsights:ConnectionString";

    /// <summary>
    /// Registers a single, opinionated OpenTelemetry + Azure Monitor telemetry pipeline for the host application:
    /// ASP.NET Core and HttpClient instrumentation, the Azure Monitor exporter, and SAM's audit enrichment
    /// (<see cref="AuditActivityEnricher"/>).
    /// </summary>
    /// <remarks>
    /// This replaces the need for each consuming team to individually wire up
    /// <c>AddApplicationInsightsTelemetry</c>, instrumentation libraries, and audit enrichment. It supersedes
    /// the classic <c>Microsoft.ApplicationInsights.AspNetCore</c> SDK, which does not read the
    /// <see cref="System.Diagnostics.Activity"/> tags that <see cref="AuditActivityEnricher"/> sets, meaning
    /// audit enrichment silently never reaches Application Insights for apps still on that SDK.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Application configuration, used to resolve <c>ApplicationInsights:ConnectionString</c> when the
    /// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> environment variable is not set.
    /// </param>
    /// <param name="appName">
    /// The name of your application. Shows up as the Cloud Role Name in Application Insights.
    /// </param>
    /// <param name="configureOptions">
    /// Optional callback to override <see cref="SamMonitoringOptions"/> defaults, e.g. to disable audit
    /// enrichment entirely or exclude the caller's IP address for privacy/GDPR reasons. All fields are
    /// captured by default. Applied as a post-configure step, so it always takes precedence over any
    /// <c>SamMonitoring</c> configuration section (see <see cref="SamMonitoringOptions.ConfigSectionName"/>).
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="appName"/> is empty, or no connection string can be resolved from
    /// configuration or the standard environment variable.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the legacy <see cref="Startup.Configure"/>/<see cref="AuditActivityProcessor"/> path has
    /// already registered services on <paramref name="services"/>. Both mechanisms enrich activities with the
    /// same audit tags; registering both would silently double-tag every request.
    /// </exception>
    public static IServiceCollection AddSamMonitoring(
        this IServiceCollection services,
        IConfiguration configuration,
        string appName,
        Action<SamMonitoringOptions>? configureOptions = null)
    {
        Guard.Against.NullOrWhiteSpace(appName, nameof(appName));

#pragma warning disable CS0618 // Type or member is obsolete - this is the detection this fail-fast guard needs.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(AuditActivityProcessor)))
        {
            throw new InvalidOperationException(
                $"{nameof(AuditActivityProcessor)} is already registered (likely via the legacy "
                    + $"{nameof(Startup)}.{nameof(Startup.Configure)}()). Registering both it and "
                    + $"{nameof(AddSamMonitoring)}() would double-tag every activity with audit enrichment. "
                    + $"Remove the {nameof(Startup)}.{nameof(Startup.Configure)}() call and use only "
                    + $"{nameof(AddSamMonitoring)}()."
            );
        }
#pragma warning restore CS0618

        var connectionString = ResolveConnectionString(configuration);
        Guard.Against.NullOrWhiteSpace(
            connectionString,
            // Not an actual parameter of AddSamMonitoring - connectionString is resolved internally from
            // configuration/environment - so this is a fixed, descriptive label rather than nameof(), to avoid
            // ArgumentException.ParamName pointing callers at a name that doesn't exist in this method's signature.
            "connectionString (resolved)",
            $"No Azure Monitor connection string was found. Set the APPLICATIONINSIGHTS_CONNECTION_STRING " +
            $"environment variable, or the '{ConnectionStringConfigKey}' configuration key.");

        // Standard .NET Options pattern: bind from the "SamMonitoring" configuration section first, so ops
        // teams can tune this per-environment via appsettings/App Configuration without a code change, then
        // apply configureOptions as a post-configure step so a caller's explicit code-based choice always wins
        // over configuration. Registered as IOptions<SamMonitoringOptions>, resolvable elsewhere in the
        // consuming app if needed, instead of a locally-captured, one-off POCO.
        services.Configure<SamMonitoringOptions>(configuration.GetSection(SamMonitoringOptions.ConfigSectionName));
        if (configureOptions is not null)
        {
            services.PostConfigure(configureOptions);
        }

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: appName))
            .UseAzureMonitor(o => o.ConnectionString = connectionString);

        // Native OpenTelemetry extensibility point (instead of a custom BaseProcessor<Activity>): runs once
        // per inbound request rather than on every ended activity (including nested dependency spans), and
        // receives the HttpRequest directly. AddOptions<T>().Configure<TDep> composes correctly even though
        // UseAzureMonitor() above is what actually registers AddAspNetCoreInstrumentation() under the hood.
        // Uses "+=" (multicast) rather than "=" so a consuming app can still register its own
        // EnrichWithHttpRequest (via its own Configure<AspNetCoreTraceInstrumentationOptions> call) to add
        // app-specific custom dimensions, without silently overwriting SAM's mandatory audit tags.
        // Uses the Configure<TDep> overload on OptionsBuilder<T> to inject IOptionsMonitor<SamMonitoringOptions>
        // into the callback, so EnableAuditEnrichment/CaptureClientIpAddress are read fresh per request
        // (honoring IOptionsMonitor reload-on-change semantics) rather than closing over an options instance
        // resolved once at startup.
        services
            .AddOptions<AspNetCoreTraceInstrumentationOptions>()
            .Configure<IOptionsMonitor<SamMonitoringOptions>>(
                (o, samOptionsMonitor) =>
                    o.EnrichWithHttpRequest += (activity, httpRequest) =>
                    {
                        var samOptions = samOptionsMonitor.CurrentValue;
                        if (samOptions.EnableAuditEnrichment)
                        {
                            AuditActivityEnricher.Enrich(activity, httpRequest, samOptions);
                        }
                    });

        return services;
    }

    private static string? ResolveConnectionString(IConfiguration configuration) =>
        configuration[ConnectionStringConfigKey]
        ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
}

