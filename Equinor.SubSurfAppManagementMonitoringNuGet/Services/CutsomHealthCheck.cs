using Equinor.SubSurfAppManagementMonitoringNuget.Models;

namespace Equinor.SubSurfAppManagementMonitoringNuget.Services;


/// <summary>
/// Provides an abstraction for executing health checks
/// for custom services used by the application.
/// </summary>
public interface ICustomHealthCheck
{
    Task<Resource> CheckHealth(CancellationToken cancellationToken);    
}
