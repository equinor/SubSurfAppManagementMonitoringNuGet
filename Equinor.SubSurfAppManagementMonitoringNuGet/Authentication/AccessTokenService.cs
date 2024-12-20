namespace Equinor.SubSurfAppManagementMonitoringNuGet.Authentication;

/// <summary>
/// Defines methods for achieving accesstokens 
/// </summary>
public interface IAccessTokenService
{
    /// <summary>
    /// Get access token on behalf of the app
    /// </summary>
    Task<string> GetAccessTokenAsync(string resourceId);

    /// <summary>
    /// Get access token on behalf of the user
    /// </summary>
    Task<string> GetAccessTokenOnBehalfOfAsync(string resourceId);
}