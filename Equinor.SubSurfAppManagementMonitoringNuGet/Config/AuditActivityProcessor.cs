using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

/// <summary>
/// Enriches activities with user and request metadata for audit telemetry.
/// Replaces AuditTelemetryInitializer
/// </summary>
/// <remarks>
/// Legacy path, kept only for backward compatibility with existing consumers of
/// <see cref="Startup.Configure"/>. Prefer
/// <see cref="Extensions.MonitoringServiceCollectionExtensions.AddSamMonitoring"/>, which uses the native
/// OpenTelemetry <c>EnrichWithHttpRequest</c> extensibility point (<see cref="AuditActivityEnricher"/>) instead:
/// that runs once per inbound request rather than on every ended activity (including nested dependency spans),
/// and its host also configures the Azure Monitor exporter. Do not register both in the same application -
/// every activity would then be tagged twice by two different mechanisms.
/// </remarks>
[Obsolete(
    "Use AddSamMonitoring() instead, which uses AuditActivityEnricher via the native OpenTelemetry "
        + "EnrichWithHttpRequest extensibility point and also configures the Azure Monitor exporter."
)]
public class AuditActivityProcessor(IHttpContextAccessor contextAccessor) : BaseProcessor<Activity>
{
    public const string AuthenticatedUserTag = AuditTagNames.AuthenticatedUserTag;
    public const string RemoteIpTag = AuditTagNames.RemoteIpTag;
    public const string ApplicationRolesTag = AuditTagNames.ApplicationRolesTag;
    public const string UserAgentTag = AuditTagNames.UserAgentTag;

    public override void OnEnd(Activity data)
    {
        try
        {
            if (contextAccessor.HttpContext is not { } httpContext)
                return;

            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].LastOrDefault();
            var ip = !string.IsNullOrWhiteSpace(forwardedFor)
                ? forwardedFor.Split(',').LastOrDefault()?.Trim()
                : httpContext.Connection.RemoteIpAddress?.ToString();

            if (!string.IsNullOrWhiteSpace(ip))
            {
                data.SetTag(RemoteIpTag, ip);
            }

            if (httpContext.Request.Headers.UserAgent is { Count: > 0 } userAgent)
            {
                data.SetTag(UserAgentTag, userAgent.ToString());
            }

            EnrichAuthenticatedUser(data, httpContext);
        }
        catch
        {
            // Enrichment must never break the pipeline; tags set here are best-effort. This does NOT set
            // AuthenticatedUserTag to "<unknown>" - that sentinel is only set when we already know the request
            // is authenticated but claim resolution itself failed (see EnrichAuthenticatedUser), so an
            // unrelated failure (e.g. malformed X-Forwarded-For header) on an anonymous request can't corrupt
            // SAM's per-role/per-user KQL analytics by mislabeling it as an unresolved authenticated user.
        }
        finally
        {
            base.OnEnd(data);
        }
    }

    private static void EnrichAuthenticatedUser(Activity data, HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        try
        {
            var claims = httpContext.User.Claims.ToList();

            data.SetTag(AuthenticatedUserTag, AuditClaims.ResolveUserId(claims));
            data.SetTag(ApplicationRolesTag, string.Join(", ", AuditClaims.ResolveRoles(claims)));
        }
        catch
        {
            data.SetTag(AuthenticatedUserTag, AuditClaims.UnknownUser);
        }
    }
}
