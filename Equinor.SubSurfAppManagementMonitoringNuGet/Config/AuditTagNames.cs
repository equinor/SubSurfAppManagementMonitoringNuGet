namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

/// <summary>
/// Central definitions for audit-related telemetry tag names, shared by both the legacy
/// <see cref="AuditActivityProcessor"/> processor and the native <see cref="AuditActivityEnricher"/>
/// enrichment path.
/// </summary>
/// <remarks>
/// These exact names are relied upon by SAM's own Application Insights KQL queries
/// (e.g. <c>Sam.Api.Services.ApplicationInsightService.GetRoleCountPerDay</c>, which reads
/// <c>customDimensions["UserApplicationRoles"]</c> and the <c>user_AuthenticatedId</c> field the Azure Monitor
/// exporter derives from <c>enduser.id</c>), so renaming any of them would silently break that feature for
/// every consuming application.
/// </remarks>
public static class AuditTagNames
{
    /// <summary>OpenTelemetry semantic convention tag; mapped by the Azure Monitor exporter to <c>user_AuthenticatedId</c>.</summary>
    public const string AuthenticatedUserTag = "enduser.id";

    /// <summary>OpenTelemetry semantic convention tag; mapped by the Azure Monitor exporter to the request's client IP.</summary>
    public const string RemoteIpTag = "client.address";

    /// <summary>Custom tag; surfaced under Application Insights' <c>customDimensions</c>.</summary>
    public const string ApplicationRolesTag = "UserApplicationRoles";

    /// <summary>Custom tag; surfaced under Application Insights' <c>customDimensions</c>.</summary>
    public const string UserAgentTag = "user-agent";
}
