using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using OpenTelemetry;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

/// <summary>
/// Enriches activities with user and request metadata for audit telemetry.
/// Replaces AuditTelemetryInitializer
/// </summary>
public class AuditActivityProcessor(IHttpContextAccessor contextAccessor) : BaseProcessor<Activity>
{
    public const string AuthenticatedUserTag = "enduser.id";
    public const string RemoteIpTag = "client.address";
    public const string ApplicationRolesTag = "UserApplicationRoles";
    public const string UserAgentTag = "user-agent";
        
    public override void OnEnd(Activity data)
    {
        try
        {
            if (contextAccessor.HttpContext is not { } httpContext) return;
            
            var ip = ResolveClientIp(httpContext);
            if (!string.IsNullOrWhiteSpace(ip))
            {
                data.SetTag(RemoteIpTag, ip);
            }

            if (httpContext.Request.Headers.UserAgent is { Count: > 0 } userAgent)
            {
                data.SetTag(UserAgentTag, userAgent.ToString());
            }

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId =
                    httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ??
                    httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ??
                    httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Upn)?.Value;

                data.SetTag(AuthenticatedUserTag, userId);

                var roles = new List<string>();
                foreach (var identity in httpContext.User.Identities)
                {
                    roles.AddRange(identity.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value));
                }

                data.SetTag(ApplicationRolesTag, string.Join(", ", roles));
            }
        }
        catch
        {
            data.SetTag(AuthenticatedUserTag, "<unknown>");
        }
        finally
        {
            base.OnEnd(data);
        }
    }

    private static string? ResolveClientIp(HttpContext httpContext)
    {
        var forwarded = httpContext.Request.Headers["Forwarded"].FirstOrDefault();
        var fromForwardedHeader = TryParseForwardedHeader(forwarded);
        if (!string.IsNullOrWhiteSpace(fromForwardedHeader))
        {
            return fromForwardedHeader;
        }

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var firstHop = forwardedFor?.Split(',').FirstOrDefault()?.Trim();
        if (!string.IsNullOrWhiteSpace(firstHop))
        {
            return StripPort(firstHop);
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string? TryParseForwardedHeader(string? forwardedHeader)
    {
        if (string.IsNullOrWhiteSpace(forwardedHeader))
        {
            return null;
        }

        var firstEntry = forwardedHeader.Split(',').FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstEntry))
        {
            return null;
        }

        foreach (var part in firstEntry.Split(';'))
        {
            var trimmed = part.Trim();
            if (!trimmed.StartsWith("for=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = trimmed[4..].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(value) || value.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // RFC 7239 can send IPv6 as for="[2001:db8::1]:1234"
            if (value.StartsWith("[") && value.Contains(']'))
            {
                var endBracket = value.IndexOf(']');
                return value[1..endBracket];
            }

            return StripPort(value);
        }

        return null;
    }

    private static string StripPort(string value)
    {
        if (IPAddress.TryParse(value, out _))
        {
            return value;
        }

        var colonIndex = value.LastIndexOf(':');
        if (colonIndex > 0)
        {
            var withoutPort = value[..colonIndex];
            if (IPAddress.TryParse(withoutPort, out _))
            {
                return withoutPort;
            }
        }

        return value;
    }
}