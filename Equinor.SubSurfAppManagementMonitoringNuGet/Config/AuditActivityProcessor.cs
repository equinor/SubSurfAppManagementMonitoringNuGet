using System.Diagnostics;
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
            var httpContext = contextAccessor.HttpContext;
            if (httpContext?.Connection.RemoteIpAddress != null)
            {
                data.SetTag(RemoteIpTag, httpContext.Connection.RemoteIpAddress.ToString());
            }

            if (httpContext?.Request.Headers.UserAgent is { Count: > 0 } userAgent)
            {
                data.SetTag(UserAgentTag, userAgent.ToString());
            }

            if (httpContext?.User.Identity?.IsAuthenticated == true)
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

        base.OnEnd(data);
    }
}