using Equinor.SubSurfAppManagementMonitoringNuGet.Authentication;
using Equinor.SubSurfAppManagementMonitoringNuGet.HealthServices.smda;
using Equinor.SubSurfAppManagementMonitoringNuGet.HttpClients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Extensions;

public static class SmdaHealthChecksBuilderExtension
{
    private const string NAME = "smda";

    /// <summary>
    /// Add a health check for smda.
    /// </summary>
    /// <typeparam name="T">The type of the access token service implementing <see cref="IAccessTokenService"/>.</typeparam>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="cacheControlHeaderValue">The cache control header to be set</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'smda' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <param name="requestUrl">The url to be used, should include the api version </param>
    /// <param name="requestHealthPath">The path to get the health data</param>
    /// <param name="apiKey">Api to be set</param>
    /// <param name="resourceId">Id for the resource to get an accesstoken for</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddSmdaHealthChecks<T>(
        this IHealthChecksBuilder builder,
        string requestUrl,
        string requestHealthPath,
        string apiKey,
        string resourceId,
        string cacheControlHeaderValue,
        string? name = default,
        HealthStatus?
            failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default) where T : class, IAccessTokenService
    {
        return builder.AddSmdaHealthChecks<T>(_ => requestUrl, _ => requestHealthPath,_ => apiKey, _ => resourceId,_ => cacheControlHeaderValue, name, failureStatus, tags, timeout );
    }


    /// <summary>
    /// Add a health check for Redis services.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    /// <param name="requestUrlFactory"></param>
    /// <param name="requestPathFactory">A Factory for setting the request path</param>
    /// <param name="apiKeyFactory"></param>
    /// <param name="resourceIdFactory"></param>
    /// <param name="cacheControlHeaderFactory">A factory to build cache headers</param>
    /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'redis' will be used for the name.</param>
    /// <param name="failureStatus">
    /// The <see cref="HealthStatus"/> that should be reported when the health check fails. Optional. If <c>null</c> then
    /// the default status of <see cref="HealthStatus.Unhealthy"/> will be reported.
    /// </param>
    /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <returns>The specified <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddSmdaHealthChecks<T>(
        this IHealthChecksBuilder builder,
        Func<IServiceProvider, string> requestUrlFactory,
        Func<IServiceProvider, string> requestPathFactory,
        Func<IServiceProvider, string> apiKeyFactory,
        Func<IServiceProvider, string> resourceIdFactory,
        Func<IServiceProvider, string> cacheControlHeaderFactory,
        string? name = default,
        HealthStatus?
            failureStatus = default,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default) where T : class, IAccessTokenService
    {
        builder.Services.AddHttpClient<ISmdaClient, SmdaClient>((sp, client) =>
        {
            var baseAddress = requestUrlFactory(sp);
            var apiKey = apiKeyFactory(sp);
            var cacheControl = cacheControlHeaderFactory(sp);

            client.BaseAddress = new Uri(baseAddress);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            client.DefaultRequestHeaders.Add("Cache-Control", cacheControl);
            client.Timeout = timeout ?? client.Timeout;
            
            Console.WriteLine($"Smda baseaddress: {client.BaseAddress}");
        });
        
        builder.Services.AddScoped<IAccessTokenService, T>();
        
        return builder.Add(new HealthCheckRegistration(
            name ?? NAME,
            sp =>
            {
                var tokenService = sp.GetRequiredService<IAccessTokenService>();
                var client = sp.GetRequiredService<ISmdaClient>();
                var logger = sp.GetRequiredService<ILogger<SmdaHealthCheck>>();
                var requestPath = requestPathFactory(sp);
                var resourceId = resourceIdFactory(sp);
                return new SmdaHealthCheck(client, tokenService, requestPath, resourceId, logger);
            },
            failureStatus,
            tags,
            timeout
        ));
    }
}