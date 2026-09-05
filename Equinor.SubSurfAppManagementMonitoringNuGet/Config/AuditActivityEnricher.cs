using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

/// <summary>
/// Enriches the ASP.NET Core request activity with user and request metadata for audit telemetry, using
/// OpenTelemetry's native <c>EnrichWithHttpRequest</c> extensibility point on
/// <c>OpenTelemetry.Instrumentation.AspNetCore.AspNetCoreTraceInstrumentationOptions</c>.
/// </summary>
/// <remarks>
/// Unlike <see cref="AuditActivityProcessor"/> (a <see cref="OpenTelemetry.BaseProcessor{T}"/> that runs on every
/// ended activity, including dependency spans nested inside a request), this enrichment method only runs once
/// per inbound HTTP request, and receives the <see cref="HttpRequest"/> directly instead of requiring an
/// injected <see cref="IHttpContextAccessor"/>. See
/// https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md#enrich
/// </remarks>
public static class AuditActivityEnricher
{
    /// <summary>
    /// Matches the <c>Action&lt;Activity, HttpRequest&gt;</c> signature of
    /// <c>AspNetCoreTraceInstrumentationOptions.EnrichWithHttpRequest</c>. Enriches with all fields enabled;
    /// use the <see cref="Enrich(Activity, HttpRequest, SamMonitoringOptions)"/> overload to respect a
    /// consumer's privacy/GDPR configuration (e.g. disabling IP capture).
    /// </summary>
    public static void Enrich(Activity activity, HttpRequest httpRequest) =>
        Enrich(activity, httpRequest, new SamMonitoringOptions());

    /// <summary>
    /// Enriches the activity honoring <paramref name="options"/>, so privacy/GDPR-sensitive fields (currently
    /// just the client IP address) can be excluded per consuming application.
    /// </summary>
    public static void Enrich(Activity activity, HttpRequest httpRequest, SamMonitoringOptions options)
    {
        try
        {
            var httpContext = httpRequest.HttpContext;

            if (options.CaptureClientIpAddress)
            {
                var forwardedFor = httpRequest.Headers["X-Forwarded-For"].LastOrDefault();
                var ip = !string.IsNullOrWhiteSpace(forwardedFor)
                    ? forwardedFor.Split(',').LastOrDefault()?.Trim()
                    : httpContext.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrWhiteSpace(ip))
                {
                    activity.SetTag(AuditTagNames.RemoteIpTag, ip);
                }
            }

            if (httpRequest.Headers.UserAgent is { Count: > 0 } userAgent)
            {
                activity.SetTag(AuditTagNames.UserAgentTag, userAgent.ToString());
            }

            EnrichAuthenticatedUser(activity, httpContext);
        }
        catch
        {
            // Enrichment must never break the request pipeline; tags set here are best-effort. Note this does
            // NOT set AuthenticatedUserTag to "<unknown>" - that sentinel is only set when we already know the
            // request is authenticated but claim resolution itself failed (see EnrichAuthenticatedUser), so an
            // unrelated failure (e.g. malformed X-Forwarded-For header) on an anonymous request can't corrupt
            // SAM's per-role/per-user KQL analytics by mislabeling it as an unresolved authenticated user.
        }
    }

    private static void EnrichAuthenticatedUser(Activity activity, HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        try
        {
            var claims = httpContext.User.Claims.ToList();

            activity.SetTag(AuditTagNames.AuthenticatedUserTag, AuditClaims.ResolveUserId(claims));
            activity.SetTag(
                AuditTagNames.ApplicationRolesTag,
                string.Join(", ", AuditClaims.ResolveRoles(claims))
            );
        }
        catch
        {
            activity.SetTag(AuditTagNames.AuthenticatedUserTag, AuditClaims.UnknownUser);
        }
    }
}
