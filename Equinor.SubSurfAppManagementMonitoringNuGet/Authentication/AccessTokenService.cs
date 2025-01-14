using Equinor.SubSurfAppManagementMonitoringNuGet.HealthServices.smda;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.Authentication;

/// <summary>
/// Defines the interface which must be implemented for achieving accesstokens for default HealthCheckServices such as smda: <see cref="SmdaHealthCheck"/>
/// </summary>
public interface IAccessTokenService
{
    /// <summary>
    /// Get access token on behalf of the caller
    /// </summary>
    Task<string> GetAccessTokenOnBehalfOfAsync(string resourceId);
}