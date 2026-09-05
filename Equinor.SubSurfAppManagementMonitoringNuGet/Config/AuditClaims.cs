using System.Security.Claims;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

/// <summary>
/// Shared claim-resolution logic for audit telemetry, used by both <see cref="AuditActivityProcessor"/> (legacy)
/// and <see cref="AuditActivityEnricher"/> (current), so the two enrichment paths can never silently drift
/// from one another.
/// </summary>
internal static class AuditClaims
{
    /// <summary>
    /// Placeholder value set when a request is authenticated but no recognized identity claim can be resolved.
    /// </summary>
    public const string UnknownUser = "<unknown>";

    /// <summary>
    /// Claim types checked for application roles, in addition to <see cref="ClaimTypes.Role"/>. Some consuming
    /// apps configure <c>JwtBearerOptions.MapInboundClaims = false</c> (a common recommendation to avoid claim
    /// renaming surprises), in which case the raw, un-mapped Azure AD app role claim ("roles") is used instead
    /// of the mapped <see cref="ClaimTypes.Role"/> claim type. Checking both keeps role capture consistent
    /// regardless of how a consuming app configures its JWT bearer handler.
    /// </summary>
    private static readonly string[] RoleClaimTypes = [ClaimTypes.Role, "roles"];

    /// <summary>
    /// Resolves a human-identifiable user id from the given claims, falling back through
    /// <see cref="ClaimTypes.Name"/>, <see cref="ClaimTypes.Email"/>, <see cref="ClaimTypes.Upn"/>, and the raw
    /// "upn" claim type, in that order. Returns <see cref="UnknownUser"/> if none are present.
    /// </summary>
    public static string ResolveUserId(IReadOnlyCollection<Claim> claims) =>
        claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
        ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
        ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Upn)?.Value
        ?? claims
            .FirstOrDefault(c => string.Equals(c.Type, "upn", StringComparison.OrdinalIgnoreCase))
            ?.Value
        ?? UnknownUser;

    /// <summary>
    /// Resolves the distinct set of application role claim values from the given claims.
    /// </summary>
    /// <remarks>
    /// <paramref name="claims"/> is expected to come from <see cref="ClaimsPrincipal.Claims"/>, which already
    /// flattens claims across every <see cref="ClaimsIdentity"/> on the principal, so no separate iteration
    /// over identities is needed. <see cref="Enumerable.Distinct{TSource}(IEnumerable{TSource})"/> drops
    /// duplicate role claims that commonly occur when multiple identities (e.g. cookie + JWT) carry the same
    /// role, which would otherwise inflate SAM's per-role usage counts.
    /// </remarks>
    public static IEnumerable<string> ResolveRoles(IReadOnlyCollection<Claim> claims) =>
        claims.Where(c => RoleClaimTypes.Contains(c.Type)).Select(c => c.Value).Distinct();
}
