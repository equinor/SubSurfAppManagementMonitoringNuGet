namespace Equinor.SubSurfAppManagementMonitoringNuGet.Models;

/// <summary>
/// Represents a resource within the application, including its name, status, and additional details.
/// </summary>
public class Resource
{
    /// <summary>
    /// Gets or sets the name of the resource.
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the resource. 
    /// This may indicate values such as "Healthy," "Degraded," or "Unavailable...
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an additional message providing more details about the resource's status.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
