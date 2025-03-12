using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Config;

public class AuditTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IWebHostEnvironment _env;

    public AuditTelemetryInitializer(IHttpContextAccessor contextAccessor, IWebHostEnvironment env)
    {
        _contextAccessor = contextAccessor;
        _env = env;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _contextAccessor.HttpContext;
        try
        {
            if (httpContext?.Connection.RemoteIpAddress != null)
            {
                telemetry.Context.Location.Ip = httpContext.Connection.RemoteIpAddress.ToString();
            }

            if (httpContext?.Request?.Headers["User-Agent"] is not null)
            {
                telemetry.Context.GlobalProperties["user-agent"] = httpContext.Request.Headers["User-Agent"].ToString();
            }

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                if (httpContext.User.HasClaim(c => c.Type == ClaimTypes.Name))
                {
                    telemetry.Context.User.AuthenticatedUserId =
                        httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                }
                else if (httpContext.User.HasClaim(c => c.Type.Equals("upn", StringComparison.OrdinalIgnoreCase)))
                {
                    telemetry.Context.User.AuthenticatedUserId =
                        httpContext.User.Claims
                            .FirstOrDefault(c => c.Type.Equals("upn", StringComparison.OrdinalIgnoreCase))?.Value;
                }
                else
                {
                    telemetry.Context.User.AuthenticatedUserId = "<unknown>";
                }
            }
        }
        catch
        {
            telemetry.Context.User.AuthenticatedUserId = "<unknown>";
        }
    }
}