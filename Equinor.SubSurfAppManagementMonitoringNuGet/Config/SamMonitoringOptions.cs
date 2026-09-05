namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

/// <summary>
/// Configures SAM's built-in audit telemetry enrichment (user identity, roles, IP address, user agent) added by
/// <see cref="Extensions.MonitoringServiceCollectionExtensions.AddSamMonitoring"/>.
/// </summary>
/// <remarks>
/// Application Insights request telemetry enriched this way (<c>enduser.id</c>, <c>UserApplicationRoles</c>,
/// and the caller's IP address) can constitute personal data under GDPR. Enrichment is enabled by default so
/// SAM's cross-application analytics (e.g. per-role usage) keep working out of the box without every consuming
/// team needing to know this exists, but apps with stricter privacy requirements can narrow or fully disable it
/// here.
/// </remarks>
public class SamMonitoringOptions
{
    /// <summary>
    /// Configuration section name this options class binds from by default (e.g. <c>SamMonitoring</c> in
    /// <c>appsettings.json</c>), in addition to the code-based <c>configureOptions</c> callback on
    /// <see cref="Extensions.MonitoringServiceCollectionExtensions.AddSamMonitoring"/>. The callback is applied
    /// as a post-configure step, so it always takes precedence over configuration-bound values.
    /// </summary>
    public const string ConfigSectionName = "SamMonitoring";

    /// <summary>
    /// Master switch for SAM's audit enrichment (user identity, roles, IP address, user agent). Defaults to
    /// <see langword="true"/>. Set to <see langword="false"/> to send no personal data via this enrichment path
    /// at all, e.g. for apps with stricter privacy/GDPR requirements that already handle audit logging
    /// elsewhere. Doing so also removes that app from SAM's cross-application role/usage analytics.
    /// </summary>
    public bool EnableAuditEnrichment { get; set; } = true;

    /// <summary>
    /// Whether to capture the caller's IP address (<c>client.address</c> tag). Defaults to
    /// <see langword="true"/>. IP addresses are personal data under GDPR Art. 4(1); disable this independently
    /// of <see cref="EnableAuditEnrichment"/> to keep the authenticated user/role audit trail (needed for SAM's
    /// analytics) while excluding raw IP addresses from telemetry.
    /// </summary>
    public bool CaptureClientIpAddress { get; set; } = true;
}
