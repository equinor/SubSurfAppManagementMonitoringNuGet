using Equinor.SubSurfAppManagementMonitoringNuGet.Config;
using Equinor.SubSurfAppManagementMonitoringNuGet.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Trace;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.UnitTests;

public class MonitoringServiceCollectionExtensionsShould
{
    private const string EnvVarName = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    private string? _originalEnvVar;

    [SetUp]
    public void Setup()
    {
        _originalEnvVar = Environment.GetEnvironmentVariable(EnvVarName);
        Environment.SetEnvironmentVariable(EnvVarName, null);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(EnvVarName, _originalEnvVar);
    }

    [Test]
    public void ThrowArgumentException_WhenAppNameIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=test");

        Assert.Throws<ArgumentException>(() =>
            services.AddSamMonitoring(configuration, appName: string.Empty));
    }

    [Test]
    public void ThrowInvalidOperationException_WhenLegacyAuditActivityProcessorIsAlreadyRegistered()
    {
        // Registering both the legacy Startup.Configure()/AuditActivityProcessor path and AddSamMonitoring()
        // would silently double-tag every activity with two different enrichment mechanisms. Fail fast instead.
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=test");
#pragma warning disable CS0618 // Type or member is obsolete - this is the exact legacy path being guarded against.
        Startup.Configure(services);
#pragma warning restore CS0618

        Assert.Throws<InvalidOperationException>(() =>
            services.AddSamMonitoring(configuration, appName: "MyApp"));
    }

    [Test]
    public void ThrowArgumentException_WhenNoConnectionStringIsConfiguredOrSetAsEnvironmentVariable()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: null);

        Assert.Throws<ArgumentNullException>(() =>
            services.AddSamMonitoring(configuration, appName: "MyApp"));
    }

    [Test]
    public void ResolveConnectionString_FromConfiguration_WhenEnvironmentVariableIsAbsent()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=from-config");

        Assert.DoesNotThrow(() => services.AddSamMonitoring(configuration, appName: "MyApp"));
    }

    [Test]
    public void ResolveConnectionString_FromEnvironmentVariable_WhenConfigurationIsAbsent()
    {
        Environment.SetEnvironmentVariable(EnvVarName, "InstrumentationKey=from-env");
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: null);

        Assert.DoesNotThrow(() => services.AddSamMonitoring(configuration, appName: "MyApp"));
    }

    [Test]
    public void RegisterTracerProvider_WhenConfiguredSuccessfully()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=test");

        services.AddSamMonitoring(configuration, appName: "MyApp");
        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetService<TracerProvider>(), Is.Not.Null);
    }

    [Test]
    public void WireAuditActivityEnricher_AsTheAspNetCoreEnrichWithHttpRequestCallback()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=test");

        services.AddSamMonitoring(configuration, appName: "MyApp");
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value;
        using var activity = new System.Diagnostics.Activity("Test").Start();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";

        options.EnrichWithHttpRequest?.Invoke(activity, httpContext.Request);

        Assert.That(activity.GetTagItem(AuditTagNames.UserAgentTag), Is.EqualTo("TestAgent/1.0"));
    }

    [Test]
    public void NotSetAnyAuditTags_WhenEnableAuditEnrichmentIsDisabled()
    {
        // The enrichment callback is now always registered (it's resolved via IOptionsMonitor at request time
        // rather than gating registration itself), so this asserts the observable effect - no tags set - not
        // the registration mechanics.
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=test");

        services.AddSamMonitoring(
            configuration,
            appName: "MyApp",
            options => options.EnableAuditEnrichment = false);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value;
        using var activity = new System.Diagnostics.Activity("Test").Start();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";

        options.EnrichWithHttpRequest?.Invoke(activity, httpContext.Request);

        Assert.That(
            activity.GetTagItem(AuditTagNames.UserAgentTag),
            Is.Null,
            "Disabling EnableAuditEnrichment must suppress all audit tags, including non-PII ones"
        );
    }

    [Test]
    public void BindSamMonitoringOptions_FromTheSamMonitoringConfigurationSection()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            connectionString: "InstrumentationKey=test",
            extra: new Dictionary<string, string?>
            {
                [$"{SamMonitoringOptions.ConfigSectionName}:{nameof(SamMonitoringOptions.EnableAuditEnrichment)}"] =
                    "false",
            });

        services.AddSamMonitoring(configuration, appName: "MyApp");
        using var provider = services.BuildServiceProvider();

        var samOptions = provider.GetRequiredService<IOptions<SamMonitoringOptions>>().Value;

        Assert.That(
            samOptions.EnableAuditEnrichment,
            Is.False,
            "SamMonitoringOptions should bind from the 'SamMonitoring' configuration section"
        );
    }

    [Test]
    public void PreferCodeConfigureOptionsOverConfiguration_WhenBothAreProvided()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            connectionString: "InstrumentationKey=test",
            extra: new Dictionary<string, string?>
            {
                [$"{SamMonitoringOptions.ConfigSectionName}:{nameof(SamMonitoringOptions.EnableAuditEnrichment)}"] =
                    "true",
            });

        services.AddSamMonitoring(configuration, appName: "MyApp", options => options.EnableAuditEnrichment = false);
        using var provider = services.BuildServiceProvider();

        var samOptions = provider.GetRequiredService<IOptions<SamMonitoringOptions>>().Value;

        Assert.That(
            samOptions.EnableAuditEnrichment,
            Is.False,
            "The code-based configureOptions callback should win over the configuration section"
        );
    }

    [Test]
    public void NotCaptureClientIpAddress_WhenCaptureClientIpAddressIsDisabled()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=test");

        services.AddSamMonitoring(
            configuration,
            appName: "MyApp",
            options => options.CaptureClientIpAddress = false);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value;
        using var activity = new System.Diagnostics.Activity("Test").Start();
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        options.EnrichWithHttpRequest?.Invoke(activity, httpContext.Request);

        Assert.That(activity.GetTagItem(AuditTagNames.RemoteIpTag), Is.Null);
    }

    [Test]
    public void ComposeWithConsumerRegisteredEnrichment_InsteadOfOverwritingIt()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: "InstrumentationKey=test");
        var customEnrichCalled = false;

        services.AddSamMonitoring(configuration, appName: "MyApp");
        // Simulates a consuming app registering its own enrichment after AddSamMonitoring().
        services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
            options.EnrichWithHttpRequest += (_, _) => customEnrichCalled = true);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>().Value;
        using var activity = new System.Diagnostics.Activity("Test").Start();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";

        options.EnrichWithHttpRequest?.Invoke(activity, httpContext.Request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(customEnrichCalled, Is.True, "The consuming app's own enrichment should still run");
            Assert.That(
                activity.GetTagItem(AuditTagNames.UserAgentTag),
                Is.EqualTo("TestAgent/1.0"),
                "SAM's own enrichment should still run and set its tags"
            );
        }
    }

    private static IConfiguration BuildConfiguration(
        string? connectionString,
        IDictionary<string, string?>? extra = null)
    {
        var data = new Dictionary<string, string?>();
        if (connectionString is not null)
            data[MonitoringServiceCollectionExtensions.ConnectionStringConfigKey] = connectionString;

        if (extra is not null)
        {
            foreach (var (key, value) in extra)
                data[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }
}
